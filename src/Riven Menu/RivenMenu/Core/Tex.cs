using System.Collections.Generic;
using UnityEngine;

namespace RivenMenu
{
    public static class Tex
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

        private static Texture2D New(int w, int h, TextureWrapMode wrap = TextureWrapMode.Clamp) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = wrap,
                hideFlags = HideFlags.HideAndDontSave
            };

        private static float RoundedSd(float fx, float fy, float w, float h, float r)
        {
            float dx = Mathf.Max(r - fx, fx - (w - r), 0f);
            float dy = Mathf.Max(r - fy, fy - (h - r), 0f);
            float outside = Mathf.Sqrt(dx * dx + dy * dy) - r;
            if (dx > 0f || dy > 0f) return outside;
            return -Mathf.Min(Mathf.Min(fx, w - fx), Mathf.Min(fy, h - fy));
        }

        private static Texture2D Build(string key, int w, int h, System.Func<float, float, Color> pixel,
            TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            if (cache.TryGetValue(key, out Texture2D found) && found != null) return found;
            Texture2D t = New(w, h, wrap);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x + 0.5f, y + 0.5f);
            t.SetPixels(px);
            t.Apply();
            cache[key] = t;
            return t;
        }

        private static int SliceSize(float r, int pad) => Mathf.CeilToInt(r) * 2 + pad * 2 + 4;

        public static GUIStyle FillStyle(float radius)
        {
            string key = "fill" + radius;
            if (styles.TryGetValue(key, out GUIStyle s) && s.normal.background != null) return s;
            int size = SliceSize(radius, 1);
            float r = Mathf.Max(radius, 0.01f);
            Texture2D t = Build(key, size, size, (x, y) =>
            {
                float a = Mathf.Clamp01(0.5f - RoundedSd(x, y, size, size, r));
                float shade = Mathf.Lerp(0.8f, 1f, y / size);
                return new Color(shade, shade, shade, a);
            });
            return styles[key] = Slice(t, Mathf.CeilToInt(radius) + 2);
        }

        public static GUIStyle EdgeStyle(float radius, float width)
        {
            string key = "edge" + radius + "_" + width;
            if (styles.TryGetValue(key, out GUIStyle s) && s.normal.background != null) return s;
            int size = SliceSize(radius, 1);
            float r = Mathf.Max(radius, 0.01f);
            Texture2D t = Build(key, size, size, (x, y) =>
            {
                float sd = RoundedSd(x, y, size, size, r);
                float outer = Mathf.Clamp01(0.5f - sd);
                float inner = Mathf.Clamp01(0.5f - (sd + width));
                return new Color(1f, 1f, 1f, outer - inner);
            });
            return styles[key] = Slice(t, Mathf.CeilToInt(radius) + 2);
        }

        public static GUIStyle GlowStyle(float radius, int spread)
        {
            string key = "glow" + radius + "_" + spread;
            if (styles.TryGetValue(key, out GUIStyle s) && s.normal.background != null) return s;
            int size = SliceSize(radius + spread, 1);
            float r = radius + spread;
            float inner = size - spread * 2f;
            float ir = Mathf.Max(radius, 0.01f);
            Texture2D t = Build(key, size, size, (x, y) =>
            {
                float sd = RoundedSd(x - spread, y - spread, inner, inner, ir);
                float a = sd <= 0f ? 1f : Mathf.Pow(1f - Mathf.Clamp01(sd / spread), 2.4f);
                return new Color(1f, 1f, 1f, a);
            });
            return styles[key] = Slice(t, Mathf.CeilToInt(r) + 2);
        }

        private static GUIStyle Slice(Texture2D t, int edge) =>
            new GUIStyle { normal = { background = t }, border = new RectOffset(edge, edge, edge, edge) };

        public static Texture2D Circle => Build("circle", 64, 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(31.5f - d));
        });

        public static Texture2D SoftDot => Build("softdot", 32, 32, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(16f, 16f)) / 16f;
            return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2f));
        });

        public static Texture2D Sheen => Build("sheen", 64, 4, (x, y) =>
        {
            float t = x / 64f;
            float a = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 3f);
            return new Color(1f, 1f, 1f, a);
        });

        public static Texture2D Scanline => Build("scan", 4, 4, (x, y) =>
            new Color(0f, 0f, 0f, y < 2f ? 0.18f : 0f), TextureWrapMode.Repeat);

        public static Texture2D Pill(int w, int h) => Build("pill" + w + "x" + h, w, h, (x, y) =>
            new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - RoundedSd(x, y, w, h, h / 2f))));

        public static Texture2D Diamond => Build("diamond", 32, 32, (x, y) =>
        {
            float d = Mathf.Abs(x - 16f) + Mathf.Abs(y - 16f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(15f - d));
        });

        public static Texture2D Power => Build("power", 64, 64, (x, y) =>
        {
            float c = 32f, R = 21f, th = 4.2f;
            float vx = x - c, vy = y - c;
            float ring = Mathf.Abs(Mathf.Sqrt(vx * vx + vy * vy) - R) - th;
            if (Mathf.Abs(Mathf.Atan2(vx, vy)) < 0.62f) ring = 99f;
            float ly = Mathf.Clamp(vy, 2f, R + 2f);
            float line = Mathf.Sqrt(vx * vx + (vy - ly) * (vy - ly)) - th;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - Mathf.Min(ring, line)));
        });

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static Texture2D DoorArrow(string key, float doorX, float doorEdge, float arrowFrom, float arrowTo) =>
            Build(key, 64, 64, (x, y) =>
            {
                const float th = 3.6f;
                float d = Mathf.Min(Seg(x, y, doorEdge, 12f, doorX, 12f),
                    Mathf.Min(Seg(x, y, doorX, 12f, doorX, 52f), Seg(x, y, doorX, 52f, doorEdge, 52f)));
                float head = arrowTo - 11f * Mathf.Sign(arrowTo - arrowFrom);
                d = Mathf.Min(d, Seg(x, y, arrowFrom, 32f, arrowTo, 32f));
                d = Mathf.Min(d, Mathf.Min(Seg(x, y, arrowTo, 32f, head, 21f), Seg(x, y, arrowTo, 32f, head, 43f)));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(th + 0.5f - d));
            });

        public static Texture2D Leave => DoorArrow("leave", 12f, 30f, 24f, 53f);
        public static Texture2D Rejoin => DoorArrow("rejoin", 52f, 34f, 9f, 40f);

        private static float PolyCoverage(float x, float y, Vector2[] pts)
        {
            bool inside = false;
            float min = float.MaxValue;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                Vector2 a = pts[i], b = pts[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                min = Mathf.Min(min, Seg(x, y, a.x, a.y, b.x, b.y));
            }
            return Mathf.Clamp01(inside ? 0.5f + min : 0.5f - min);
        }

        private static Texture2D Poly(string key, int size, float box, params float[] xy)
        {
            Vector2[] pts = new Vector2[xy.Length / 2];
            float k = size / box;
            for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(xy[i * 2] * k, size - xy[i * 2 + 1] * k);
            return Build(key, size, size, (x, y) => new Color(1f, 1f, 1f, PolyCoverage(x, y, pts)));
        }

        public static Texture2D RiftLeft => Poly("riftL", 96, 200f, 100f, 10f, 122.5f, 32.5f, 77.5f, 167.5f, 10f, 100f);
        public static Texture2D RiftRight => Poly("riftR", 96, 200f, 122.5f, 32.5f, 190f, 100f, 100f, 190f, 77.5f, 167.5f);
        public static Texture2D Star => Poly("star", 64, 64f, 32f, 4f, 37f, 27f, 60f, 32f, 37f, 37f, 32f, 60f, 27f, 37f, 4f, 32f, 27f, 27f);
        public static Texture2D DiamondOutline => Build("diamondOutline", 64, 64, (x, y) =>
        {
            float d = Mathf.Abs(Mathf.Abs(x - 32f) + Mathf.Abs(y - 32f) - 26f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(3.2f - d));
        });

        public static readonly Vector2 RiftAxis = new Vector2(0.949f, 0.316f);
    }
}
