using System;
using UnityEngine;

namespace RivenMenu
{
    public static class Logo
    {
        private static Texture2D texture;
        public static bool Failed { get; private set; }

        public static Texture2D Texture
        {
            get
            {
                if (texture != null || Failed) return texture;
                try
                {
                    byte[] bytes = Convert.FromBase64String(Assets.LogoPngBase64);
                    Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                    if (t.LoadImage(bytes))
                    {
                        t.wrapMode = TextureWrapMode.Clamp;
                        t.filterMode = FilterMode.Bilinear;
                        texture = t;
                    }
                    else Failed = true;
                }
                catch (Exception e)
                {
                    Failed = true;
                    Debug.LogWarning("[Riven] logo decode failed: " + e.Message);
                }
                return texture;
            }
        }
    }
}
