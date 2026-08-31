using UnityEngine;

namespace BladeBreath
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private Combatant _player;
        private Combatant _enemy;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _centerStyle;

        public void Configure(Combatant player, Combatant enemy)
        {
            _player = player;
            _enemy = enemy;
        }

        private void OnGUI()
        {
            EnsureStyles();

            const float margin = 24f;
            float panelWidth = Mathf.Min(460f, Screen.width * 0.42f);

            GUI.Box(new Rect(margin - 12f, margin - 12f, panelWidth + 24f, 158f), GUIContent.none);
            GUI.Label(new Rect(margin, margin, panelWidth, 32f), "无铭：漳城夜 · Unity 战斗灰盒", _titleStyle);

            if (_player != null)
            {
                DrawBar(
                    new Rect(margin, margin + 43f, panelWidth, 22f),
                    _player.HealthRatio,
                    $"无铭者  生命 {_player.Health:0}/{_player.MaxHealth:0}",
                    new Color(0.66f, 0.12f, 0.10f));

                DrawBar(
                    new Rect(margin, margin + 73f, panelWidth, 20f),
                    _player.PostureRatio,
                    $"架势 {_player.Posture:0}/{_player.MaxPosture:0}",
                    new Color(0.82f, 0.60f, 0.16f));

                GUI.Label(
                    new Rect(margin, margin + 104f, panelWidth, 28f),
                    $"状态：{_player.LastEvent}",
                    _labelStyle);
            }

            if (_enemy != null)
            {
                float enemyWidth = Mathf.Min(420f, Screen.width * 0.38f);
                float enemyX = Screen.width - enemyWidth - margin;
                GUI.Box(new Rect(enemyX - 12f, margin - 12f, enemyWidth + 24f, 126f), GUIContent.none);
                GUI.Label(new Rect(enemyX, margin, enemyWidth, 28f), "造像署执刃者", _titleStyle);
                DrawBar(
                    new Rect(enemyX, margin + 38f, enemyWidth, 20f),
                    _enemy.HealthRatio,
                    $"生命 {_enemy.Health:0}/{_enemy.MaxHealth:0}",
                    new Color(0.52f, 0.12f, 0.11f));
                DrawBar(
                    new Rect(enemyX, margin + 66f, enemyWidth, 20f),
                    _enemy.PostureRatio,
                    $"架势 {_enemy.Posture:0}/{_enemy.MaxPosture:0}",
                    new Color(0.82f, 0.60f, 0.16f));
                GUI.Label(new Rect(enemyX, margin + 91f, enemyWidth, 24f), _enemy.LastEvent, _labelStyle);
            }

            float controlsWidth = Mathf.Min(670f, Screen.width - margin * 2f);
            GUI.Box(
                new Rect(margin, Screen.height - 82f, controlsWidth, 56f),
                "WASD/方向键 移动　J/左键 斩击与处决　K/右键 守/起手弹反　Space/Shift 闪身　R 重置");

            bool playerDead = _player != null && _player.IsDead;
            bool enemyDead = _enemy != null && _enemy.IsDead;
            if (playerDead || enemyDead)
            {
                string result = enemyDead ? "执刃者已倒下" : "无铭者倒下";
                Rect rect = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 70f, 460f, 140f);
                GUI.Box(rect, GUIContent.none);
                GUI.Label(new Rect(rect.x, rect.y + 24f, rect.width, 42f), result, _centerStyle);
                GUI.Label(new Rect(rect.x, rect.y + 78f, rect.width, 32f), "按 R / 手柄 Start 重新交锋", _centerStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };

            _centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static void DrawBar(Rect rect, float ratio, string label, Color fill)
        {
            ratio = Mathf.Clamp01(ratio);
            Color previous = GUI.color;

            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(rect, label, new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14
            });

            GUI.color = previous;
        }
    }
}
