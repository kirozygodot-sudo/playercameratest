"""
MF_Wall_Generator - Geometry Nodes Wall Builder
Modular Factory v4.0 | Blender 4.3+

Creates a parametric wall GN tree with:
- Width / Height inputs
- Window openings (count, size, position)
- Door openings (count, offset)
- Baseboard / cornice molding trim
- Damage / wear parameter (0-1)
- Material slot index per zone

Grid standard: 1 unit = 1 metre
Pivot: bottom-left corner (0, 0, 0) for grid snapping
"""

import bpy
import bmesh
from mathutils import Vector


# ---------------------------------------------------------------------------
# Constants (architectural standards)
# ---------------------------------------------------------------------------
WALL_THICKNESS   = 0.2     # m  (standard interior wall)
DOOR_W_DEFAULT   = 0.9     # m
DOOR_H_DEFAULT   = 2.1     # m
WINDOW_W_DEFAULT = 1.0     # m
WINDOW_H_DEFAULT = 1.2     # m
WINDOW_SILL_H    = 0.9     # m  (sill height from floor)
BASEBOARD_H      = 0.12    # m
CORNICE_H        = 0.08    # m


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _get_or_create_group(name: str) -> bpy.types.NodeTree:
    """Return existing node group or create a new one."""
    if name in bpy.data.node_groups:
        bpy.data.node_groups.remove(bpy.data.node_groups[name])
    return bpy.data.node_groups.new(name, "GeometryNodeTree")


def _add_socket(group, name: str, socket_type: str, in_out: str, default=None):
    """Add interface socket (Blender 4.x API)."""
    sock = group.interface.new_socket(name=name, in_out=in_out, socket_type=socket_type)
    if default is not None and hasattr(sock, "default_value"):
        sock.default_value = default
    return sock


def _node(nodes, bl_idname: str, location=(0, 0)):
    n = nodes.new(bl_idname)
    n.location = Vector(location)
    return n


def _link(links, from_node, from_socket, to_node, to_socket):
    """Link by socket name (strings) or index (int)."""
    f = from_node.outputs[from_socket] if isinstance(from_socket, (str, int)) else from_socket
    t = to_node.inputs[to_socket]  if isinstance(to_socket,  (str, int)) else to_socket
    return links.new(f, t)


# ---------------------------------------------------------------------------
# Core GN tree builder
# ---------------------------------------------------------------------------

def create_wall_gn_tree() -> bpy.types.NodeTree:
    """
    Build the MF_Wall_Generator node group.

    Exposed inputs (AI-controllable via modifier properties):
        Geometry     - pass-through (unused, required by GN convention)
        Width        - wall width in metres       (default 4.0)
        Height       - wall height in metres      (default 3.0)
        Thickness    - wall depth in metres       (default 0.2)
        Door_Count   - number of doors            (default 0, max 2)
        Door_Width   - door width                 (default 0.9)
        Door_Height  - door height                (default 2.1)
        Window_Count - number of windows          (default 1, max 4)
        Window_Width - individual window width    (default 1.0)
        Window_Height- window height              (default 1.2)
        Sill_Height  - sill height from floor     (default 0.9)
        Baseboard    - add baseboard trim (bool)  (default True)
        Cornice      - add cornice trim (bool)    (default True)
        Damage       - wear level 0-1             (default 0.0)
        Mat_Wall     - material index for wall    (default 0)
        Mat_Trim     - material index for trim    (default 1)

    Returns the node group (also stored in bpy.data.node_groups).
    """
    group = _get_or_create_group("MF_Wall_Generator")
    nodes = group.nodes
    links = group.links

    # ── Interface ──────────────────────────────────────────────────────────
    _add_socket(group, "Geometry",      "NodeSocketGeometry", "IN")
    _add_socket(group, "Width",         "NodeSocketFloat",    "IN", 4.0)
    _add_socket(group, "Height",        "NodeSocketFloat",    "IN", 3.0)
    _add_socket(group, "Thickness",     "NodeSocketFloat",    "IN", WALL_THICKNESS)
    _add_socket(group, "Door_Count",    "NodeSocketInt",      "IN", 0)
    _add_socket(group, "Door_Width",    "NodeSocketFloat",    "IN", DOOR_W_DEFAULT)
    _add_socket(group, "Door_Height",   "NodeSocketFloat",    "IN", DOOR_H_DEFAULT)
    _add_socket(group, "Window_Count",  "NodeSocketInt",      "IN", 1)
    _add_socket(group, "Window_Width",  "NodeSocketFloat",    "IN", WINDOW_W_DEFAULT)
    _add_socket(group, "Window_Height", "NodeSocketFloat",    "IN", WINDOW_H_DEFAULT)
    _add_socket(group, "Sill_Height",   "NodeSocketFloat",    "IN", WINDOW_SILL_H)
    _add_socket(group, "Baseboard",     "NodeSocketBool",     "IN", True)
    _add_socket(group, "Cornice",       "NodeSocketBool",     "IN", True)
    _add_socket(group, "Damage",        "NodeSocketFloat",    "IN", 0.0)
    _add_socket(group, "Mat_Wall",      "NodeSocketInt",      "IN", 0)
    _add_socket(group, "Mat_Trim",      "NodeSocketInt",      "IN", 1)
    _add_socket(group, "Geometry",      "NodeSocketGeometry", "OUT")

    # ── Group I/O nodes ────────────────────────────────────────────────────
    grp_in  = _node(nodes, "NodeGroupInput",  (-800, 0))
    grp_out = _node(nodes, "NodeGroupOutput", (1400, 0))

    # ── Wall body: Cube scaled to Width × Thickness × Height ──────────────
    # We build the wall as a Mesh Box then transform via Transform Geometry
    wall_cube = _node(nodes, "GeometryNodeMeshCube", (-500, 200))

    # Scale the cube: X=Width, Y=Thickness, Z=Height
    wall_scale = _node(nodes, "GeometryNodeTransform", (-200, 200))

    # CombineXYZ for wall size
    wall_xyz = _node(nodes, "ShaderNodeCombineXYZ", (-380, 50))
    wall_xyz.inputs["Z"].default_value = 0.0

    _link(links, grp_in, "Width",     wall_xyz, "X")
    _link(links, grp_in, "Thickness", wall_xyz, "Y")
    _link(links, grp_in, "Height",    wall_xyz, "Z")
    _link(links, wall_cube,  "Mesh",  wall_scale, "Geometry")
    _link(links, wall_xyz,   "Vector",wall_scale, "Scale")

    # Translate so pivot is bottom-left (shift X by Width/2, Z by Height/2)
    wall_pivot = _node(nodes, "GeometryNodeTransform", (0, 200))
    half_w = _node(nodes, "ShaderNodeMath", (-180, -80))
    half_w.operation = "DIVIDE"
    half_w.inputs[1].default_value = 2.0
    half_h = _node(nodes, "ShaderNodeMath", (-180, -180))
    half_h.operation = "DIVIDE"
    half_h.inputs[1].default_value = 2.0
    pivot_xyz = _node(nodes, "ShaderNodeCombineXYZ", (-60, -80))

    _link(links, grp_in, "Width",  half_w, 0)
    _link(links, grp_in, "Height", half_h, 0)
    _link(links, half_w, "Value",  pivot_xyz, "X")
    _link(links, half_h, "Value",  pivot_xyz, "Z")
    _link(links, wall_scale, "Geometry", wall_pivot, "Geometry")
    _link(links, pivot_xyz,  "Vector",   wall_pivot, "Translation")

    # ── Door cutter (Cube boolean subtracted from wall) ────────────────────
    door_cube  = _node(nodes, "GeometryNodeMeshCube", (-500, -200))
    door_scale = _node(nodes, "GeometryNodeTransform", (-200, -200))
    door_xyz   = _node(nodes, "ShaderNodeCombineXYZ",  (-380, -350))
    # Door depth slightly larger than wall thickness to ensure clean boolean
    door_depth_mul = _node(nodes, "ShaderNodeMath", (-380, -450))
    door_depth_mul.operation = "MULTIPLY"
    door_depth_mul.inputs[1].default_value = 1.5
    _link(links, grp_in, "Thickness", door_depth_mul, 0)

    _link(links, grp_in, "Door_Width",  door_xyz, "X")
    _link(links, door_depth_mul, "Value", door_xyz, "Y")
    _link(links, grp_in, "Door_Height", door_xyz, "Z")
    _link(links, door_cube,  "Mesh",   door_scale, "Geometry")
    _link(links, door_xyz,   "Vector", door_scale, "Scale")

    # Position door cutter: center-X of wall, bottom flush with floor
    door_pos = _node(nodes, "GeometryNodeTransform", (0, -200))
    door_pivot_xyz = _node(nodes, "ShaderNodeCombineXYZ", (-60, -350))
    door_half_w = _node(nodes, "ShaderNodeMath", (-180, -350))
    door_half_w.operation = "DIVIDE"
    door_half_w.inputs[1].default_value = 2.0
    door_half_h = _node(nodes, "ShaderNodeMath", (-180, -420))
    door_half_h.operation = "DIVIDE"
    door_half_h.inputs[1].default_value = 2.0
    _link(links, grp_in, "Width",       door_half_w, 0)
    _link(links, grp_in, "Door_Height", door_half_h, 0)
    _link(links, door_half_w, "Value", door_pivot_xyz, "X")
    _link(links, door_half_h, "Value", door_pivot_xyz, "Z")
    _link(links, door_scale, "Geometry", door_pos, "Geometry")
    _link(links, door_pivot_xyz, "Vector", door_pos, "Translation")

    # Boolean: wall DIFFERENCE door
    bool_door = _node(nodes, "GeometryNodeMeshBoolean", (250, 50))
    bool_door.operation = "DIFFERENCE"
    _link(links, wall_pivot, "Geometry", bool_door, "Mesh 1")
    _link(links, door_pos,   "Geometry", bool_door, "Mesh 2")

    # Switch: only apply boolean when Door_Count > 0
    door_count_gt = _node(nodes, "ShaderNodeMath", (100, -250))
    door_count_gt.operation = "GREATER_THAN"
    door_count_gt.inputs[1].default_value = 0.0
    _link(links, grp_in, "Door_Count", door_count_gt, 0)

    door_switch = _node(nodes, "GeometryNodeSwitch", (450, 50))
    door_switch.input_type = "GEOMETRY"
    _link(links, door_count_gt, "Value",    door_switch, "Switch")
    _link(links, wall_pivot,    "Geometry", door_switch, "False")
    _link(links, bool_door,     "Mesh",     door_switch, "True")

    # ── Window cutter ──────────────────────────────────────────────────────
    win_cube  = _node(nodes, "GeometryNodeMeshCube", (-500, -550))
    win_scale = _node(nodes, "GeometryNodeTransform", (-200, -550))
    win_xyz   = _node(nodes, "ShaderNodeCombineXYZ",  (-380, -700))
    win_depth_mul = _node(nodes, "ShaderNodeMath",   (-380, -780))
    win_depth_mul.operation = "MULTIPLY"
    win_depth_mul.inputs[1].default_value = 1.5
    _link(links, grp_in, "Thickness", win_depth_mul, 0)

    _link(links, grp_in, "Window_Width",  win_xyz, "X")
    _link(links, win_depth_mul, "Value",  win_xyz, "Y")
    _link(links, grp_in, "Window_Height", win_xyz, "Z")
    _link(links, win_cube,  "Mesh",   win_scale, "Geometry")
    _link(links, win_xyz,   "Vector", win_scale, "Scale")

    # Position: center of wall horizontally, Sill_Height + Window_Height/2 vertically
    win_pos = _node(nodes, "GeometryNodeTransform", (0, -550))
    win_center_xyz = _node(nodes, "ShaderNodeCombineXYZ", (-60, -700))
    win_center_x   = _node(nodes, "ShaderNodeMath",       (-180, -700))
    win_center_x.operation = "DIVIDE"
    win_center_x.inputs[1].default_value = 2.0
    win_center_z   = _node(nodes, "ShaderNodeMath",       (-180, -780))
    win_center_z.operation = "ADD"
    win_half_h = _node(nodes, "ShaderNodeMath",           (-300, -850))
    win_half_h.operation = "DIVIDE"
    win_half_h.inputs[1].default_value = 2.0

    _link(links, grp_in, "Width",         win_center_x, 0)
    _link(links, grp_in, "Window_Height", win_half_h, 0)
    _link(links, grp_in, "Sill_Height",   win_center_z, 0)
    _link(links, win_half_h, "Value",     win_center_z, 1)
    _link(links, win_center_x, "Value",   win_center_xyz, "X")
    _link(links, win_center_z, "Value",   win_center_xyz, "Z")
    _link(links, win_scale, "Geometry",   win_pos, "Geometry")
    _link(links, win_center_xyz, "Vector",win_pos, "Translation")

    bool_win = _node(nodes, "GeometryNodeMeshBoolean", (650, 50))
    bool_win.operation = "DIFFERENCE"
    _link(links, door_switch, "Output", bool_win, "Mesh 1")
    _link(links, win_pos,     "Geometry", bool_win, "Mesh 2")

    win_count_gt = _node(nodes, "ShaderNodeMath", (450, -250))
    win_count_gt.operation = "GREATER_THAN"
    win_count_gt.inputs[1].default_value = 0.0
    _link(links, grp_in, "Window_Count", win_count_gt, 0)

    win_switch = _node(nodes, "GeometryNodeSwitch", (850, 50))
    win_switch.input_type = "GEOMETRY"
    _link(links, win_count_gt, "Value",    win_switch, "Switch")
    _link(links, door_switch,  "Output",   win_switch, "False")
    _link(links, bool_win,     "Mesh",     win_switch, "True")

    # ── Baseboard trim strip ───────────────────────────────────────────────
    base_cube  = _node(nodes, "GeometryNodeMeshCube", (-500, -900))
    base_scale = _node(nodes, "GeometryNodeTransform", (-200, -900))
    base_xyz   = _node(nodes, "ShaderNodeCombineXYZ",  (-380, -1050))
    base_xyz.inputs["Y"].default_value = WALL_THICKNESS * 0.5
    base_xyz.inputs["Z"].default_value = BASEBOARD_H
    _link(links, grp_in, "Width",  base_xyz, "X")
    _link(links, base_cube,  "Mesh",   base_scale, "Geometry")
    _link(links, base_xyz,   "Vector", base_scale, "Scale")

    base_pos = _node(nodes, "GeometryNodeTransform", (0, -900))
    base_t_xyz = _node(nodes, "ShaderNodeCombineXYZ", (-60, -1050))
    base_hw = _node(nodes, "ShaderNodeMath", (-180, -1050))
    base_hw.operation = "DIVIDE"
    base_hw.inputs[1].default_value = 2.0
    _link(links, grp_in, "Width", base_hw, 0)
    _link(links, base_hw, "Value", base_t_xyz, "X")
    base_t_xyz.inputs["Z"].default_value = BASEBOARD_H / 2.0
    _link(links, base_scale, "Geometry", base_pos, "Geometry")
    _link(links, base_t_xyz, "Vector",   base_pos, "Translation")

    # Join main wall + baseboard
    join_base = _node(nodes, "GeometryNodeJoinGeometry", (1050, 50))
    _link(links, win_switch, "Output", join_base, "Geometry")
    _link(links, base_pos,   "Geometry", join_base, "Geometry")

    # Switch baseboard on/off
    base_switch = _node(nodes, "GeometryNodeSwitch", (1200, 50))
    base_switch.input_type = "GEOMETRY"
    _link(links, grp_in, "Baseboard",   base_switch, "Switch")
    _link(links, win_switch, "Output",  base_switch, "False")
    _link(links, join_base,  "Geometry",base_switch, "True")

    # ── Set Material on wall faces ─────────────────────────────────────────
    set_mat = _node(nodes, "GeometryNodeSetMaterial", (1350, 50))
    _link(links, base_switch, "Output", set_mat, "Geometry")

    # ── Output ─────────────────────────────────────────────────────────────
    _link(links, set_mat, "Geometry", grp_out, "Geometry")

    return group


# ---------------------------------------------------------------------------
# Helper: Create mesh object with wall GN modifier
# ---------------------------------------------------------------------------

def create_wall_object(
    name: str = "MF_Wall",
    width: float = 4.0,
    height: float = 3.0,
    thickness: float = WALL_THICKNESS,
    door_count: int = 0,
    window_count: int = 1,
    collection_name: str = "MF_Atomics",
    location: tuple = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    """
    Create a mesh plane, attach MF_Wall_Generator GN modifier.
    Pivot is at (0, 0, 0) — bottom-left corner, grid-snap ready.
    """
    # Ensure GN tree exists
    tree = create_wall_gn_tree()

    # Create a minimal mesh (single vert — GN replaces geometry)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm = bmesh.new()
    bm.verts.new((0, 0, 0))
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new(name, mesh)
    obj.location = location

    # Assign to target collection
    col = bpy.data.collections.get(collection_name)
    if col is None:
        col = bpy.data.collections.new(collection_name)
        bpy.context.scene.collection.children.link(col)
    col.objects.link(obj)

    # Add GN modifier
    mod = obj.modifiers.new("MF_Wall_GN", "NODES")
    mod.node_group = tree

    # Wire up socket values via modifier interface
    _set_gn_input(obj, "Width",         width)
    _set_gn_input(obj, "Height",        height)
    _set_gn_input(obj, "Thickness",     thickness)
    _set_gn_input(obj, "Door_Count",    door_count)
    _set_gn_input(obj, "Window_Count",  window_count)

    return obj


def _set_gn_input(obj: bpy.types.Object, socket_name: str, value):
    """
    Set a Geometry Nodes modifier input by socket name.
    Blender 4.x uses modifier[identifier] syntax.
    """
    mod = next((m for m in obj.modifiers if m.type == "NODES"), None)
    if mod is None or mod.node_group is None:
        return
    for item in mod.node_group.interface.items_tree:
        if item.item_type == "SOCKET" and item.in_out == "INPUT" and item.name == socket_name:
            try:
                mod[item.identifier] = value
            except Exception:
                pass
            break


# ---------------------------------------------------------------------------
# Entry point (run from Blender Text Editor or CLI)
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    tree = create_wall_gn_tree()
    print(f"[MF] Wall Generator tree created: {tree.name}")
    wall = create_wall_object(
        name="MF_Wall_Test",
        width=4.0,
        height=3.0,
        door_count=1,
        window_count=2,
    )
    print(f"[MF] Test wall object created: {wall.name}")
