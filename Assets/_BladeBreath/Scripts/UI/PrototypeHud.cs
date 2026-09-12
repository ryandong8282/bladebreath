using UnityEngine;

namespace BladeBreath
{
    // Compact combat HUD for the vertical slice. Gameplay state remains owned by Combatant.
    public sealed class PrototypeHud : MonoBehaviour
    {
        private Combatant _player;
        private Combatant _enemy;
        private PlayerController _playerController;
        private Texture2D _skillIconAtlas;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _barStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _locationStyle;
        private GUIStyle _keyStyle;
        private GUIStyle _skillStyle;
        private GUIStyle _calloutStyle;
        private float _styleScale;
        private string _lastPlayerEvent;
        private float _playerEventChangedAt;

        private static readonly string[] SkillNames = { "迎推", "回锋", "震烈" };
        private static readonly string[] SkillKeys = { "H", "U", "I" };

        private static readonly Color Ink = new Color(0.018f, 0.016f, 0.014f, 0.74f);
        private static readonly Color Border = new Color(0.5f, 0.4f, 0.25f, 0.72f);
        private static readonly Color Bone = new Color(0.88f, 0.82f, 0.68f);
        private static readonly Color Muted = new Color(0.57f, 0.55f, 0.49f);
        private static readonly Color Health = new Color(0.52f, 0.035f, 0.025f);
        private static readonly Color Posture = new Color(0.78f, 0.42f, 0.055f);

        public bool HasSkillIconAtlas => _skillIconAtlas != null;

        public void Configure(
            Combatant player,
            Combatant enemy,
            PlayerController playerController = null,
            Texture2D skillIconAtlas = null)
        {
            _player = player;
            _enemy = enemy;
            _playerController = playerController;
            _skillIconAtlas = skillIconAtlas;
            _lastPlayerEvent = player != null ? player.LastEvent : string.Empty;
            _playerEventChangedAt = Time.unscaledTime;
        }

        private void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1.18f);
            EnsureStyles(scale);
            float margin = 28f * scale;

            DrawLocation(scale, margin);
            DrawEnemyStatus(scale, margin);
            DrawPlayerStatus(scale, margin);
            DrawSkillBar(scale, margin);
            DrawEventCallout(scale, margin);

            bool playerDead = _player != null && _player.IsDead;
            bool enemyDead = _enemy != null && _enemy.IsDead;
            if (playerDead || enemyDead) DrawResult(scale, enemyDead);
        }

        private void DrawLocation(float scale, float margin)
        {
            GUI.Label(new Rect(margin, margin, 300f * scale, 34f * scale), "灰窑外院", _locationStyle);
            GUI.Label(new Rect(margin + 2f * scale, margin + 31f * scale, 320f * scale, 20f * scale),
                "建德城郊 · 军器监遗址", _subtitleStyle);
            DrawRect(new Rect(margin, margin + 57f * scale, 120f * scale, 1f), Border);
        }

        private void DrawEnemyStatus(float scale, float margin)
        {
            if (_enemy == null) return;
            float width = Mathf.Min(560f * scale, Screen.width * 0.48f);
            float x = (Screen.width - width) * 0.5f;
            float y = margin * 0.72f;
            GUI.Label(new Rect(x, y, width, 25f * scale), "造像署执刃者", _centerStyle);
            DrawFineBar(new Rect(x, y + 28f * scale, width, 13f * scale),
                _enemy.HealthRatio, Health, new Color(0.8f, 0.68f, 0.47f, 0.9f));
            DrawFineBar(new Rect(x + width * 0.18f, y + 47f * scale, width * 0.64f, 6f * scale),
                _enemy.PostureRatio, Posture, new Color(0.34f, 0.3f, 0.24f, 0.7f));
        }

        private void DrawPlayerStatus(float scale, float margin)
        {
            if (_player == null) return;
            float width = Mathf.Min(360f * scale, Screen.width * 0.3f);
            float bottom = Screen.height - margin;
            GUI.Label(new Rect(margin, bottom - 91f * scale, width, 24f * scale), "无铭者", _titleStyle);
            DrawFineBar(new Rect(margin, bottom - 61f * scale, width, 12f * scale),
                _player.HealthRatio, Health, new Color(0.77f, 0.66f, 0.46f, 0.88f));
            DrawFineBar(new Rect(margin, bottom - 42f * scale, width * 0.82f, 7f * scale),
                _player.PostureRatio, Posture, new Color(0.3f, 0.28f, 0.23f, 0.72f));

            float edgeRatio = _player.MaxEdge > 0 ? (float)_player.Edge / _player.MaxEdge : 0f;
            DrawFineBar(new Rect(margin, bottom - 27f * scale, width * 0.62f, 5f * scale),
                edgeRatio, new Color(0.16f, 0.52f, 0.68f), new Color(0.3f, 0.28f, 0.23f, 0.62f));
            string momentum = _player.HasClashMomentum
                ? $"锋意 {_player.Edge}/{_player.MaxEdge}　抗衡 {_player.ClashMomentumRemaining:0.0}s"
                : $"锋意 {_player.Edge}/{_player.MaxEdge}";
            GUI.Label(new Rect(margin + width * 0.65f, bottom - 39f * scale, width * 0.5f, 22f * scale),
                momentum, _subtitleStyle);
        }

        private void DrawSkillBar(float scale, float margin)
        {
            const float iconSize = 64f;
            const float gap = 13f;
            float width = (iconSize * 3f + gap * 2f) * scale;
            float x = Screen.width - margin - width;
            float y = Screen.height - margin - 86f * scale;

            for (int slot = 0; slot < 3; slot++)
            {
                Rect card = new Rect(x + slot * (iconSize + gap) * scale, y, iconSize * scale, iconSize * scale);
                DrawPanel(card, new Color(0.025f, 0.023f, 0.019f, 0.72f),
                    slot == 2 ? new Color(0.62f, 0.23f, 0.08f, 0.85f) : Border);
                Rect icon = new Rect(card.x + 5f * scale, card.y + 5f * scale,
                    card.width - 10f * scale, card.height - 10f * scale);
                bool enoughEdge = _player != null && _playerController != null &&
                                  _player.Edge >= _playerController.GetSkillCost(slot);
                float remaining = _playerController != null ? _playerController.GetSkillCooldownRemaining(slot) : 0f;
                Color previous = GUI.color;
                GUI.color = enoughEdge ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.88f);
                if (_skillIconAtlas != null)
                {
                    GUI.DrawTextureWithTexCoords(icon, _skillIconAtlas,
                        new Rect(slot / 3f, 0f, 1f / 3f, 1f), true);
                }
                else
                {
                    DrawRect(icon, new Color(0.09f, 0.08f, 0.065f, 1f));
                    GUI.Label(icon, "刃", _centerStyle);
                }
                GUI.color = previous;

                if (remaining > 0f)
                {
                    float ratio = _playerController.GetSkillCooldownRatio(slot);
                    DrawRect(new Rect(icon.x, icon.y, icon.width, icon.height * ratio),
                        new Color(0.01f, 0.012f, 0.014f, 0.8f));
                    GUI.Label(icon, remaining.ToString("0.0"), _centerStyle);
                }

                Rect key = new Rect(card.x - 7f * scale, card.y - 7f * scale, 22f * scale, 20f * scale);
                DrawPanel(key, new Color(0.025f, 0.023f, 0.019f, 0.92f), Border);
                GUI.Label(key, SkillKeys[slot], _keyStyle);
                GUI.Label(new Rect(card.x, card.yMax + 3f * scale, card.width, 18f * scale),
                    SkillNames[slot], _skillStyle);
            }
        }

        private void DrawEventCallout(float scale, float margin)
        {
            if (_player == null) return;
            if (_lastPlayerEvent != _player.LastEvent)
            {
                _lastPlayerEvent = _player.LastEvent;
                _playerEventChangedAt = Time.unscaledTime;
            }
            float age = Time.unscaledTime - _playerEventChangedAt;
            if (age > 1.25f) return;

            string callout = null;
            if (_lastPlayerEvent.Contains("弹")) callout = "弹反";
            else if (_lastPlayerEvent.Contains("拼刀") || _lastPlayerEvent.Contains("抗衡")) callout = "抗衡";
            else if (_lastPlayerEvent.Contains("架势崩溃")) callout = "破势";
            if (callout == null) return;

            float alpha = Mathf.Clamp01(1f - Mathf.Max(0f, age - 0.65f) / 0.6f);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(margin * 1.15f, Screen.height * 0.38f, 220f * scale, 64f * scale),
                callout, _calloutStyle);
            GUI.color = previous;
        }

        private void DrawResult(float scale, bool enemyDead)
        {
            string result = enemyDead ? "执刃者伏诛" : "无铭者倒下";
            Rect rect = new Rect(Screen.width * 0.5f - 190f * scale, Screen.height * 0.5f - 48f * scale,
                380f * scale, 96f * scale);
            DrawPanel(rect, new Color(0.012f, 0.011f, 0.01f, 0.9f), Border);
            GUI.Label(new Rect(rect.x, rect.y + 12f * scale, rect.width, 36f * scale), result, _centerStyle);
            GUI.Label(new Rect(rect.x, rect.y + 57f * scale, rect.width, 22f * scale),
                "按 R 重新交锋", _subtitleStyle);
        }

        private void EnsureStyles(float scale)
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                _subtitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
                _labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
                _barStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                _centerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _locationStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                _keyStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _skillStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _calloutStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            }
            if (Mathf.Approximately(_styleScale, scale)) return;

            _styleScale = scale;
            _titleStyle.fontSize = Mathf.RoundToInt(18f * scale);
            _titleStyle.normal.textColor = Bone;
            _subtitleStyle.fontSize = Mathf.RoundToInt(11f * scale);
            _subtitleStyle.normal.textColor = Muted;
            _labelStyle.fontSize = Mathf.RoundToInt(13f * scale);
            _labelStyle.normal.textColor = Bone;
            _barStyle.fontSize = Mathf.RoundToInt(10f * scale);
            _barStyle.normal.textColor = Color.white;
            _centerStyle.fontSize = Mathf.RoundToInt(18f * scale);
            _centerStyle.normal.textColor = Bone;
            _locationStyle.fontSize = Mathf.RoundToInt(24f * scale);
            _locationStyle.normal.textColor = Bone;
            _keyStyle.fontSize = Mathf.RoundToInt(10f * scale);
            _keyStyle.normal.textColor = new Color(0.95f, 0.78f, 0.42f);
            _skillStyle.fontSize = Mathf.RoundToInt(12f * scale);
            _skillStyle.normal.textColor = Bone;
            _calloutStyle.fontSize = Mathf.RoundToInt(34f * scale);
            _calloutStyle.normal.textColor = new Color(0.96f, 0.76f, 0.34f);
        }

        private static void DrawFineBar(Rect rect, float ratio, Color fill, Color rim)
        {
            ratio = Mathf.Clamp01(ratio);
            DrawRect(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), rim);
            DrawRect(rect, new Color(0.018f, 0.016f, 0.014f, 0.9f));
            DrawRect(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), fill);
            DrawRect(new Rect(rect.x, rect.y, rect.width * ratio, Mathf.Max(1f, rect.height * 0.16f)),
                new Color(1f, 0.74f, 0.45f, 0.38f));
        }

        private static void DrawPanel(Rect rect, Color background, Color border)
        {
            DrawRect(rect, border);
            DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), background);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
