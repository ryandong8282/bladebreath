class_name CombatTypes
extends RefCounted


enum State {
	IDLE,
	MOVING,
	ATTACKING,
	GUARDING,
	DODGING,
	WINDUP,
	RECOVERING,
	STAGGERED,
	DEAD,
}


enum HitResult {
	IGNORED,
	HIT,
	BLOCKED,
	PARRIED,
	DODGED,
	PERFECT_DODGE,
	CLASHED,
}


enum AttackKind {
	LIGHT,
	SKILL,
	ENEMY_NORMAL,
	ENEMY_DELAYED,
	ENEMY_UNBLOCKABLE,
	EXECUTION,
}


static func hit_result_name(result: int) -> String:
	match result:
		HitResult.HIT:
			return "HIT"
		HitResult.BLOCKED:
			return "BLOCKED"
		HitResult.PARRIED:
			return "PARRIED"
		HitResult.DODGED:
			return "DODGED"
		HitResult.PERFECT_DODGE:
			return "PERFECT_DODGE"
		HitResult.CLASHED:
			return "CLASHED"
		_:
			return "IGNORED"
