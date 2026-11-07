using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class Players
    {
        private const string Cat = "Players";

        [MenuButton("Known People Alerts", Category = Cat, Order = 0, Default = true, Notify = false, OnEnable = nameof(KnownOn), OnDisable = nameof(KnownOff), Tooltip = "Tells you when a known player from the aceutils list is in your lobby")]
        public static void KnownPeopleAlerts() { }
        public static void KnownOn() => Social.KnownAlerts = true;
        public static void KnownOff() => Social.KnownAlerts = false;

        [MenuButton("Check Lobby", ButtonType.Button, Category = Cat, Order = 1, Notify = false, Tooltip = "Lists every known person in your current lobby")]
        public static void CheckLobby() => Social.CheckLobby();

        [MenuButton("Reload Known List", ButtonType.Button, Category = Cat, Order = 2, Notify = false, Tooltip = "Downloads the newest known people list")]
        public static void ReloadList()
        {
            Social.Reload();
            Notify.Push(Social.ListStatus);
        }

        private static readonly RigPool knownTags = new RigPool(Visuals.NewTag);

        [MenuButton("Known People Tags", Category = Cat, Order = 3, Default = true, Notify = false, OnDisable = nameof(KnownTagsOff), Tooltip = "Shows a gold tag with their name above known people")]
        public static void KnownPeopleTags() => knownTags.Sync((r, go) =>
        {
            string who = Social.KnownName(r);
            bool show = who != null;
            if (go.activeSelf != show) go.SetActive(show);
            if (!show) return;
            TextMesh tm = go.GetComponent<TextMesh>();
            Util.SetText(tm, "★ " + who + " ★");
            Util.SetColor(tm, new Color(1f, 0.82f, 0.25f));
            go.transform.position = GunLib.HeadOf(r) + Vector3.up * 0.62f;
            Visuals.Billboard(go.transform);
        });
        public static void KnownTagsOff() => knownTags.Clear();

        [MenuButton("Riven User ESP", Category = Cat, Order = 4, Notify = false, OnEnable = nameof(RivenOn), OnDisable = nameof(RivenOff), Tooltip = "Shows the Riven logo over other Riven users. Only Riven users with this on can see each other")]
        public static void RivenUserEsp() => rivenLogos.Sync((r, go) =>
        {
            bool show = Social.IsRiven(r);
            if (go.activeSelf != show) go.SetActive(show);
            if (!show) return;
            float bob = Mathf.Sin(Time.time * 2.5f + r.GetInstanceID()) * 0.03f;
            go.transform.position = GunLib.HeadOf(r) + Vector3.up * (0.55f + bob);
            Visuals.Billboard(go.transform);
        });
        public static void RivenOn() => Social.RivenEsp = true;
        public static void RivenOff()
        {
            Social.RivenEsp = false;
            rivenLogos.Clear();
        }

        [MenuButton("Riven User Alerts", Category = Cat, Order = 5, Default = true, Notify = false, OnEnable = nameof(RivenAlertOn), OnDisable = nameof(RivenAlertOff), Tooltip = "Pops up when another Riven user is in your lobby (needs Riven User ESP)")]
        public static void RivenUserAlerts() { }
        public static void RivenAlertOn() => Social.RivenAlerts = true;
        public static void RivenAlertOff() => Social.RivenAlerts = false;

        private static readonly RigPool rivenLogos = new RigPool(NewLogo);
        private static Material logoMat;

        private static Shader ColorShader()
        {
            foreach (string name in new[] { "Sprites/Default", "Unlit/Transparent", "UI/Default", "Legacy Shaders/Particles/Alpha Blended" })
            {
                Shader s = Shader.Find(name);
                if (s != null) return s;
            }
            return Shader.Find("GUI/Text Shader");
        }

        private static GameObject NewLogo(VRRig r)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "RivenUserLogo";
            go.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            if (logoMat == null)
            {
                logoMat = new Material(ColorShader()) { mainTexture = Logo.Texture, color = Color.white, renderQueue = 3500 };
            }
            Renderer rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = logoMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go;
        }

        [MenuButton("Riven Users Here", ButtonType.Button, Category = Cat, Order = 6, Notify = false, Tooltip = "Counts the Riven users in your lobby")]
        public static void RivenUsersHere()
        {
            if (!Social.RivenEsp) { Notify.Push("Turn on Riven User ESP first"); return; }
            int n = Social.RivenUsers.Count;
            Notify.Push(n == 0 ? "No other Riven users here" : n == 1 ? "1 other Riven user here" : n + " other Riven users here");
        }

        [MenuButton("Player List HUD", Category = Cat, Order = 7, OnEnable = nameof(ListOn), OnDisable = nameof(ListOff), Tooltip = "Shows everyone in your room on screen, known people and Riven users are marked")]
        public static void PlayerListHud() { }
        public static void ListOn() => Hud.Show("playerlist", PlayerList);
        public static void ListOff() => Hud.Hide("playerlist");

        private static string cachedList = "";
        private static float nextList;

        private static string PlayerList()
        {
            if (Time.unscaledTime < nextList) return cachedList;
            nextList = Time.unscaledTime + 0.5f;
            NetworkSystem net = NetworkSystem.Instance;
            if (net == null || !net.InRoom) return cachedList = "Not in a room";
            List<string> lines = new List<string> { "Players " + net.RoomPlayerCount + "/10" };
            foreach (VRRig r in Util.Rigs)
            {
                if (r == null || r.Creator == null) continue;
                string line = r.Creator.NickName;
                if (r.Creator.IsLocal) line += " (you)";
                if (Util.IsTagged(r)) line += "  [IT]";
                string k = Social.KnownName(r);
                if (k != null) line += "  ★ " + k;
                if (Social.IsRiven(r)) line += "  [RIVEN]";
                lines.Add(line);
            }
            return cachedList = string.Join("\n", lines);
        }

        [MenuButton("Copy Player IDs", ButtonType.Button, Category = Cat, Order = 8, Notify = false, Tooltip = "Copies everyone's name and player ID in your room to your clipboard")]
        public static void CopyIds()
        {
            NetworkSystem net = NetworkSystem.Instance;
            if (net == null || !net.InRoom) { Notify.Push("Not in a room"); return; }
            List<string> lines = new List<string>();
            foreach (NetPlayer p in net.AllNetPlayers)
                if (p != null) lines.Add(p.NickName + " - " + p.UserId);
            GUIUtility.systemCopyBuffer = string.Join("\n", lines);
            Notify.Push("Copied " + lines.Count + " player IDs");
        }
    }
}
