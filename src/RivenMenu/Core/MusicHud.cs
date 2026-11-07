using UnityEngine;
using UnityEngine.XR;

namespace RivenMenu
{
    public static class MusicHud
    {
        public static bool Shown;

        private const float W = 330f, H = 96f;
        private static Vector2 pos = new Vector2(-1f, -1f);
        private static bool dragging;
        private static Vector2 grab;
        private static Font font;
        private static GUIStyle title, artist, small, icon;

        public static void Draw()
        {
            if (!Shown || SystemMedia.Instance == null) return;
            if (XRSettings.isDeviceActive && !MenuGUI.DrawInVR) return;
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

                SystemMedia m = SystemMedia.Instance;
                MenuTheme th = Theme.Current;
                Color acc = Theme.Accent, acc2 = Theme.Accent2;
                Rect panel = new Rect(pos.x, pos.y, W, H);
                Event e = Event.current;

                Rect dragBar = new Rect(panel.x, panel.y, panel.width - 80f, 40f);
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

                DrawArt(new Rect(panel.x + 12f, panel.y + 12f, 44f, 44f), m.Playing);

                string track = m.Found ? (m.Playing ? m.Track : m.Status) : "Spotify not open";
                string by = m.Found && m.Playing ? m.Artist : (m.Found ? m.App : "start a song to see it here");
                if (track.Length > 28) track = track.Substring(0, 27) + "…";
                if (by.Length > 32) by = by.Substring(0, 31) + "…";

                GUI.color = th.Text;
                GUI.Label(new Rect(panel.x + 66f, panel.y + 10f, W - 150f, 20f), track, title);
                GUI.color = Theme.WithA(th.Text, 0.62f);
                GUI.Label(new Rect(panel.x + 66f, panel.y + 30f, W - 150f, 18f), by, artist);

                GUI.color = Theme.WithA(acc, 0.85f);
                GUI.Label(new Rect(panel.xMax - 78f, panel.y + 12f, 66f, 16f), m.Playing ? "SPOTIFY" : m.Found ? "PAUSED" : "", small);

                Rect bar = new Rect(panel.x + 16f, panel.y + 58f, W - 32f, 6f);
                DrawWave(bar, acc, acc2, m.Playing);
                GUI.color = Theme.WithA(th.Text, 0.6f);
                GUI.Label(new Rect(panel.x + 16f, panel.y + 66f, 80f, 18f), m.Playing ? SystemMedia.Clock(m.Elapsed) : "--:--", small);

                float by2 = panel.y + 66f;
                if (Btn(new Rect(panel.center.x - 58f, by2, 32f, 24f), "◀◀", acc, th)) m.Prev();
                if (Btn(new Rect(panel.center.x - 18f, by2, 36f, 24f), m.Playing ? "‖" : "▶", acc, th)) m.PlayPause();
                if (Btn(new Rect(panel.center.x + 26f, by2, 32f, 24f), "▶▶", acc, th)) m.Next();

                Edge(panel, 14f, Theme.WithA(Theme.Lighten(acc, 0.3f), th.Edge));
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private static void DrawArt(Rect r, bool playing)
        {
            Fill(r, 8f, Theme.WithA(Color.Lerp(Theme.Current.Panel, Color.black, 0.3f), 1f));
            float split = 1.5f + (playing ? Mathf.Sin(Time.unscaledTime * 3f) * 1.4f : 0.5f);
            Vector2 c = r.center;
            float size = 30f;
            Vector2 off = Tex.RiftAxis * split;
            GUI.color = Theme.Accent;
            GUI.DrawTexture(new Rect(c.x - size / 2f - off.x, c.y - size / 2f - off.y, size, size), Tex.RiftLeft);
            GUI.color = Theme.Accent2;
            GUI.DrawTexture(new Rect(c.x - size / 2f + off.x, c.y - size / 2f + off.y, size, size), Tex.RiftRight);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(c.x - 5f, c.y - 5f, 10f, 10f), Tex.Star);
            Edge(r, 8f, Theme.WithA(Theme.Accent, 0.5f));
        }

        private static void DrawWave(Rect r, Color acc, Color acc2, bool playing)
        {
            int bars = 28;
            float bw = r.width / bars;
            for (int i = 0; i < bars; i++)
            {
                float h = playing
                    ? (0.3f + 0.7f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f + i * 0.5f) * Mathf.Cos(Time.unscaledTime * 2f + i)))
                    : 0.18f;
                float bh = r.height * h * 2.4f;
                Color c = Color.Lerp(acc, acc2, i / (float)bars);
                Fill(new Rect(r.x + i * bw, r.center.y - bh / 2f, bw - 1.5f, bh), 1f, Theme.WithA(c, playing ? 0.9f : 0.4f));
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

        private static void BuildStyles()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title = new GUIStyle { font = font, fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, richText = true, normal = { textColor = Color.white } };
            artist = new GUIStyle { font = font, fontSize = 12, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            small = new GUIStyle { font = font, fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            icon = new GUIStyle { font = font, fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }
    }
}
