using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivenMenu.Mods
{
    public static class NameTags
    {
        private const string Cat = "Name Tags";

        private class Info
        {
            public Vector3 LastPos;
            public float Speed;
            public float NextText;
        }

        private static readonly Dictionary<VRRig, Info> info = new Dictionary<VRRig, Info>();
        private static readonly StringBuilder sb = new StringBuilder(128);
        private static Material wallMat;
        private static Font font;

        private static float SizeScale
        {
            get
            {
                switch (Menu.Option("Tag Size"))
                {
                    case "Small": return 0.7f;
                    case "Big": return 1.4f;
                    case "Huge": return 2f;
                    default: return 1f;
                }
            }
        }

        private static float Height
        {
            get
            {
                switch (Menu.Option("Tag Height"))
                {
                    case "Low": return 0.3f;
                    case "High": return 0.75f;
                    default: return 0.48f;
                }
            }
        }

        private static GameObject NewTag(VRRig r)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject go = new GameObject("RivenNameTag");
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 56;
            tm.characterSize = 0.011f;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.richText = true;
            tm.fontStyle = FontStyle.Bold;
            go.GetComponent<MeshRenderer>().material = font.material;
            return go;
        }

        private static Material WallMaterial
        {
            get
            {
                if (wallMat == null)
                {
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    wallMat = new Material(font.material) { renderQueue = 4000 };
                    wallMat.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                }
                return wallMat;
            }
        }

        private static readonly RigPool tags = new RigPool(NewTag);

        private static string ColorCode(Color c) =>
            Mathf.RoundToInt(c.r * 9f) + " " + Mathf.RoundToInt(c.g * 9f) + " " + Mathf.RoundToInt(c.b * 9f);

        [MenuButton("Name Tags", Category = Cat, Order = 0, OnDisable = nameof(Off), Tooltip = "Shows a name tag above everyone, change what it shows below")]
        public static void Tags()
        {
            float now = Time.time;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 me = Util.BodyT != null ? Util.BodyT.position : Vector3.zero;
            bool walls = Menu.IsOn("Tags Through Walls");
            bool hideFar = Menu.IsOn("Hide Far Tags");
            bool fade = Menu.IsOn("Fade Far Tags");
            float scale = SizeScale;
            float height = Height;
            string mode = Menu.Option("Tag Info");

            tags.Sync((r, go) =>
            {
                if (!info.TryGetValue(r, out Info inf))
                {
                    inf = new Info { LastPos = r.transform.position };
                    info[r] = inf;
                }
                Vector3 pos = r.transform.position;
                float inst = (pos - inf.LastPos).magnitude / dt;
                inf.Speed = Mathf.Lerp(inf.Speed, inst, 0.1f);
                inf.LastPos = pos;

                float dist = Vector3.Distance(me, pos);
                bool show = !hideFar || dist < 25f;
                if (go.activeSelf != show) go.SetActive(show);
                if (!show) return;

                TextMesh tm = go.GetComponent<TextMesh>();
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                Material want = walls ? WallMaterial : font.material;
                if (mr.sharedMaterial != want) mr.sharedMaterial = want;

                float k = scale * Mathf.Clamp(dist / 6f, 1f, 3f) * 0.011f;
                if (!Mathf.Approximately(tm.characterSize, k)) tm.characterSize = k;

                Color c = Util.PlayerColor(r);
                if (fade) c.a *= Mathf.Clamp01(1.2f - dist / 40f);
                Util.SetColor(tm, c);

                if (now >= inf.NextText)
                {
                    inf.NextText = now + 0.15f;
                    Util.SetText(tm, Build(r, mode, dist, inf.Speed));
                }

                go.transform.position = GunLib.HeadOf(r) + Vector3.up * height;
                Visuals.Billboard(go.transform);
            });
        }

        private static string Build(VRRig r, string mode, float dist, float speed)
        {
            sb.Length = 0;
            bool everything = mode == "Everything";
            string known = Social.KnownName(r);
            if (known != null) sb.Append("<color=#FFD040>★ ").Append(known).Append(" ★</color>\n");
            if (Social.IsRiven(r)) sb.Append("<color=#").Append(Theme.AccentHex).Append(">[RIVEN]</color> ");
            if (Menu.IsOn("Show Tagged Icon") && Util.IsTagged(r)) sb.Append("<color=#FF5050>[IT]</color> ");
            sb.Append(GunLib.NameOf(r));

            if (mode == "Name + Distance" || everything) sb.Append("\n<size=40>").Append(dist.ToString("0.0")).Append("m</size>");
            if (mode == "Name + Status" || everything)
                sb.Append(mode == "Name + Status" ? "\n<size=40>" : "<size=40>  ").Append(Util.IsTagged(r) ? "tagged" : "running").Append("</size>");
            if (Menu.IsOn("Show Speed") || everything) sb.Append("\n<size=36>").Append(speed.ToString("0.0")).Append(" m/s</size>");
            if (Menu.IsOn("Show Color Codes") || everything) sb.Append("\n<size=36>color ").Append(ColorCode(r.playerColor)).Append("</size>");
            if (Menu.IsOn("Show Player IDs") && r.Creator != null) sb.Append("\n<size=32>").Append(r.Creator.UserId).Append("</size>");
            return sb.ToString();
        }

        public static void Off()
        {
            tags.Clear();
            info.Clear();
        }

        [MenuButton("Tag Info", ButtonType.Option, Category = Cat, Order = 1, Options = new[] { "Name", "Name + Distance", "Name + Status", "Everything" }, Notify = false, Tooltip = "What the name tag shows")]
        public static void TagInfo() { }

        [MenuButton("Tag Size", ButtonType.Option, Category = Cat, Order = 2, Options = new[] { "Normal", "Small", "Big", "Huge" }, Notify = false, Tooltip = "How big name tags are")]
        public static void TagSize() { }

        [MenuButton("Tag Height", ButtonType.Option, Category = Cat, Order = 3, Options = new[] { "Normal", "Low", "High" }, Notify = false, Tooltip = "How high above heads name tags float")]
        public static void TagHeight() { }

        [MenuButton("Tags Through Walls", Category = Cat, Order = 4, Notify = false, Tooltip = "Name tags show through walls")]
        public static void ThroughWalls() { }

        [MenuButton("Show Tagged Icon", Category = Cat, Order = 5, Default = true, Notify = false, Tooltip = "Adds a red [IT] to tagged players")]
        public static void TaggedIcon() { }

        [MenuButton("Show Speed", Category = Cat, Order = 6, Notify = false, Tooltip = "Shows how fast each player is moving")]
        public static void ShowSpeed() { }

        [MenuButton("Show Color Codes", Category = Cat, Order = 7, Notify = false, Tooltip = "Shows each player's color code")]
        public static void ShowColors() { }

        [MenuButton("Show Player IDs", Category = Cat, Order = 8, Notify = false, Tooltip = "Shows each player's ID under their name")]
        public static void ShowIds() { }

        [MenuButton("Hide Far Tags", Category = Cat, Order = 9, Notify = false, Tooltip = "Hides tags for players over 25m away")]
        public static void HideFar() { }

        [MenuButton("Fade Far Tags", Category = Cat, Order = 10, Default = true, Notify = false, Tooltip = "Tags fade out the further away people are")]
        public static void FadeFar() { }

        private static readonly RigPool distTags = new RigPool(Visuals.NewTag);

        [MenuButton("Distance Tags", Category = Cat, Order = 11, OnDisable = nameof(DistOff), Tooltip = "Just shows how far away everyone is")]
        public static void DistanceTags()
        {
            Vector3 me = Util.BodyT != null ? Util.BodyT.position : Vector3.zero;
            distTags.Sync((r, go) =>
            {
                TextMesh tm = go.GetComponent<TextMesh>();
                float d = Vector3.Distance(me, r.transform.position);
                Util.SetText(tm, d.ToString("0") + "m");
                Util.SetColor(tm, Color.white);
                go.transform.position = GunLib.HeadOf(r) + Vector3.up * 0.3f;
                Visuals.Billboard(go.transform);
            });
        }
        public static void DistOff() => distTags.Clear();

        private static readonly RigPool roleTags = new RigPool(Visuals.NewTag);

        [MenuButton("Tagger Labels", Category = Cat, Order = 12, OnDisable = nameof(RoleOff), Tooltip = "A big IT label floats over every tagged player")]
        public static void TaggerLabels() => roleTags.Sync((r, go) =>
        {
            bool it = Util.IsTagged(r);
            if (go.activeSelf != it) go.SetActive(it);
            if (!it) return;
            TextMesh tm = go.GetComponent<TextMesh>();
            Util.SetText(tm, "IT");
            Util.SetColor(tm, new Color(1f, 0.3f + Mathf.Sin(Time.time * 6f) * 0.15f, 0.3f));
            tm.characterSize = 0.03f;
            go.transform.position = GunLib.HeadOf(r) + Vector3.up * 0.95f;
            Visuals.Billboard(go.transform);
        });
        public static void RoleOff() => roleTags.Clear();
    }
}
