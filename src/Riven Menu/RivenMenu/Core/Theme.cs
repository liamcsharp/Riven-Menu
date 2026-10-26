using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivenMenu
{
    public class MenuTheme
    {
        public string Name;
        public string Title = "riven.menu";
        public Color Accent;
        public Color Accent2;
        public Color Panel;
        public Color Row;
        public Color Text = new Color(0.94f, 0.95f, 1f);
        public float Radius = 18f;
        public float RowRadius = 12f;
        public float Opacity = 0.94f;
        public float Edge = 0.35f;
        public bool Upper = true;
        public bool Particles = true;
        public bool Rainbow;
        public bool Scanlines;
    }

    public static class Theme
    {
        private static Color C(int hex) =>
            new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);

        public static readonly List<MenuTheme> All = new List<MenuTheme>
        {
            new MenuTheme { Name = "Riven", Accent = C(0x9B7BFF), Accent2 = C(0x5CC8FF), Panel = C(0x151327), Row = C(0x3A3570) },
            new MenuTheme { Name = "Seralyth", Title = "seralyth", Accent = C(0xC05CFF), Accent2 = C(0xFF5CC8), Panel = C(0x140B1F), Row = C(0x3B1F57), RowRadius = 10f },
            new MenuTheme { Name = "ii's Stupid", Title = "ii's stupid menu", Accent = C(0xFF8A2B), Accent2 = C(0xFFC46B), Panel = C(0x1A1714), Row = C(0x3C342D), Radius = 10f, RowRadius = 6f, Particles = false },
            new MenuTheme { Name = "CHUD OS", Title = "CHUD_OS://", Accent = C(0x39FF6A), Accent2 = C(0x00C2FF), Panel = C(0x040A05), Row = C(0x0E2414), Radius = 4f, RowRadius = 2f, Edge = 0.6f, Scanlines = true },
            new MenuTheme { Name = "Hamburbur", Title = "hamburbur", Accent = C(0xFFB347), Accent2 = C(0xE23D28), Panel = C(0x2A170C), Row = C(0x6B3E1F), Radius = 22f, RowRadius = 16f },
            new MenuTheme { Name = "Atlas", Title = "atlas", Accent = C(0x3D8BFF), Accent2 = C(0x7DE3FF), Panel = C(0x0A1426), Row = C(0x1B3360), RowRadius = 8f },
            new MenuTheme { Name = "Elixir", Title = "elixir", Accent = C(0xFF5CB8), Accent2 = C(0x4DFFD8), Panel = C(0x1A0D1E), Row = C(0x4A2150), Radius = 20f, RowRadius = 14f },
            new MenuTheme { Name = "Shiba", Title = "shiba", Accent = C(0xFFA24C), Accent2 = C(0xFFE0B0), Panel = C(0x22160D), Row = C(0x5A3A22) },
            new MenuTheme { Name = "Void", Title = "void", Accent = C(0xFFFFFF), Accent2 = C(0x9A9A9A), Panel = C(0x000000), Row = C(0x161616), Edge = 0.2f, Particles = false },
            new MenuTheme { Name = "Midnight", Accent = C(0x6C8CFF), Accent2 = C(0xB39CFF), Panel = C(0x070A1A), Row = C(0x18204A) },
            new MenuTheme { Name = "Cherry", Accent = C(0xFF4D6D), Accent2 = C(0xFFB3C1), Panel = C(0x1F0A10), Row = C(0x5A1A2A) },
            new MenuTheme { Name = "Mint", Accent = C(0x4DFFB8), Accent2 = C(0xB8FFE4), Panel = C(0x0A1A15), Row = C(0x1D4A3C) },
            new MenuTheme { Name = "Ocean", Accent = C(0x2EC5FF), Accent2 = C(0x2E6BFF), Panel = C(0x061623), Row = C(0x123C5C) },
            new MenuTheme { Name = "Sunset", Accent = C(0xFF7A45), Accent2 = C(0xFF3D8B), Panel = C(0x1F0E12), Row = C(0x5C2A2A) },
            new MenuTheme { Name = "Matrix", Title = "matrix", Accent = C(0x00FF41), Accent2 = C(0x008F11), Panel = C(0x000300), Row = C(0x002B0B), Radius = 0f, RowRadius = 0f, Edge = 0.7f, Scanlines = true, Upper = false },
            new MenuTheme { Name = "Monochrome", Accent = C(0xE6E6E6), Accent2 = C(0x8C8C8C), Panel = C(0x141414), Row = C(0x333333), Particles = false },
            new MenuTheme { Name = "Gold", Accent = C(0xFFD34D), Accent2 = C(0xFF9F1C), Panel = C(0x1A1405), Row = C(0x4D3B0F) },
            new MenuTheme { Name = "Blood", Accent = C(0xFF1F3D), Accent2 = C(0x8A0015), Panel = C(0x120204), Row = C(0x3D0811), RowRadius = 6f },
            new MenuTheme { Name = "Frost", Accent = C(0xB8ECFF), Accent2 = C(0x6FB7FF), Panel = C(0x0D1A24), Row = C(0x2B4A63), Radius = 24f, RowRadius = 16f },
            new MenuTheme { Name = "Toxic", Accent = C(0xB6FF1F), Accent2 = C(0xFFF11F), Panel = C(0x0F1405), Row = C(0x2F3D0F) },
            new MenuTheme { Name = "Candy", Accent = C(0xFF8AD8), Accent2 = C(0x8AD8FF), Panel = C(0x241426), Row = C(0x5C3366), Radius = 26f, RowRadius = 20f },
            new MenuTheme { Name = "Retro", Title = "retro.exe", Accent = C(0xFF2BD6), Accent2 = C(0x2BF5FF), Panel = C(0x12072A), Row = C(0x34155C), Radius = 6f, RowRadius = 4f, Scanlines = true },
            new MenuTheme { Name = "Glass", Accent = C(0xFFFFFF), Accent2 = C(0xA8C8FF), Panel = C(0x6A7BA8), Row = C(0x9AA9D6), Opacity = 0.55f, Edge = 0.6f },
            new MenuTheme { Name = "Lunar", Accent = C(0xD9DEF0), Accent2 = C(0x8E9BC2), Panel = C(0x101219), Row = C(0x2E3346) },
            new MenuTheme { Name = "Galaxy", Accent = C(0xA35CFF), Accent2 = C(0x3DA5FF), Panel = C(0x07031A), Row = C(0x24124D) },
            new MenuTheme { Name = "Forest", Accent = C(0x7BD66A), Accent2 = C(0xD6C26A), Panel = C(0x0C160B), Row = C(0x264221) },
            new MenuTheme { Name = "Lava", Accent = C(0xFF5A1F), Accent2 = C(0xFFC21F), Panel = C(0x1A0702), Row = C(0x4D1A08) },
            new MenuTheme { Name = "Rainbow", Accent = Color.white, Accent2 = Color.white, Panel = C(0x101010), Row = C(0x2E2E2E), Rainbow = true },
        };

        public static readonly string[] Names = All.Select(t => t.Name).ToArray();

        public static int Index { get; private set; }
        public static MenuTheme Current => All[Index];

        public static void Set(int index) => Index = Mathf.Clamp(index, 0, All.Count - 1);

        public static Color Accent => Current.Rainbow ? Hue(0f) : Current.Accent;
        public static Color Accent2 => Current.Rainbow ? Hue(0.33f) : Current.Accent2;
        public static string AccentHex => ColorUtility.ToHtmlStringRGB(Accent);

        public static Color Hue(float offset)
        {
            float h = Mathf.Repeat(Time.unscaledTime * 0.08f + offset, 1f);
            return Color.HSVToRGB(h, 0.55f, 1f);
        }

        public static Color Lighten(Color c, float t) => Color.Lerp(c, Color.white, t);

        public static Color WithA(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
