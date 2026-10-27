using UnityEngine;

namespace RivenMenu
{
    public class Splash : MonoBehaviour
    {
        private const float Hold = 2.6f, FadeIn = 0.4f, FadeOut = 0.6f;
        private float t;
        private bool played;
        private Font font;
        private GUIStyle title, sub;

        public static void Show()
        {
            if (PlayerPrefs.GetInt("RivenMenu.NoSplash", 0) == 1) return;
            new GameObject("RivenSplash").AddComponent<Splash>();
        }

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            if (!played && t > 0.25f)
            {
                played = true;
                if (RivenAudio.Instance != null) RivenAudio.Instance.PlayWelcome();
            }
            float life = Hold + FadeOut;
            if (t >= life) Destroy(gameObject);
        }

        private void OnGUI()
        {
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -2000;

            try
            {
                if (title == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    title = new GUIStyle { font = font, fontSize = 46, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = Color.white } };
                    sub = new GUIStyle { font = font, fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true, normal = { textColor = Color.white } };
                }

                float alpha = Mathf.Clamp01(t / FadeIn);
                if (t > Hold) alpha = Mathf.Clamp01(1f - (t - Hold) / FadeOut);

                float cx = Screen.width / 2f, cy = Screen.height / 2f;
                GUI.color = new Color(0f, 0f, 0f, 0.82f * alpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

                float intro = Mathf.Clamp01(t / 0.8f);
                float pop = 1f - Mathf.Pow(1f - intro, 3f);
                float size = 190f * Mathf.LerpUnclamped(0.4f, 1f, pop);
                Vector2 c = new Vector2(cx, cy - 70f);

                float spin = Mathf.Sin(t * 1.1f) * 22f + (1f - pop) * 180f;
                float breathe = 1f + Mathf.Sin(t * 3f) * 0.04f;
                float split = (2f + Mathf.Sin(t * 2.2f) * 1.6f) * pop;

                GUI.color = new Color(1f, 1f, 1f, alpha * 0.4f);
                Tint(new Rect(c.x - size * 0.75f, c.y - size * 0.75f, size * 1.5f, size * 1.5f), Tex.SoftDot, new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, alpha * (0.25f + 0.1f * Mathf.Sin(t * 4f))));

                Matrix4x4 saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(spin, c);
                float s = size * breathe;
                DrawMark(c, s, split, alpha);
                GUI.matrix = saved;

                string wave = Wave("RIVEN MENU", alpha);
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(0f, cy + 70f, Screen.width, 60f), wave, title);
                GUI.color = new Color(1f, 1f, 1f, alpha * (0.6f + 0.4f * Mathf.Sin(t * 3f)));
                GUI.Label(new Rect(0f, cy + 125f, Screen.width, 30f), "Welcome To Riven Menu", sub);
                GUI.color = Color.white;
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private void DrawMark(Vector2 c, float size, float gap, float alpha)
        {
            Vector2 off = Tex.RiftAxis * gap;
            Rect r = new Rect(c.x - size / 2f, c.y - size / 2f, size, size);
            GUI.color = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, alpha);
            GUI.DrawTexture(new Rect(r.x - off.x, r.y - off.y, size, size), Tex.RiftLeft);
            GUI.color = new Color(Theme.Accent2.r, Theme.Accent2.g, Theme.Accent2.b, alpha);
            GUI.DrawTexture(new Rect(r.x + off.x, r.y + off.y, size, size), Tex.RiftRight);
            float ss = size * 0.34f;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(c.x - ss / 2f, c.y - ss / 2f, ss, ss), Tex.Star);
            GUI.color = Color.white;
        }

        private void Tint(Rect r, Texture tex, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, tex);
            GUI.color = Color.white;
        }

        private string Wave(string text, float alpha)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int n = Mathf.Max(1, text.Length - 1);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') { sb.Append(' '); continue; }
                float k = 0.5f + 0.5f * Mathf.Sin(t * 3f - i * 0.4f);
                Color col = Color.Lerp(Color.white, Theme.Lighten(Color.Lerp(Theme.Accent, Theme.Accent2, i / (float)n), 0.3f), k);
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(col)).Append('>').Append(text[i]).Append("</color>");
            }
            return sb.ToString();
        }
    }
}
