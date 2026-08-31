class_name StyleDefinition
extends Resource


@export var style_id: StringName = &"hearing_blade"
@export var display_name: String = "听刃"
@export_multiline var description: String = "以弹反夺取节奏，再用锋意放大优势。"
@export var edge_source_text: String = "精准弹反"
@export var skill_1_name: String = "震刃"
@export var skill_2_name: String = "回锋"
@export var accent_color: Color = Color(0.52, 0.9, 1.0, 1.0)
@export_range(1, 6, 1) var max_edge: int = 3
