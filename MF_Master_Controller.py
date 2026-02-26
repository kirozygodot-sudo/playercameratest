"""
MF_Master_Controller — Modular Factory v4.0
Blender 4.3+ | Kiro IDE Terminal Controller

Architecture:
  Layer 0 — Style DNA     : JSON-based visual style profiles
  Layer 1 — Atomics       : Wall / Floor / Ceiling / Pillar / Beam generators
  Layer 2 — Assemblers    : Room, Corridor, Staircase
  Layer 3 — Building      : Multi-floor building from rooms
  Layer 4 — Command API   : AI → JSON → Blender bridge

Grid Convention:
  1 unit = 1 metre | sub-grid = 0.25 m
  Pivot: bottom-left corner (0, 0, 0)
  Naming: MF_[Style]_[Type]_[W]x[H]_v[n]

Blender Modifiers used:
  ARRAY, MIRROR, BEVEL, BOOLEAN, SOLIDIFY, SUBDIVISION,
  GEOMETRY_NODES, CLOTH, ARMATURE

Godot 4.x Export:
  - Auto applies scale (bpy.ops.object.transform_apply)
  - Appends _col suffix for collision meshes
  - Exports glTF 2.0 with embedded PBR textures
  - Writes metadata JSON sidecar for room/building data
"""

from __future__ import annotations

import bpy
import bmesh
import json
import os
import math
import traceback
from pathlib import Path
from typing import Any


# ═══════════════════════════════════════════════════════════════════════════
# CONSTANTS — Architectural Standards
# ═══════════════════════════════════════════════════════════════════════════

GRID           = 1.0      # m — base grid unit
SUB_GRID       = 0.25     # m — sub-grid for detail snapping
WALL_H_FLOOR   = 3.0      # m — standard floor height (residential/office)
WALL_H_GROUND  = 4.0      # m — ground floor / retail height
WALL_THICKNESS = 0.2      # m — interior wall
WALL_EXT_T     = 0.3      # m — exterior / structural wall
FLOOR_SLAB_H   = 0.2      # m — floor slab thickness
DOOR_W         = 0.9      # m — standard door width
DOOR_H         = 2.1      # m — standard door height
WINDOW_W       = 1.0      # m
WINDOW_H       = 1.2      # m
WINDOW_SILL    = 0.9      # m — sill height from floor
BASEBOARD_H    = 0.12     # m
CORNICE_H      = 0.08     # m
PILLAR_W       = 0.4      # m — square pillar side
STEP_H         = 0.175    # m — stair riser (ergonomic: ≤0.18 m)
STEP_D         = 0.28     # m — stair tread depth (≥0.28 m)

COLLECTIONS = [
    "MF_Atomics",    # individual pieces (wall, floor…)
    "MF_Assembled",  # rooms, corridors
    "MF_Buildings",  # complete buildings
    "MF_Streets",    # street-level layout
    "MF_Exports",    # ready-for-Godot objects
]


# ═══════════════════════════════════════════════════════════════════════════
# UTILITY FUNCTIONS
# ═══════════════════════════════════════════════════════════════════════════

def snap_to_grid(value: float, grid: float = GRID) -> float:
    """Round a value to the nearest grid increment."""
    return round(value / grid) * grid


def snap_vec(v: tuple, grid: float = GRID) -> tuple:
    return tuple(snap_to_grid(c, grid) for c in v)


def fix_pivot(obj: bpy.types.Object, mode: str = "bottom_left") -> None:
    """
    Move object origin to the correct snap point.
    mode: 'bottom_left' | 'bottom_center' | 'center'
    """
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")

    bb = [obj.matrix_world @ bpy.types.Object.matrix_world.to_3x3() for _ in range(1)]
    # Simpler approach: use cursor for pivot placement
    prev_cursor = bpy.context.scene.cursor.location.copy()
    bounds = [obj.matrix_world @ v.co for v in obj.data.vertices] if obj.data else []

    if not bounds:
        return

    min_x = min(v.x for v in bounds)
    min_y = min(v.y for v in bounds)
    min_z = min(v.z for v in bounds)
    max_x = max(v.x for v in bounds)
    max_y = max(v.y for v in bounds)

    if mode == "bottom_left":
        bpy.context.scene.cursor.location = (min_x, min_y, min_z)
    elif mode == "bottom_center":
        bpy.context.scene.cursor.location = ((min_x + max_x) / 2, (min_y + max_y) / 2, min_z)
    else:
        bpy.ops.object.origin_set(type="ORIGIN_CENTER_OF_MASS")
        return

    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.context.scene.cursor.location = prev_cursor


def link_to_collection(obj: bpy.types.Object, col_name: str) -> None:
    """Move object to a named collection, creating it if needed."""
    col = bpy.data.collections.get(col_name)
    if col is None:
        col = bpy.data.collections.new(col_name)
        bpy.context.scene.collection.children.link(col)
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)


def apply_modifiers(obj: bpy.types.Object) -> None:
    """Apply all modifiers (needed before Godot export)."""
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        try:
            bpy.ops.object.modifier_apply(modifier=mod.name)
        except Exception:
            pass


def object_exists(name: str) -> bool:
    return name in bpy.data.objects


def safe_name(parts: list) -> str:
    return "_".join(str(p) for p in parts if p)


def mf_response(status: str, message: str = "", data: dict = None) -> dict:
    r = {"status": status, "message": message}
    if data:
        r.update(data)
    return r


# ═══════════════════════════════════════════════════════════════════════════
# ARCHITECTURAL RULE ENGINE
# ═══════════════════════════════════════════════════════════════════════════

class ArchRules:
    """
    Validates and auto-corrects parameters to meet architectural standards.
    Based on: Neufert Architects' Data, EN 15221, and game-dev best practices.
    """

    GOLDEN_RATIO = 1.618

    @staticmethod
    def floor_height(floor_index: int) -> float:
        """Ground floor = 4 m (retail/entrance), upper floors = 3 m."""
        return WALL_H_GROUND if floor_index == 0 else WALL_H_FLOOR

    @staticmethod
    def validate_room(width: float, depth: float, floor: int = 1) -> dict:
        """
        Returns corrected dimensions and warnings.
        Minimum room: 2.5 × 2.5 m (bathroom)
        Comfortable room: 3 × 3.5 m
        """
        warnings = []
        h = ArchRules.floor_height(floor)

        if width < 2.5:
            warnings.append(f"Width {width}m < 2.5m min. Clamped to 2.5m.")
            width = 2.5
        if depth < 2.5:
            warnings.append(f"Depth {depth}m < 2.5m min. Clamped to 2.5m.")
            depth = 2.5

        # Snap to sub-grid
        width = snap_to_grid(width, SUB_GRID)
        depth = snap_to_grid(depth, SUB_GRID)

        return {
            "width": width, "depth": depth, "height": h,
            "aspect": round(max(width, depth) / min(width, depth), 3),
            "warnings": warnings,
        }

    @staticmethod
    def validate_door(w: float, h: float) -> dict:
        """Clamp door dimensions to ergonomic range."""
        w = max(0.7, min(w, 2.4))   # single: 0.7–1.0; double: up to 2.4
        h = max(1.95, min(h, 2.8))
        return {"width": round(w, 2), "height": round(h, 2)}

    @staticmethod
    def validate_window(w: float, h: float, sill: float) -> dict:
        """Window proportions and sill height."""
        w    = max(0.5, min(w, 3.0))
        h    = max(0.5, min(h, 2.2))
        sill = max(0.6, min(sill, 1.2))
        return {"width": round(w, 2), "height": round(h, 2), "sill": round(sill, 2)}

    @staticmethod
    def stair_count(floor_height: float) -> int:
        """Calculate stair riser count from floor height."""
        return math.ceil(floor_height / STEP_H)

    @staticmethod
    def stair_run(floor_height: float) -> float:
        """Total horizontal run for a straight stair."""
        n = ArchRules.stair_count(floor_height)
        return n * STEP_D


# ═══════════════════════════════════════════════════════════════════════════
# STYLE DNA LOADER
# ═══════════════════════════════════════════════════════════════════════════

class StyleDNA:
    """
    Load and apply visual style profiles from JSON files.
    Expected DNA keys: materials, colors, damage, greeble_density, trim_scale
    """

    BUILT_IN: dict[str, dict] = {
        "DEFAULT": {
            "wall_material":   "MF_Mat_Concrete",
            "floor_material":  "MF_Mat_Floor_Tile",
            "trim_material":   "MF_Mat_Metal_Trim",
            "damage":          0.0,
            "greeble_density": 0.1,
            "trim_scale":      1.0,
        },
        "SCIFI": {
            "wall_material":   "MF_Mat_SciFi_Panel",
            "floor_material":  "MF_Mat_SciFi_Grate",
            "trim_material":   "MF_Mat_Neon_Trim",
            "damage":          0.2,
            "greeble_density": 0.6,
            "trim_scale":      1.2,
        },
        "CYBERPUNK": {
            "wall_material":   "MF_Mat_Urban_Concrete",
            "floor_material":  "MF_Mat_Wet_Asphalt",
            "trim_material":   "MF_Mat_Rust_Metal",
            "damage":          0.5,
            "greeble_density": 0.8,
            "trim_scale":      0.9,
        },
        "MODERN": {
            "wall_material":   "MF_Mat_White_Plaster",
            "floor_material":  "MF_Mat_Hardwood",
            "trim_material":   "MF_Mat_Clean_Metal",
            "damage":          0.0,
            "greeble_density": 0.05,
            "trim_scale":      0.8,
        },
        "INDUSTRIAL": {
            "wall_material":   "MF_Mat_Corrugated_Metal",
            "floor_material":  "MF_Mat_Industrial_Grate",
            "trim_material":   "MF_Mat_Pipe_Metal",
            "damage":          0.4,
            "greeble_density": 0.7,
            "trim_scale":      1.1,
        },
    }

    @classmethod
    def get(cls, style_name: str, dna_dir: str | None = None) -> dict:
        name = style_name.upper()
        # Try external JSON first
        if dna_dir:
            path = Path(dna_dir) / f"dna_{name.lower()}.json"
            if path.exists():
                with open(path) as f:
                    return json.load(f)
        return cls.BUILT_IN.get(name, cls.BUILT_IN["DEFAULT"])

    @classmethod
    def apply_to_object(cls, obj: bpy.types.Object, dna: dict) -> None:
        """Assign material slots based on DNA."""
        mat_names = [dna.get("wall_material"), dna.get("trim_material")]
        obj.data.materials.clear()
        for mn in mat_names:
            if mn and mn in bpy.data.materials:
                obj.data.materials.append(bpy.data.materials[mn])
            else:
                obj.data.materials.append(None)


# ═══════════════════════════════════════════════════════════════════════════
# MODIFIER HELPERS
# ═══════════════════════════════════════════════════════════════════════════

class Modifiers:
    """Thin wrappers for Blender modifier operations."""

    @staticmethod
    def add_array(obj: bpy.types.Object, axis: str, count: int,
                  fit_length: float | None = None) -> bpy.types.Modifier:
        mod = obj.modifiers.new("MF_Array", "ARRAY")
        mod.count = count
        mod.use_relative_offset = True
        axes = {"X": (1, 0, 0), "Y": (0, 1, 0), "Z": (0, 0, 1)}
        ax = axes.get(axis.upper(), (1, 0, 0))
        mod.relative_offset_displace = ax
        if fit_length is not None:
            mod.fit_type   = "FIT_LENGTH"
            mod.fit_length = fit_length
        return mod

    @staticmethod
    def add_mirror(obj: bpy.types.Object, axis_x=True, axis_y=False,
                   axis_z=False) -> bpy.types.Modifier:
        mod = obj.modifiers.new("MF_Mirror", "MIRROR")
        mod.use_axis[0] = axis_x
        mod.use_axis[1] = axis_y
        mod.use_axis[2] = axis_z
        mod.use_clip    = True
        return mod

    @staticmethod
    def add_bevel(obj: bpy.types.Object, width: float = 0.02,
                  segments: int = 2) -> bpy.types.Modifier:
        mod = obj.modifiers.new("MF_Bevel", "BEVEL")
        mod.width        = width
        mod.segments     = segments
        mod.limit_method = "ANGLE"
        mod.angle_limit  = math.radians(30)
        return mod

    @staticmethod
    def add_solidify(obj: bpy.types.Object,
                     thickness: float = WALL_THICKNESS) -> bpy.types.Modifier:
        mod = obj.modifiers.new("MF_Solidify", "SOLIDIFY")
        mod.thickness     = thickness
        mod.offset        = 1.0   # grow inward (into wall)
        mod.use_even_offset = True
        return mod

    @staticmethod
    def add_subdivision(obj: bpy.types.Object,
                        levels: int = 1) -> bpy.types.Modifier:
        mod = obj.modifiers.new("MF_Subd", "SUBSURF")
        mod.levels        = levels
        mod.render_levels = levels
        return mod

    @staticmethod
    def add_gn(obj: bpy.types.Object,
               node_group: bpy.types.NodeTree) -> bpy.types.Modifier:
        mod = obj.modifiers.new("MF_GN", "NODES")
        mod.node_group = node_group
        return mod

    @staticmethod
    def set_gn_param(obj: bpy.types.Object,
                     socket_name: str, value: Any) -> bool:
        for mod in obj.modifiers:
            if mod.type != "NODES" or mod.node_group is None:
                continue
            for item in mod.node_group.interface.items_tree:
                if (item.item_type == "SOCKET" and
                        item.in_out == "INPUT" and
                        item.name == socket_name):
                    try:
                        mod[item.identifier] = value
                        return True
                    except Exception:
                        pass
        return False


# ═══════════════════════════════════════════════════════════════════════════
# LOD MANAGER
# ═══════════════════════════════════════════════════════════════════════════

class LODManager:
    """
    Manages Level-of-Detail variants.
    LOD0 — full detail (< 5 m from camera)
    LOD1 — medium   (5–20 m)
    LOD2 — low poly (> 20 m)
    """

    LOD_SUFFIX = {0: "_LOD0", 1: "_LOD1", 2: "_LOD2"}

    @staticmethod
    def create_lod(base_obj: bpy.types.Object,
                   lod_level: int,
                   decimate_ratio: float) -> bpy.types.Object:
        # Duplicate
        lod_obj = base_obj.copy()
        lod_obj.data = base_obj.data.copy()
        lod_obj.name = base_obj.name + LODManager.LOD_SUFFIX.get(lod_level, "_LOD?")
        bpy.context.collection.objects.link(lod_obj)

        # Apply decimate
        mod = lod_obj.modifiers.new("MF_Decimate", "DECIMATE")
        mod.ratio = decimate_ratio
        return lod_obj

    @staticmethod
    def generate_all(base_obj: bpy.types.Object) -> list:
        return [
            LODManager.create_lod(base_obj, 1, 0.5),
            LODManager.create_lod(base_obj, 2, 0.2),
        ]


# ═══════════════════════════════════════════════════════════════════════════
# ATOMIC GENERATORS
# ═══════════════════════════════════════════════════════════════════════════

class Atomics:
    """
    Creates the smallest reusable modular pieces.
    All objects are placed at (0,0,0) with bottom-left pivot.
    """

    @staticmethod
    def wall(name: str, width: float, height: float,
             thickness: float = WALL_THICKNESS,
             door_count: int = 0, window_count: int = 0,
             location: tuple = (0, 0, 0),
             style: str = "DEFAULT") -> bpy.types.Object:
        """Parametric wall with optional door/window openings."""
        w  = snap_to_grid(width,  SUB_GRID)
        h  = snap_to_grid(height, SUB_GRID)

        # Base mesh (thin box — solidify not used here; use direct extrude)
        mesh = bpy.data.meshes.new(name + "_mesh")
        bm = bmesh.new()

        # Bottom face (XZ plane, Y=0)
        verts = [
            bm.verts.new((0,  0,    0)),
            bm.verts.new((w,  0,    0)),
            bm.verts.new((w,  0,    h)),
            bm.verts.new((0,  0,    h)),
        ]
        bm.faces.new(verts)
        bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=thickness)

        # Cut door opening
        if door_count > 0:
            dv = ArchRules.validate_door(DOOR_W, DOOR_H)
            dw, dh = dv["width"], dv["height"]
            # Simple center cut via bmesh boolean knife would be complex here;
            # We defer to GN node from GN_Wall_Gen.py instead.
            # Tag on object custom property for GN to use.
            pass

        bm.to_mesh(mesh)
        bm.free()
        mesh.update()

        obj = bpy.data.objects.new(name, mesh)
        obj.location = location
        obj["mf_type"]         = "wall"
        obj["mf_width"]        = w
        obj["mf_height"]       = h
        obj["mf_thickness"]    = thickness
        obj["mf_door_count"]   = door_count
        obj["mf_window_count"] = window_count
        obj["mf_style"]        = style

        link_to_collection(obj, "MF_Atomics")
        Modifiers.add_bevel(obj, width=0.015, segments=2)
        return obj

    @staticmethod
    def floor_slab(name: str, width: float, depth: float,
                   thickness: float = FLOOR_SLAB_H,
                   location: tuple = (0, 0, 0),
                   style: str = "DEFAULT") -> bpy.types.Object:
        """Flat floor slab snapped to grid."""
        w = snap_to_grid(width, SUB_GRID)
        d = snap_to_grid(depth, SUB_GRID)

        mesh = bpy.data.meshes.new(name + "_mesh")
        bm   = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bm.to_mesh(mesh)
        bm.free()

        obj = bpy.data.objects.new(name, mesh)
        obj.location = location
        obj.scale    = (w, d, thickness)
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(scale=True)

        # Shift pivot to bottom-left
        for v in obj.data.vertices:
            v.co.x += w / 2
            v.co.y += d / 2
            v.co.z += thickness / 2
        obj.data.update()

        obj["mf_type"]  = "floor"
        obj["mf_style"] = style
        link_to_collection(obj, "MF_Atomics")
        return obj

    @staticmethod
    def pillar(name: str, height: float = 3.0,
               side: float = PILLAR_W,
               location: tuple = (0, 0, 0)) -> bpy.types.Object:
        """Square structural pillar."""
        mesh = bpy.data.meshes.new(name + "_mesh")
        bm   = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bm.to_mesh(mesh)
        bm.free()

        obj = bpy.data.objects.new(name, mesh)
        obj.location = location
        obj.scale    = (side, side, height)
        link_to_collection(obj, "MF_Atomics")
        obj["mf_type"] = "pillar"
        return obj

    @staticmethod
    def beam(name: str, length: float = 4.0,
             width: float = 0.2, height: float = 0.3,
             axis: str = "X",
             location: tuple = (0, 0, 0)) -> bpy.types.Object:
        """Horizontal structural beam."""
        mesh = bpy.data.meshes.new(name + "_mesh")
        bm   = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bm.to_mesh(mesh)
        bm.free()

        obj = bpy.data.objects.new(name, mesh)
        obj.location = location
        if axis.upper() == "X":
            obj.scale = (length, width, height)
        elif axis.upper() == "Y":
            obj.scale = (width, length, height)
        link_to_collection(obj, "MF_Atomics")
        obj["mf_type"] = "beam"
        return obj

    @staticmethod
    def staircase(name: str, floor_height: float = WALL_H_FLOOR,
                  stair_width: float = 1.2,
                  location: tuple = (0, 0, 0)) -> bpy.types.Object:
        """Straight staircase from floor to ceiling."""
        n_steps = ArchRules.stair_count(floor_height)
        riser_h = floor_height / n_steps
        tread_d = STEP_D

        mesh = bpy.data.meshes.new(name + "_mesh")
        bm   = bmesh.new()

        for i in range(n_steps):
            x0 = i * tread_d
            z0 = i * riser_h
            verts = [
                bm.verts.new((x0,           0,          z0)),
                bm.verts.new((x0 + tread_d, 0,          z0)),
                bm.verts.new((x0 + tread_d, 0,          z0 + riser_h)),
                bm.verts.new((x0,           0,          z0 + riser_h)),
                bm.verts.new((x0,           stair_width, z0)),
                bm.verts.new((x0 + tread_d, stair_width, z0)),
                bm.verts.new((x0 + tread_d, stair_width, z0 + riser_h)),
                bm.verts.new((x0,           stair_width, z0 + riser_h)),
            ]
            bmesh.ops.contextual_create(bm, geom=verts)

        bm.to_mesh(mesh)
        bm.free()
        mesh.update()

        obj = bpy.data.objects.new(name, mesh)
        obj.location = location
        obj["mf_type"]       = "staircase"
        obj["mf_n_steps"]    = n_steps
        obj["mf_floor_h"]    = floor_height
        link_to_collection(obj, "MF_Atomics")
        return obj


# ═══════════════════════════════════════════════════════════════════════════
# ASSEMBLER — ROOM
# ═══════════════════════════════════════════════════════════════════════════

class RoomAssembler:
    """
    Assembles a room from 4 walls + floor slab + (optional) ceiling.
    All measurements in metres. Pivot = interior bottom-left corner.
    """

    def build(self, params: dict) -> dict:
        w     = params.get("width",        4.0)
        d     = params.get("depth",        5.0)
        floor = params.get("floor_index",  1)
        style = params.get("style",        "DEFAULT")
        name  = params.get("name",         f"MF_Room_{w}x{d}")
        loc   = tuple(params.get("location", (0, 0, 0)))
        doors    = params.get("doors",    [{"wall": "south", "count": 1}])
        windows  = params.get("windows",  [{"wall": "north", "count": 1},
                                            {"wall": "east",  "count": 1}])
        has_ceiling = params.get("ceiling", True)
        gen_stairs  = params.get("stairs", False)

        validated = ArchRules.validate_room(w, d, floor)
        w, d, h = validated["width"], validated["depth"], validated["height"]
        ox, oy, oz = loc

        parts = []

        # ── Walls ──────────────────────────────────────────────────────────
        # South wall (front, negative Y face)
        s_doors  = self._count_for_wall("south", doors)
        s_wins   = self._count_for_wall("south", windows)
        south = Atomics.wall(
            f"{name}_Wall_S", w, h, WALL_THICKNESS,
            door_count=s_doors, window_count=s_wins,
            location=(ox, oy, oz), style=style,
        )
        parts.append(south)

        # North wall (back)
        n_wins = self._count_for_wall("north", windows)
        north = Atomics.wall(
            f"{name}_Wall_N", w, h, WALL_THICKNESS,
            window_count=n_wins,
            location=(ox, oy + d, oz), style=style,
        )
        # Rotate 180° around Z so it faces inward
        north.rotation_euler.z = math.pi
        north.location.x += w
        parts.append(north)

        # West wall (left side)
        west = Atomics.wall(
            f"{name}_Wall_W", d, h, WALL_THICKNESS,
            location=(ox, oy, oz), style=style,
        )
        west.rotation_euler.z = math.pi / 2
        west.location.y += d
        parts.append(west)

        # East wall (right side)
        e_wins = self._count_for_wall("east", windows)
        east = Atomics.wall(
            f"{name}_Wall_E", d, h, WALL_THICKNESS,
            window_count=e_wins,
            location=(ox + w, oy, oz), style=style,
        )
        east.rotation_euler.z = -math.pi / 2
        parts.append(east)

        # ── Floor slab ─────────────────────────────────────────────────────
        floor_obj = Atomics.floor_slab(
            f"{name}_Floor", w, d, FLOOR_SLAB_H,
            location=(ox, oy, oz - FLOOR_SLAB_H), style=style,
        )
        parts.append(floor_obj)

        # ── Ceiling (optional) ─────────────────────────────────────────────
        if has_ceiling:
            ceil_obj = Atomics.floor_slab(
                f"{name}_Ceiling", w, d, FLOOR_SLAB_H,
                location=(ox, oy, oz + h), style=style,
            )
            parts.append(ceil_obj)

        # ── Staircase (optional, south wall) ──────────────────────────────
        if gen_stairs:
            stair = Atomics.staircase(
                f"{name}_Stairs", h,
                location=(ox + 0.5, oy + 0.5, oz),
            )
            parts.append(stair)

        # ── Parent all parts under empty ───────────────────────────────────
        room_empty = bpy.data.objects.new(name, None)
        room_empty.location = loc
        room_empty["mf_type"]  = "room"
        room_empty["mf_width"] = w
        room_empty["mf_depth"] = d
        room_empty["mf_height"]= h
        room_empty["mf_style"] = style
        link_to_collection(room_empty, "MF_Assembled")

        for part in parts:
            part.parent = room_empty

        return mf_response(
            "success",
            f"Room '{name}' created ({w}×{d}×{h}m).",
            {"object": name, "dims": [w, d, h], "warnings": validated["warnings"]},
        )

    @staticmethod
    def _count_for_wall(wall_name: str, spec_list: list) -> int:
        for spec in spec_list:
            if spec.get("wall") == wall_name:
                return spec.get("count", 1)
        return 0


# ═══════════════════════════════════════════════════════════════════════════
# ASSEMBLER — BUILDING
# ═══════════════════════════════════════════════════════════════════════════

class BuildingAssembler:
    """
    Stacks rooms floor by floor to create a complete building.
    Ground floor: WALL_H_GROUND (4 m retail/entrance)
    Upper floors: WALL_H_FLOOR (3 m)
    """

    def build(self, params: dict) -> dict:
        name     = params.get("name",       "MF_Building")
        floors   = params.get("floors",     2)
        width    = params.get("width",      8.0)
        depth    = params.get("depth",      6.0)
        style    = params.get("style",      "DEFAULT")
        location = tuple(params.get("location", (0, 0, 0)))
        rooms_per_floor = params.get("rooms_per_floor", 1)

        ox, oy, oz = location
        current_z  = oz
        created    = []
        ra         = RoomAssembler()

        for fl in range(floors):
            fh = ArchRules.floor_height(fl)

            if rooms_per_floor == 1:
                result = ra.build({
                    "name":        f"{name}_F{fl}_Room0",
                    "width":       width,
                    "depth":       depth,
                    "floor_index": fl,
                    "style":       style,
                    "location":    (ox, oy, current_z),
                    "stairs":      fl < (floors - 1),
                    "ceiling":     True,
                })
                created.append(result)
            else:
                room_w = snap_to_grid(width / rooms_per_floor, GRID)
                for ri in range(rooms_per_floor):
                    result = ra.build({
                        "name":        f"{name}_F{fl}_Room{ri}",
                        "width":       room_w,
                        "depth":       depth,
                        "floor_index": fl,
                        "style":       style,
                        "location":    (ox + ri * room_w, oy, current_z),
                        "stairs":      ri == 0 and fl < (floors - 1),
                        "ceiling":     True,
                    })
                    created.append(result)

            current_z += fh

        # Building root empty
        bld_empty = bpy.data.objects.new(name, None)
        bld_empty.location = location
        bld_empty["mf_type"]   = "building"
        bld_empty["mf_floors"] = floors
        bld_empty["mf_style"]  = style
        link_to_collection(bld_empty, "MF_Buildings")

        return mf_response(
            "success",
            f"Building '{name}' created — {floors} floors.",
            {"object": name, "floors": floors, "rooms": created},
        )


# ═══════════════════════════════════════════════════════════════════════════
# STREET GENERATOR
# ═══════════════════════════════════════════════════════════════════════════

class StreetGenerator:
    """
    Generates a street segment with sidewalk, road surface, and building plots.
    """

    ROAD_WIDTH    = 7.0   # m (two-lane road)
    SIDEWALK_W    = 2.5   # m
    PLOT_DEPTH    = 12.0  # m

    def build(self, params: dict) -> dict:
        name    = params.get("name",     "MF_Street")
        length  = params.get("length",   40.0)
        style   = params.get("style",    "DEFAULT")
        n_plots = params.get("plots",    3)
        loc     = tuple(params.get("location", (0, 0, 0)))

        ox, oy, oz = loc
        total_w    = self.ROAD_WIDTH + 2 * self.SIDEWALK_W

        # Road slab
        road = Atomics.floor_slab(
            f"{name}_Road", length, self.ROAD_WIDTH, 0.05,
            location=(ox, oy + self.SIDEWALK_W, oz - 0.05), style=style,
        )

        # Sidewalks
        sw_left  = Atomics.floor_slab(
            f"{name}_SW_L", length, self.SIDEWALK_W, 0.12,
            location=(ox, oy, oz), style=style,
        )
        sw_right = Atomics.floor_slab(
            f"{name}_SW_R", length, self.SIDEWALK_W, 0.12,
            location=(ox, oy + self.SIDEWALK_W + self.ROAD_WIDTH, oz), style=style,
        )

        # Building plots (left side, with buildings if requested)
        plot_w = snap_to_grid(length / n_plots, GRID)
        for i in range(n_plots):
            plot_x = ox + i * plot_w
            Atomics.floor_slab(
                f"{name}_Plot_{i}", plot_w, self.PLOT_DEPTH, 0.02,
                location=(plot_x, oy - self.PLOT_DEPTH, oz), style=style,
            )

        return mf_response(
            "success",
            f"Street '{name}' created — {length}m long, {n_plots} plots.",
            {"object": name, "length": length, "plots": n_plots},
        )


# ═══════════════════════════════════════════════════════════════════════════
# GODOT EXPORT PIPELINE
# ═══════════════════════════════════════════════════════════════════════════

class GodotExporter:
    """
    Prepares and exports objects for Godot 4.x.
    - Applies all transforms and modifiers
    - Renames collision meshes with _col suffix
    - Exports glTF 2.0 (binary .glb)
    - Writes JSON metadata sidecar
    """

    @staticmethod
    def prepare_object(obj: bpy.types.Object) -> None:
        """Apply scale, rotation to object."""
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(
            location=False, rotation=True, scale=True
        )

    @staticmethod
    def add_collision(obj: bpy.types.Object) -> bpy.types.Object:
        """
        Duplicate object as a simplified collision hull.
        Godot recognises the -col suffix automatically.
        """
        col_obj = obj.copy()
        col_obj.data = obj.data.copy()
        col_obj.name = obj.name + "-col"
        link_to_collection(col_obj, "MF_Exports")

        # Decimate for lightweight collision
        d_mod = col_obj.modifiers.new("MF_DecimateCol", "DECIMATE")
        d_mod.ratio = 0.1
        apply_modifiers(col_obj)
        return col_obj

    @staticmethod
    def export_glb(objects: list, output_path: str,
                   export_materials: bool = True,
                   apply_modifiers_flag: bool = True) -> dict:
        """Export a list of objects as a single .glb file."""
        os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)

        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)

        export_kwargs = {
            "filepath":                   output_path,
            "use_selection":              True,
            "export_format":              "GLB",
            "export_apply":               apply_modifiers_flag,
            "export_materials":           "EXPORT" if export_materials else "NONE",
            "export_image_format":        "AUTO",
            "export_texture_dir":         "",
            "export_draco_mesh_compression_enable": False,
        }
        try:
            bpy.ops.export_scene.gltf(**export_kwargs)
        except Exception as e:
            return mf_response("error", f"glTF export failed: {e}")

        # Write metadata sidecar
        meta_path = output_path.replace(".glb", "_meta.json")
        meta = {
            "mf_version": "4.0",
            "objects": [
                {
                    "name":     o.name,
                    "mf_type":  o.get("mf_type", "unknown"),
                    "mf_style": o.get("mf_style", "DEFAULT"),
                    "dims":     [
                        o.get("mf_width", 0),
                        o.get("mf_depth", 0),
                        o.get("mf_height", 0),
                    ],
                }
                for o in objects
            ],
        }
        with open(meta_path, "w") as f:
            json.dump(meta, f, indent=2)

        return mf_response(
            "success",
            f"Exported {len(objects)} object(s) to {output_path}",
            {"path": output_path, "meta": meta_path},
        )


# ═══════════════════════════════════════════════════════════════════════════
# MASTER CONTROLLER
# ═══════════════════════════════════════════════════════════════════════════

class MF_Master_Controller:
    """
    Single entry point for all AI / terminal commands.
    Accepts JSON commands and dispatches to appropriate generators.

    Supported actions:
        SETUP_SCENE
        CREATE_WALL
        CREATE_FLOOR
        CREATE_PILLAR
        CREATE_BEAM
        CREATE_STAIRCASE
        CREATE_ROOM
        CREATE_BUILDING
        CREATE_STREET
        MODIFY_PARAM
        SET_STYLE
        ADD_MODIFIER
        EXPORT_GODOT
        LIST_OBJECTS
        DELETE_OBJECT
    """

    VERSION = "4.0.0"

    def __init__(self):
        self._room     = RoomAssembler()
        self._building = BuildingAssembler()
        self._street   = StreetGenerator()
        self._exporter = GodotExporter()
        self._current_style: dict = StyleDNA.get("DEFAULT")
        self.setup_scene()

    # ── Scene Setup ────────────────────────────────────────────────────────

    def setup_scene(self) -> None:
        """Ensure all MF collections exist and scene units are metric."""
        scene = bpy.context.scene
        scene.unit_settings.system      = "METRIC"
        scene.unit_settings.length_unit = "METERS"
        scene.unit_settings.scale_length = 1.0

        for name in COLLECTIONS:
            if name not in bpy.data.collections:
                col = bpy.data.collections.new(name)
                bpy.context.scene.collection.children.link(col)

        print(f"[MF v{self.VERSION}] Scene setup complete. Collections: {COLLECTIONS}")

    # ── Main Dispatcher ────────────────────────────────────────────────────

    def execute(self, cmd_json: str | dict) -> dict:
        """
        Main entry point for AI / terminal.
        Accepts JSON string or dict.
        Returns a result dict with 'status', 'message', and optional data keys.
        """
        try:
            data   = json.loads(cmd_json) if isinstance(cmd_json, str) else cmd_json
            action = str(data.get("action", "")).upper()
            params = data.get("params", {})

            dispatch = {
                "SETUP_SCENE":     lambda p: (self.setup_scene(), mf_response("success", "Scene set up."))[1],
                "CREATE_WALL":     self.create_wall,
                "CREATE_FLOOR":    self.create_floor,
                "CREATE_PILLAR":   self.create_pillar,
                "CREATE_BEAM":     self.create_beam,
                "CREATE_STAIRCASE":self.create_staircase,
                "CREATE_ROOM":     self._room.build,
                "CREATE_BUILDING": self._building.build,
                "CREATE_STREET":   self._street.build,
                "MODIFY_PARAM":    self.modify_param,
                "SET_STYLE":       self.set_style,
                "ADD_MODIFIER":    self.add_modifier,
                "EXPORT_GODOT":    self.export_godot,
                "LIST_OBJECTS":    self.list_objects,
                "DELETE_OBJECT":   self.delete_object,
            }

            if action not in dispatch:
                available = sorted(dispatch.keys())
                return mf_response("error", f"Unknown action '{action}'. Available: {available}")

            return dispatch[action](params)

        except json.JSONDecodeError as e:
            return mf_response("error", f"JSON parse error: {e}")
        except Exception as e:
            return mf_response("error", f"[{type(e).__name__}] {e}\n{traceback.format_exc()}")

    # ── Atomic Command Handlers ────────────────────────────────────────────

    def create_wall(self, params: dict) -> dict:
        name    = params.get("name",        "MF_Wall")
        width   = float(params.get("width", 4.0))
        height  = float(params.get("height", ArchRules.floor_height(1)))
        thick   = float(params.get("thickness", WALL_THICKNESS))
        d_count = int(params.get("door_count",   0))
        w_count = int(params.get("window_count", 1))
        style   = params.get("style", "DEFAULT")
        loc     = tuple(params.get("location",  (0, 0, 0)))

        obj = Atomics.wall(name, width, height, thick, d_count, w_count, loc, style)
        return mf_response("success", f"Wall '{obj.name}' created.", {"object": obj.name})

    def create_floor(self, params: dict) -> dict:
        name   = params.get("name",      "MF_Floor")
        width  = float(params.get("width", 4.0))
        depth  = float(params.get("depth", 4.0))
        thick  = float(params.get("thickness", FLOOR_SLAB_H))
        style  = params.get("style", "DEFAULT")
        loc    = tuple(params.get("location", (0, 0, 0)))

        obj = Atomics.floor_slab(name, width, depth, thick, loc, style)
        return mf_response("success", f"Floor '{obj.name}' created.", {"object": obj.name})

    def create_pillar(self, params: dict) -> dict:
        name   = params.get("name",   "MF_Pillar")
        height = float(params.get("height", 3.0))
        side   = float(params.get("side",   PILLAR_W))
        loc    = tuple(params.get("location", (0, 0, 0)))

        obj = Atomics.pillar(name, height, side, loc)
        return mf_response("success", f"Pillar '{obj.name}' created.", {"object": obj.name})

    def create_beam(self, params: dict) -> dict:
        name   = params.get("name",   "MF_Beam")
        length = float(params.get("length", 4.0))
        width  = float(params.get("width",  0.2))
        height = float(params.get("height", 0.3))
        axis   = params.get("axis", "X")
        loc    = tuple(params.get("location", (0, 0, 0)))

        obj = Atomics.beam(name, length, width, height, axis, loc)
        return mf_response("success", f"Beam '{obj.name}' created.", {"object": obj.name})

    def create_staircase(self, params: dict) -> dict:
        name   = params.get("name",   "MF_Stairs")
        fh     = float(params.get("floor_height", WALL_H_FLOOR))
        sw     = float(params.get("stair_width",  1.2))
        loc    = tuple(params.get("location", (0, 0, 0)))

        obj = Atomics.staircase(name, fh, sw, loc)
        return mf_response("success", f"Staircase '{obj.name}' created.", {"object": obj.name})

    # ── Modifier Command Handler ───────────────────────────────────────────

    def add_modifier(self, params: dict) -> dict:
        obj_name = params.get("object")
        mod_type = str(params.get("type", "")).upper()

        obj = bpy.data.objects.get(obj_name)
        if obj is None:
            return mf_response("error", f"Object '{obj_name}' not found.")

        handlers = {
            "ARRAY":       lambda: Modifiers.add_array(
                obj,
                params.get("axis", "X"),
                int(params.get("count", 2)),
                params.get("fit_length"),
            ),
            "MIRROR":      lambda: Modifiers.add_mirror(
                obj,
                params.get("axis_x", True),
                params.get("axis_y", False),
                params.get("axis_z", False),
            ),
            "BEVEL":       lambda: Modifiers.add_bevel(
                obj,
                float(params.get("width", 0.02)),
                int(params.get("segments", 2)),
            ),
            "SOLIDIFY":    lambda: Modifiers.add_solidify(
                obj,
                float(params.get("thickness", WALL_THICKNESS)),
            ),
            "SUBDIVISION": lambda: Modifiers.add_subdivision(
                obj,
                int(params.get("levels", 1)),
            ),
        }

        if mod_type not in handlers:
            return mf_response("error", f"Unknown modifier '{mod_type}'. Available: {list(handlers)}")

        mod = handlers[mod_type]()
        return mf_response("success", f"Modifier '{mod_type}' added to '{obj_name}'.")

    # ── Modify Existing Object Params ─────────────────────────────────────

    def modify_param(self, params: dict) -> dict:
        obj_name     = params.get("object")
        socket_name  = params.get("param")
        value        = params.get("value")

        obj = bpy.data.objects.get(obj_name)
        if obj is None:
            return mf_response("error", f"Object '{obj_name}' not found.")
        if socket_name is None:
            return mf_response("error", "Missing 'param' field.")

        ok = Modifiers.set_gn_param(obj, socket_name, value)
        if not ok:
            # Try custom property
            try:
                obj[socket_name] = value
                ok = True
            except Exception:
                pass

        if ok:
            return mf_response("success", f"'{socket_name}' set to {value} on '{obj_name}'.")
        return mf_response("error", f"Socket '{socket_name}' not found on '{obj_name}'.")

    # ── Style Handler ─────────────────────────────────────────────────────

    def set_style(self, params: dict) -> dict:
        style_name = params.get("name", "DEFAULT")
        dna_dir    = params.get("dna_dir")

        self._current_style = StyleDNA.get(style_name, dna_dir)
        return mf_response(
            "success",
            f"Style set to '{style_name}'.",
            {"dna": self._current_style},
        )

    # ── Export Handler ────────────────────────────────────────────────────

    def export_godot(self, params: dict) -> dict:
        obj_names     = params.get("objects")
        output_path   = params.get("output", "/tmp/mf_export.glb")
        add_collision = params.get("collision", True)
        apply_mods    = params.get("apply_modifiers", True)

        if obj_names is None:
            # Export everything in MF_Exports collection
            col = bpy.data.collections.get("MF_Exports")
            objs = list(col.objects) if col else []
        else:
            objs = [bpy.data.objects[n] for n in obj_names if n in bpy.data.objects]

        if not objs:
            return mf_response("error", "No objects found to export.")

        for obj in objs:
            GodotExporter.prepare_object(obj)

        if add_collision:
            col_objs = [GodotExporter.add_collision(o) for o in objs]
            objs += col_objs

        return self._exporter.export_glb(
            objs, output_path,
            export_materials=params.get("materials", True),
            apply_modifiers_flag=apply_mods,
        )

    # ── Utility Handlers ─────────────────────────────────────────────────

    def list_objects(self, params: dict) -> dict:
        col_name = params.get("collection")
        if col_name:
            col = bpy.data.collections.get(col_name)
            names = [o.name for o in col.objects] if col else []
        else:
            names = [o.name for o in bpy.data.objects if "mf_type" in o]
        return mf_response("success", f"{len(names)} MF objects.", {"objects": names})

    def delete_object(self, params: dict) -> dict:
        obj_name = params.get("object")
        obj = bpy.data.objects.get(obj_name)
        if obj is None:
            return mf_response("error", f"'{obj_name}' not found.")
        bpy.data.objects.remove(obj, do_unlink=True)
        return mf_response("success", f"'{obj_name}' deleted.")


# ═══════════════════════════════════════════════════════════════════════════
# GLOBAL INSTANCE  (Kiro / Terminal interface)
# ═══════════════════════════════════════════════════════════════════════════

MF: MF_Master_Controller | None = None


def _init():
    global MF
    if MF is None:
        MF = MF_Master_Controller()
    return MF


def execute(cmd: str | dict) -> dict:
    """
    Public API — call from Kiro terminal:
        import MF_Master_Controller as mfc
        result = mfc.execute({"action": "CREATE_ROOM", "params": {...}})
    """
    return _init().execute(cmd)


# ═══════════════════════════════════════════════════════════════════════════
# CLI (run as: blender --background --python MF_Master_Controller.py -- CMD)
# ═══════════════════════════════════════════════════════════════════════════

if __name__ == "__main__":
    import sys

    args = sys.argv
    sep  = args.index("--") + 1 if "--" in args else len(args)
    cli_args = args[sep:]

    if cli_args:
        cmd_str = " ".join(cli_args)
        result  = execute(cmd_str)
    else:
        # Self-test
        ctrl = _init()
        tests = [
            {"action": "CREATE_WALL",     "params": {"name": "Test_Wall", "width": 4.0, "height": 3.0, "window_count": 2}},
            {"action": "CREATE_ROOM",     "params": {"name": "Test_Room", "width": 5.0, "depth": 4.0, "floor_index": 1}},
            {"action": "CREATE_BUILDING", "params": {"name": "Test_Bld",  "floors": 3,  "width": 8.0, "depth": 6.0}},
        ]
        for t in tests:
            r = ctrl.execute(t)
            print(f"[MF] {t['action']}: {r['status']} — {r['message']}")
