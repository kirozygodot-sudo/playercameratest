## orbit_camera_mode.gd
## Third-person orbit kamera. Virabis varsayılan modu.
## Over-the-shoulder + aim destekli.
##
## CameraRig altındaki node yapısı:
##   YawPivot (Node3D)
##     └── PitchPivot (Node3D)
##           └── SpringArm3D
##                 └── Camera3D
##
## Sarsıntı (Shake) Profilleri:
##   - LANDING  : Orta genlik, düşük frekans — ağır iniş hissi
##   - EXPLOSION: Yüksek genlik, yüksek frekans — patlama sarsıntısı
##   - JUMP     : Düşük genlik, orta frekans — hafif zıplama titremesi
##   - IMPACT   : Orta-yüksek genlik, çok yüksek frekans — ani darbe
##   - CUSTOM   : Dışarıdan tam kontrol

class_name OrbitCameraMode
extends CameraModeBase

# ── Config ────────────────────────────────────────────────────────────────────
var mouse_sensitivity  : float   = 0.15
var aim_sensitivity_mult : float = 0.5   # Nişan alırken hassasiyet çarpanı
var pitch_min          : float   = -60.0
var pitch_max          : float   =  60.0
var rotation_smooth    : float   = 12.0

var default_distance   : float   = 4.0
var min_distance       : float   = 2.5
var max_distance       : float   = 6.0
var zoom_step          : float   = 0.5
var zoom_smooth        : float   = 8.0

# ── Sprint FOV Kick ───────────────────────────────────────────────────────────
var fov_sprint         : float   = 85.0   # Sprint'te geniş FOV
var fov_min_speed      : float   = 5.0    # Bu hızın altında FOV değişmez
var fov_max_speed      : float   = 20.0   # Bu hızın üstünde max FOV
var fov_min_value      : float   = 75.0   # Min hızda FOV değeri
var fov_max_value      : float   = 90.0   # Max hızda FOV değeri
var fov_speed_smooth   : float   = 4.0    # Hıza bağlı FOV geçiş hızı
var sprint_fov_smooth  : float   = 6.0    # Sprint FOV geçiş hızı
var _is_sprinting      : bool    = false
var _current_character_speed : float = 0.0

var offset_normal      : Vector3 = Vector3(0.6, 1.6, 0.0)  # Over-the-shoulder
var offset_aiming      : Vector3 = Vector3(0.2, 1.5, 0.0)  # Nişan → merkeze
var fov_normal         : float   = 75.0
var fov_aiming         : float   = 60.0
var aim_distance       : float   = 2.8
var aim_smooth         : float   = 8.0

# ── İç durum ──────────────────────────────────────────────────────────────────
var _yaw               : float   = 0.0
var _pitch             : float   = 0.0
var _target_dist       : float
var _current_dist      : float
var _is_aiming         : bool    = false
var _target_fov        : float
var _target_offset     : Vector3

# ── Camera Shake — FastNoiseLite tabanlı ─────────────────────────────────────
## Sarsıntı profil sabitleri
enum ShakeProfile { LANDING, EXPLOSION, JUMP, IMPACT, CUSTOM }

## Aktif sarsıntı verisi
var _shake_amount      : float   = 0.0   # Anlık genlik (0..1 arası normalize)
var _shake_decay       : float   = 5.0   # Saniyede azalma hızı
var _shake_time        : float   = 0.0   # Noise örnekleme için zaman sayacı

## Noise nesneleri — her profil için ayrı, farklı seed ile
var _noise_landing     : FastNoiseLite = FastNoiseLite.new()
var _noise_explosion   : FastNoiseLite = FastNoiseLite.new()
var _noise_jump        : FastNoiseLite = FastNoiseLite.new()
var _noise_impact      : FastNoiseLite = FastNoiseLite.new()
var _noise_custom      : FastNoiseLite = FastNoiseLite.new()

## Aktif profil parametreleri
var _active_frequency  : float   = 8.0   # Noise örnekleme frekansı (Hz)
var _active_amplitude  : float   = 0.05  # Max pozisyon ofseti (metre)
var _active_rot_amp    : float   = 0.5   # Max rotasyon ofseti (derece)
var _active_noise      : FastNoiseLite   # Şu an kullanılan noise referansı

## Profil parametreleri tablosu
## [frequency, position_amplitude, rotation_amplitude, decay_rate]
const SHAKE_PROFILES : Dictionary = {
	ShakeProfile.LANDING:   [8.0,  0.06, 0.8,  4.5],
	ShakeProfile.EXPLOSION: [20.0, 0.12, 1.5,  6.0],
	ShakeProfile.JUMP:      [12.0, 0.03, 0.4,  7.0],
	ShakeProfile.IMPACT:    [30.0, 0.08, 1.0,  8.0],
	ShakeProfile.CUSTOM:    [10.0, 0.05, 0.6,  5.0],
}

# ── Node refs (enter'da çözülür) ──────────────────────────────────────────────
var _yaw_pivot    : Node3D      = null
var _pitch_pivot  : Node3D      = null
var _spring_arm   : SpringArm3D = null
var _camera       : Camera3D    = null

# ─────────────────────────────────────────────────────────────────────────────

func _init() -> void:
	mode_name = "Orbit"
	_setup_noise_nodes()


## Tüm noise nesnelerini farklı seed ve frekanslarla başlat
func _setup_noise_nodes() -> void:
	var base_seed := randi()

	_noise_landing.seed       = base_seed
	_noise_landing.noise_type = FastNoiseLite.TYPE_PERLIN
	_noise_landing.frequency  = SHAKE_PROFILES[ShakeProfile.LANDING][0] * 0.01

	_noise_explosion.seed       = base_seed + 1000
	_noise_explosion.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	_noise_explosion.frequency  = SHAKE_PROFILES[ShakeProfile.EXPLOSION][0] * 0.01

	_noise_jump.seed       = base_seed + 2000
	_noise_jump.noise_type = FastNoiseLite.TYPE_PERLIN
	_noise_jump.frequency  = SHAKE_PROFILES[ShakeProfile.JUMP][0] * 0.01

	_noise_impact.seed       = base_seed + 3000
	_noise_impact.noise_type = FastNoiseLite.TYPE_VALUE_CUBIC
	_noise_impact.frequency  = SHAKE_PROFILES[ShakeProfile.IMPACT][0] * 0.01

	_noise_custom.seed       = base_seed + 4000
	_noise_custom.noise_type = FastNoiseLite.TYPE_PERLIN
	_noise_custom.frequency  = SHAKE_PROFILES[ShakeProfile.CUSTOM][0] * 0.01

	# Varsayılan aktif noise
	_active_noise = _noise_landing


func enter(rig: Node3D) -> void:
	_yaw_pivot   = rig.get_node("YawPivot")
	_pitch_pivot = rig.get_node("YawPivot/PitchPivot")
	_spring_arm  = rig.get_node("YawPivot/PitchPivot/SpringArm3D")
	_camera      = rig.get_node("YawPivot/PitchPivot/SpringArm3D/Camera3D")

	_target_dist    = default_distance
	_current_dist   = default_distance
	_target_fov     = fov_normal
	_target_offset  = offset_normal

	_camera.fov               = fov_normal
	_spring_arm.spring_length = default_distance
	_spring_arm.position      = offset_normal


func exit(_rig: Node3D) -> void:
	# Kamera pozisyonunu sıfırla (geçiş artefaktı önleme)
	if _camera:
		_camera.position = Vector3.ZERO
		_camera.rotation_degrees = Vector3.ZERO


func handle_input(_rig: Node3D, event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		var sens := mouse_sensitivity * (aim_sensitivity_mult if _is_aiming else 1.0)
		_yaw   -= event.relative.x * sens
		_pitch -= event.relative.y * sens
		_pitch  = clampf(_pitch, pitch_min, pitch_max)

	if event is InputEventMouseButton and event.pressed:
		match event.button_index:
			MOUSE_BUTTON_WHEEL_UP:
				_target_dist = clampf(_target_dist - zoom_step, min_distance, max_distance)
			MOUSE_BUTTON_WHEEL_DOWN:
				_target_dist = clampf(_target_dist + zoom_step, min_distance, max_distance)

	# Aim mode — sağ tık
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_RIGHT:
		_set_aiming(event.pressed)


func process(_rig: Node3D, delta: float) -> void:
	if _yaw_pivot == null: return

	var t_rot  := rotation_smooth * delta
	var t_aim  := aim_smooth * delta
	var t_zoom := zoom_smooth * delta

	_yaw_pivot.rotation_degrees.y   = lerp_angle(_yaw_pivot.rotation_degrees.y,   _yaw,   t_rot)
	_pitch_pivot.rotation_degrees.x = lerp_angle(_pitch_pivot.rotation_degrees.x, _pitch, t_rot)

	_current_dist             = lerp(_current_dist, _target_dist, t_zoom)
	_spring_arm.spring_length = _current_dist

	_spring_arm.position = lerp(_spring_arm.position, _target_offset, t_aim)

	# ── Dinamik FOV ──────────────────────────────────────────────────────────
	_update_dynamic_fov(delta)

	# ── FastNoiseLite tabanlı Camera Shake ───────────────────────────────────
	_update_camera_shake(delta)


## Hıza ve sprint durumuna göre FOV'u güncelle.
## Öncelik sırası: Aim > Sprint > Hız bazlı
func _update_dynamic_fov(delta: float) -> void:
	var final_fov : float

	if _is_aiming:
		# Nişan alırken sabit dar FOV
		final_fov = fov_aiming
	elif _is_sprinting:
		# Sprint: sabit geniş FOV
		final_fov = fov_sprint
	else:
		# Hıza duyarlı FOV: hız arttıkça FOV genişler
		var speed_ratio := clampf(
			(_current_character_speed - fov_min_speed) / maxf(fov_max_speed - fov_min_speed, 0.001),
			0.0, 1.0
		)
		# Ease-in eğrisi: hızın başlangıcında yavaş, sonunda hızlı genişleme
		var eased_ratio := speed_ratio * speed_ratio
		final_fov = lerpf(fov_min_value, fov_max_value, eased_ratio)

	# Hedef FOV'u güncelle (aim geçişlerinde aim_smooth, diğerlerinde speed_smooth)
	var smooth_factor := aim_smooth if _is_aiming else fov_speed_smooth
	_camera.fov = lerpf(_camera.fov, final_fov, smooth_factor * delta)


## FastNoiseLite tabanlı sarsıntı uygula.
## Noise örneklemesi zamanla ilerler → organik, sürekli titreme hissi.
func _update_camera_shake(delta: float) -> void:
	if _shake_amount <= 0.001:
		# Sarsıntı bitti — kamerayı sıfırla
		_camera.position       = Vector3.ZERO
		_camera.rotation_degrees = Vector3.ZERO
		_shake_amount          = 0.0
		return

	# Zaman sayacını ilerlet (noise örnekleme hızı = active_frequency)
	_shake_time += delta * _active_frequency

	# Noise'dan 3 eksen için bağımsız değerler örnekle
	# Farklı offset'ler → birbirinden bağımsız eksen hareketleri
	var nx := _active_noise.get_noise_2d(_shake_time,        0.0)
	var ny := _active_noise.get_noise_2d(_shake_time + 100.0, 0.0)
	var nz := _active_noise.get_noise_2d(_shake_time + 200.0, 0.0)

	# Rotasyon için ayrı örnekler (pozisyondan bağımsız)
	var rx := _active_noise.get_noise_2d(0.0, _shake_time + 300.0)
	var ry := _active_noise.get_noise_2d(0.0, _shake_time + 400.0)

	# Genliği uygula
	var pos_scale := _active_amplitude * _shake_amount
	var rot_scale := _active_rot_amp   * _shake_amount

	_camera.position = Vector3(nx, ny, nz) * pos_scale
	_camera.rotation_degrees = Vector3(rx * rot_scale, ry * rot_scale, 0.0)

	# Sarsıntıyı azalt
	_shake_amount = maxf(0.0, _shake_amount - _shake_decay * delta)


func get_forward(_rig: Node3D) -> Vector3:
	return -_yaw_pivot.global_basis.z if _yaw_pivot else Vector3.FORWARD


func get_right(_rig: Node3D) -> Vector3:
	return _yaw_pivot.global_basis.x if _yaw_pivot else Vector3.RIGHT


func get_debug_label() -> String:
	return "Orbit | aim=%s | dist=%.1f | yaw=%.0f pitch=%.0f | shake=%.2f" % [
		_is_aiming, _current_dist, _yaw, _pitch, _shake_amount
	]


func is_aiming() -> bool:
	return _is_aiming


func set_sensitivity(value: float) -> void:
	mouse_sensitivity = value


## Sprint durumunu ayarla (FOV kick için)
func set_character_speed(speed: float) -> void:
	_current_character_speed = speed

func set_sprinting(value: bool) -> void:
	_is_sprinting = value


# ── PUBLIC SHAKE API ──────────────────────────────────────────────────────────

## Önceden tanımlı profil ile sarsıntı ekle.
## amount: 0.0..1.0 arası normalize genlik çarpanı
func add_shake(amount: float, profile: ShakeProfile = ShakeProfile.LANDING) -> void:
	var params : Array = SHAKE_PROFILES[profile]
	_active_frequency = params[0]
	_active_amplitude = params[1]
	_active_rot_amp   = params[2]
	_shake_decay      = params[3]
	_active_noise     = _get_noise_for_profile(profile)

	# Sarsıntıyı biriktir (stacking), ama 1.0'ı geçme
	_shake_amount = minf(_shake_amount + amount, 1.0)


## Tam özelleştirilebilir sarsıntı (patlama büyüklüğü, özel efektler için)
func add_shake_custom(amount: float, frequency: float, pos_amplitude: float,
		rot_amplitude: float, decay: float) -> void:
	_active_frequency = frequency
	_active_amplitude = pos_amplitude
	_active_rot_amp   = rot_amplitude
	_shake_decay      = decay
	_active_noise     = _noise_custom
	_shake_amount     = minf(_shake_amount + amount, 1.0)


## Anlık sarsıntıyı durdur (cutscene geçişi vb.)
func stop_shake() -> void:
	_shake_amount = 0.0
	if _camera:
		_camera.position       = Vector3.ZERO
		_camera.rotation_degrees = Vector3.ZERO


# ── Private ───────────────────────────────────────────────────────────────────

func _set_aiming(value: bool) -> void:
	_is_aiming     = value
	_target_fov    = fov_aiming    if value else fov_normal
	_target_offset = offset_aiming if value else offset_normal
	_target_dist   = aim_distance  if value else default_distance


func _get_noise_for_profile(profile: ShakeProfile) -> FastNoiseLite:
	match profile:
		ShakeProfile.LANDING:   return _noise_landing
		ShakeProfile.EXPLOSION: return _noise_explosion
		ShakeProfile.JUMP:      return _noise_jump
		ShakeProfile.IMPACT:    return _noise_impact
		_:                      return _noise_custom
