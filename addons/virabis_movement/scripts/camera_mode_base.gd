## camera_mode_base.gd
## Tüm kamera modlarının base class'ı.
##
## CameraController sadece bu interface'i bilir — modu içini bilmez.
## Yeni mod = yeni dosya. Mevcut hiçbir koda dokunulmaz.
##
## Şimdiki modlar:   OrbitCameraMode (varsayılan)
##                   VehicleCameraMode (stub)
## İleride eklenebilir: FirstPersonCameraMode, DragonCameraMode,
##                      HelicopterCameraMode, CinematicCameraMode, TopDownCameraMode

class_name CameraModeBase
extends RefCounted

var mode_name: String = "Base"

## Mod aktif olunca çağrılır. Node ref'lerini al, FOV/offset başlangıcını ayarla.
func enter(rig: Node3D) -> void: pass

## Mod kapanınca çağrılır. Temizlik varsa yap.
func exit(rig: Node3D) -> void: pass

## _process'te her frame çağrılır.
func process(rig: Node3D, delta: float) -> void: pass

## Mouse motion, scroll vb. kamera inputları buraya gelir.
## Hareket inputu PlayerController'da — buraya gelmez.
func handle_input(rig: Node3D, event: InputEvent) -> void: pass

## PlayerController'ın input yönünü hesaplamak için kullanır.
func get_forward(rig: Node3D) -> Vector3:
	return -rig.global_basis.z

func get_right(rig: Node3D) -> Vector3:
	return rig.global_basis.x

## Debug overlay string'i.
func set_character_speed(speed: float) -> void: pass

func get_debug_label() -> String:
	return mode_name
