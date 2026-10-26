using UnityEngine;

namespace RivenMenu.Mods
{
    public static class Settings
    {
        public static string[] ThemeNames => Theme.Names;

        [MenuButton("Menu Theme", ButtonType.Option, Category = "Settings", Order = 0, OptionsFrom = nameof(ThemeNames), Tooltip = "Changes the whole look of the menu")]
        public static void MenuTheme() => Theme.Set(Menu.Index("Menu Theme"));

        [MenuButton("Animation Speed", ButtonType.Option, Category = "Settings", Order = 1, Options = new[] { "Normal", "Fast", "Slow", "Off" }, Tooltip = "How fast menu animations play")]
        public static void AnimationSpeed()
        {
            switch (Menu.Option("Animation Speed"))
            {
                case "Fast": MenuGUI.AnimSpeed = 1.8f; break;
                case "Slow": MenuGUI.AnimSpeed = 0.5f; break;
                case "Off": MenuGUI.AnimSpeed = 0f; break;
                default: MenuGUI.AnimSpeed = 1f; break;
            }
        }

        [MenuButton("Menu Size", ButtonType.Option, Category = "Settings", Order = 2, Options = new[] { "100%", "110%", "120%", "130%", "80%", "90%" }, Tooltip = "Scales the whole menu")]
        public static void MenuSize() => MenuGUI.Scale = float.Parse(Menu.Option("Menu Size").TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture) / 100f;

        [MenuButton("Menu Particles", Category = "Settings", Order = 3, Default = true, Notify = false, OnEnable = nameof(ParticlesOn), OnDisable = nameof(ParticlesOff), Tooltip = "Floating particles inside the menu")]
        public static void MenuParticles() { }
        public static void ParticlesOn() => MenuGUI.ParticlesOn = true;
        public static void ParticlesOff() => MenuGUI.ParticlesOn = false;

        [MenuButton("Menu Glow", Category = "Settings", Order = 4, Default = true, Notify = false, OnEnable = nameof(GlowOn), OnDisable = nameof(GlowOff), Tooltip = "Pulsing glow around the menu")]
        public static void MenuGlow() { }
        public static void GlowOn() => MenuGUI.GlowOn = true;
        public static void GlowOff() => MenuGUI.GlowOn = false;

        [MenuButton("Menu Sounds", Category = "Settings", Order = 5, Default = true, Notify = false, OnEnable = nameof(SoundsOn), OnDisable = nameof(SoundsOff), Tooltip = "Click sounds when you press buttons")]
        public static void MenuSounds() { }
        public static void SoundsOn() => MenuGUI.SoundsOn = true;
        public static void SoundsOff() => MenuGUI.SoundsOn = false;

        [MenuButton("Notifications", Category = "Settings", Order = 6, Default = true, Notify = false, Tooltip = "Popups when you press a button")]
        public static void Notifications() { }

        [MenuButton("Reset Menu Position", ButtonType.Button, Category = "Settings", Order = 7, Tooltip = "Moves the menu back to the corner")]
        public static void ResetMenuPosition() => MenuGUI.ResetPosition();

        [MenuButton("Discord RPC", Category = "Settings", Order = 8, Default = true, Notify = false, OnEnable = nameof(RpcOn), OnDisable = nameof(RpcOff), Tooltip = "Shows Riven Menu on your Discord profile")]
        public static void DiscordRpc() { }
        public static void RpcOn() => SetRpc(true);
        public static void RpcOff() => SetRpc(false);

        private static void SetRpc(bool on)
        {
            DiscordRPC.Enabled = on;
            DiscordRPC.Instance?.Refresh();
        }

        [MenuButton("RPC Show Room Code", Category = "Settings", Order = 9, Notify = false, OnEnable = nameof(RoomCodeOn), OnDisable = nameof(RoomCodeOff), Tooltip = "Shows your room code on Discord so friends can join you")]
        public static void RpcShowRoomCode() { }
        public static void RoomCodeOn() { DiscordRPC.ShowRoomCode = true; DiscordRPC.Instance?.Refresh(); }
        public static void RoomCodeOff() { DiscordRPC.ShowRoomCode = false; DiscordRPC.Instance?.Refresh(); }

        [MenuButton("Join Discord", ButtonType.Button, Category = "Settings", Order = 10, Notify = false, Tooltip = "Opens the Riven Menu Discord server")]
        public static void JoinDiscord() => Application.OpenURL(DiscordRPC.DiscordUrl);

        [MenuButton("Open GitHub", ButtonType.Button, Category = "Settings", Order = 11, Notify = false, Tooltip = "Opens the Riven Menu GitHub to download updates")]
        public static void OpenGitHub() => Application.OpenURL(DiscordRPC.GitHubUrl);

        [MenuButton("Gun Hand", ButtonType.Option, Category = "Settings", Order = 24, Options = new[] { "Right", "Left" }, Notify = false, Tooltip = "Which VR hand aims gun mods")]
        public static void GunHand()
        {
            GunLib.LeftHanded = Menu.Index("Gun Hand") == 1;
            GunLib.Hide();
        }

        [MenuButton("Follow Menu Theme", Category = "Settings", Order = 20, Tooltip = "Visuals use the menu color")]
        public static void FollowMenuTheme() { }

        [MenuButton("Hidden on Camera", Category = "Settings", Order = 21, Tooltip = "Hides visuals from the spectator camera")]
        public static void HiddenOnCamera() { }

        [MenuButton("Transparent Theme", Category = "Settings", Order = 22, Tooltip = "Makes visuals see-through")]
        public static void TransparentTheme() { }

        [MenuButton("Gun Color", ButtonType.Option, Category = "Settings", Order = 23, Options = new[] { "Theme", "White", "Red", "Green", "Cyan" }, Notify = false, Tooltip = "Color of the aim line for gun mods")]
        public static void GunColor() { }

        public static float FlySpeed = 15f;
        private static readonly float[] FlySpeeds = { 15f, 20f, 30f, 45f, 5f, 10f };

        [MenuButton("Fly Speed", ButtonType.Option, Category = "Settings", Order = 30, Options = new[] { "15", "20", "30", "45", "5", "10" }, Notify = false, Tooltip = "Speed for every fly mod")]
        public static void SetFlySpeed() => FlySpeed = FlySpeeds[Menu.Index("Fly Speed")];

        public static float SpeedBoost = 1.3f;
        private static readonly float[] SpeedBoosts = { 1.3f, 1.5f, 1.8f, 2.2f, 1.1f };

        [MenuButton("Speed Boost Amount", ButtonType.Option, Category = "Settings", Order = 31, Options = new[] { "1.3x", "1.5x", "1.8x", "2.2x", "1.1x" }, Notify = false, Tooltip = "Strength of Speed Boost")]
        public static void SetSpeedBoost() => SpeedBoost = SpeedBoosts[Menu.Index("Speed Boost Amount")];

        public static Color PlatformColor = Color.black;

        [MenuButton("Platform Color", ButtonType.Option, Category = "Settings", Order = 32, Options = new[] { "Black", "Theme", "White", "Rainbow" }, Notify = false, Tooltip = "Color of platforms")]
        public static void SetPlatformColor() { }

        public static Color PlatformTint
        {
            get
            {
                switch (Menu.Option("Platform Color"))
                {
                    case "Theme": return Theme.Accent;
                    case "White": return Color.white;
                    case "Rainbow": return Theme.Hue(0f);
                    default: return PlatformColor;
                }
            }
        }
    }
}
