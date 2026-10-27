using UnityEngine;

namespace RivenMenu
{
    public static class MusicHud
    {
        public static bool Shown;

        private const float W = 340f, H = 92f;
        private static Vector2 pos = new Vector2(-1f, -1f);
        private static bool dragging, seeking;
        private static Vector2 grab;
        private static Font font;
        private static GUIStyle title, small, icon;

        public static void Draw()
        {
            if (!Shown || RivenAudio.Instance == null) return;
            if (Event.current.type == EventType.Layout) return;

            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -900;

            try
            {
                if (title == null) BuildStyles();
                if (pos.x < 0f) pos = new Vector2(20f, Screen.height - H - 20f);
                pos.x = Mathf.Clamp(pos.x, 0f, Screen.width - W);
                pos.y = Mathf.Clamp(pos.y, 0f, Screen.height - H);

                RivenAudio a = RivenAudio.Instance;
                MenuTheme th = Theme.Current;
                Color acc = Theme.Accent;
                Rect panel = new Rect(pos.x, pos.y, W, H);

                Event e = Event.current;
                Rect dragBar = new Rect(panel.x, panel.y, panel.width, 34f);
                if (e.type == EventType.MouseDown && e.button == 0 && dragBar.Contains(e.mousePosition))
                {
                    dragging = true;
                    grab = e.mousePosition - pos;
                    e.Use();
                }
                if (dragging && e.type == EventType.MouseDrag) { pos = e.mousePosition - grab; e.Use(); }
                if (e.rawType == EventType.MouseUp) dragging = false;

                Glow(panel, 14, Theme.WithA(acc, 0.2f));
                Fill(panel, 14f, Theme.WithA(th.Panel, 0.96f));

                float split = 1.5f + Mathf.Sin(Time.unscaledTime * 2.4f) * 1f;
                DrawMark(new Vector2(panel.x + 26f, panel.y + 24f), 26f, split);

                string name = a.TrackName;
                if (name.Length > 30) name = name.Substring(0, 29) + "…";
                GUI.color = th.Text;
                GUI.Label(new Rect(panel.x + 48f, panel.y + 8f, W - 110f, 22f), name, title);

                GUI.color = Theme.WithA(th.Text, 0.6f);
                string counter = a.HasMusic ? a.TrackNumber + "/" + a.TrackCount : "";
                GUI.Label(new Rect(panel.xMax - 60f, panel.y + 10f, 50f, 18f), counter, small);

                float len = Mathf.Max(0.01f, a.Length);
                float cur = a.Time;
                Rect bar = new Rect(panel.x + 16f, panel.y + 44f, W - 32f, 8f);
                float frac = Mathf.Clamp01(cur / len);

                if (e.type == EventType.MouseDown && e.button == 0 && new Rect(bar.x - 4f, bar.y - 6f, bar.width + 8f, 20f).Contains(e.mousePosition))
                {
                    seeking = true;
                    e.Use();
                }
                if (seeking && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
                {
                    frac = Mathf.Clamp01((e.mousePosition.x - bar.x) / bar.width);
                    a.Seek(frac * len);
                    e.Use();
                }
                if (e.rawType == EventType.MouseUp) seeking = false;

                Fill(bar, 4f, Theme.WithA(th.Text, 0.14f));
                Fill(new Rect(bar.x, bar.y, bar.width * frac, bar.height), 4f, acc);
                Fill(new Rect(bar.x + bar.width * frac - 4f, bar.y - 3f, 8f, 14f), 4f, Theme.Lighten(acc, 0.3f));

                GUI.color = Theme.WithA(th.Text, 0.75f);
                GUI.Label(new Rect(panel.x + 16f, panel.y + 56f, 80f, 18f), Clock(cur), small);
                GUI.Label(new Rect(panel.xMax - 96f, panel.y + 56f, 80f, 18f), Clock(len), SmallRight());

                float by = panel.y + 60f;
                if (Btn(new Rect(panel.center.x - 58f, by, 32f, 24f), "◀◀", acc, th)) a.Prev();
                if (Btn(new Rect(panel.center.x - 18f, by, 36f, 24f), a.Playing ? "‖" : "▶", acc, th)) a.TogglePlay();
                if (Btn(new Rect(panel.center.x + 26f, by, 32f, 24f), "▶▶", acc, th)) a.Next();

                Edge(panel, 14f, Theme.WithA(Theme.Lighten(acc, 0.3f), th.Edge));
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private static bool Btn(Rect r, string label, Color acc, MenuTheme th)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, 6f, hover ? Theme.WithA(acc, 0.5f) : Theme.WithA(th.Row, 0.6f));
            GUI.color = th.Text;
            GUI.Label(r, label, icon);
            GUI.color = Color.white;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hover)
            {
                Event.current.Use();
                if (MenuGUI.SoundsOn && RivenAudio.Instance != null) RivenAudio.Instance.PlayClick();
                return true;
            }
            return false;
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        private static void DrawMark(Vector2 c, float size, float gap)
        {
            Vector2 off = Tex.RiftAxis * gap;
            Rect r = new Rect(c.x - size / 2f, c.y - size / 2f, size, size);
            GUI.color = Theme.Accent;
            GUI.DrawTexture(new Rect(r.x - off.x, r.y - off.y, size, size), Tex.RiftLeft);
            GUI.color = Theme.Accent2;
            GUI.DrawTexture(new Rect(r.x + off.x, r.y + off.y, size, size), Tex.RiftRight);
            float ss = size * 0.34f;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(c.x - ss / 2f, c.y - ss / 2f, ss, ss), Tex.Star);
        }

        private static void Fill(Rect r, float radius, Color c)
        {
            GUI.color = c;
            Tex.FillStyle(Mathf.Round(radius)).Draw(r, false, false, false, false);
            GUI.color = Color.white;
        }

        private static void Edge(Rect r, float radius, Color c)
        {
            GUI.color = c;
            Tex.EdgeStyle(Mathf.Round(radius), 1f).Draw(r, false, false, false, false);
            GUI.color = Color.white;
        }

        private static void Glow(Rect r, int spread, Color c)
        {
            GUI.color = c;
            Tex.GlowStyle(14f, spread).Draw(new Rect(r.x - spread, r.y - spread, r.width + spread * 2f, r.height + spread * 2f), false, false, false, false);
            GUI.color = Color.white;
        }

        private static GUIStyle smallRight;
        private static GUIStyle SmallRight()
        {
            if (smallRight == null)
            {
                smallRight = new GUIStyle(small) { alignment = TextAnchor.MiddleRight };
            }
            return smallRight;
        }

        private static void BuildStyles()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title = new GUIStyle { font = font, fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, richText = true, normal = { textColor = Color.white } };
            small = new GUIStyle { font = font, fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            icon = new GUIStyle { font = font, fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }
    }
}
