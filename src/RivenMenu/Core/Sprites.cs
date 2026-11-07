using UnityEngine;

namespace RivenMenu
{
    public static class Sprites
    {
        private static Sprite rounded;
        private static Sprite soft;

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = Make(RoundedTex(48, 14), 14);
                return rounded;
            }
        }

        public static Sprite Soft
        {
            get
            {
                if (soft == null) soft = Make(SoftTex(64), 0);
                return soft;
            }
        }

        private static Sprite Make(Texture2D tex, int border)
        {
            Vector4 b = new Vector4(border, border, border, border);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        private static Texture2D RoundedTex(int size, float radius)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float dx = Mathf.Max(radius - fx, fx - (size - radius), 0f);
                    float dy = Mathf.Max(radius - fy, fy - (size - radius), 0f);
                    float sd = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - sd));
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        private static Texture2D SoftTex(int size)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] px = new Color[size * size];
            Vector2 c = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (size / 2f);
                    px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2f));
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
