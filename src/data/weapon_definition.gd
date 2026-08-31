class_name WeaponDefinition
extends Resource


@export var weapon_id: StringName = &"longblade"
@export var display_name: String = "长刀"
@export_multiline var description: String = "均衡的中距离武器，以主动交锋和架势压制为核心。"

@export_group("Light attack")
@export var light_damage: float = 13.0
@export var light_posture_damage: float = 16.0
@export var light_range: float = 2.55
@export var light_arc_degrees: float = 105.0

@export_group("Defense")
@export var guard_posture_multiplier: float = 0.72
@export var parry_window_seconds: float = 0.17
@export var parry_posture_damage: float = 36.0
@export var clash_posture_damage: float = 15.0
