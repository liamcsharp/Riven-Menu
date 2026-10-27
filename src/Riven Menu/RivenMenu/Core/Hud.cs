using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivenMenu
{
    public static class Hud
    {
        private static readonly List<Func<string>> lines = new List<Func<string>>();
        private static readonly Dictionary<string, Func<string>> named = new Dictionary<string, Func<string>>();
        public static Action Overlay;
        private static GUIStyle style;
        private static float fps, fpsTimer;
        private static int frames;

        public static float Fps => fps;

        public static void Show(string id, Func<string> line)
        {
            if (named.ContainsKey(id)) return;
            named[id] = line;
            lines.Add(line);
        }

        public static void Hide(string id)
        {
            if (!named.TryGetValue(id, out Func<string> line)) return;
            named.Remove(id);
            lines.Remove(line);
        }

        public static void Draw()
        {
            if (Event.current.type == EventType.Layout)
            {
                frames++;
                fpsTimer += Time.unscaledDeltaTime;
                if (fpsTimer >= 0.5f)
                {
                    fps = frames / fpsTimer;
                    frames = 0;
                    fpsTimer = 0f;
                }
            }

            if (lines.Count == 0 && Overlay == null) return;

            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
            try
            {
                if (style == null)
                {
                    Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    style = new GUIStyle { font = font, fontSize = 15, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = Color.white } };
                }

                float y = 12f;
                string hex = Theme.AccentHex;
                foreach (Func<string> line in lines)
                {
                    string text;
                    try { text = line(); }
                    catch { continue; }
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.Label(new Rect(Screen.width - 361f, y + 1f, 350f, 22f), text, style);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(Screen.width - 362f, y, 350f, 22f), $"<color=#{hex}>|</color> {text}", style);
                    y += 22f;
                }

                Overlay?.Invoke();
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }
    }
}
