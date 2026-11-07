using UnityEngine;

namespace RivenMenu.Mods
{
    public static class DiscordStatus
    {
        private const string Cat = "Discord RPC";

        private static void Set(ref bool field, bool value)
        {
            field = value;
            DiscordRPC.RefreshNow();
        }

        [MenuButton("Discord RPC", Category = Cat, Order = 0, Default = true, Notify = false, OnEnable = nameof(RpcOn), OnDisable = nameof(RpcOff), Tooltip = "Shows Riven Menu as your Discord status")]
        public static void Rpc() { }
        public static void RpcOn() => Set(ref DiscordRPC.Enabled, true);
        public static void RpcOff() => Set(ref DiscordRPC.Enabled, false);

        [MenuButton("Show Room Code", Category = Cat, Order = 1, Default = true, Notify = false, OnEnable = nameof(CodeOn), OnDisable = nameof(CodeOff), Tooltip = "Puts your room code on your status so friends can join you")]
        public static void RoomCode() { }
        public static void CodeOn() => Set(ref DiscordRPC.ShowRoomCode, true);
        public static void CodeOff() => Set(ref DiscordRPC.ShowRoomCode, false);

        [MenuButton("Show Mod Count", Category = Cat, Order = 2, Default = true, Notify = false, OnEnable = nameof(ModsOn), OnDisable = nameof(ModsOff), Tooltip = "Shows how many mods you have on")]
        public static void ModCount() { }
        public static void ModsOn() => Set(ref DiscordRPC.ShowModCount, true);
        public static void ModsOff() => Set(ref DiscordRPC.ShowModCount, false);

        [MenuButton("Show Mode and Map", Category = Cat, Order = 3, Default = true, Notify = false, OnEnable = nameof(ModeOn), OnDisable = nameof(ModeOff), Tooltip = "Shows the game mode and map, like Infection in Forest")]
        public static void ModeAndMap() { }
        public static void ModeOn() => Set(ref DiscordRPC.ShowModeAndMap, true);
        public static void ModeOff() => Set(ref DiscordRPC.ShowModeAndMap, false);

        [MenuButton("Show Player Count", Category = Cat, Order = 4, Default = true, Notify = false, OnEnable = nameof(PlayersOn), OnDisable = nameof(PlayersOff), Tooltip = "Shows how many players are in your room, like 7 of 10")]
        public static void PlayerCount() { }
        public static void PlayersOn() => Set(ref DiscordRPC.ShowPlayers, true);
        public static void PlayersOff() => Set(ref DiscordRPC.ShowPlayers, false);

        [MenuButton("Show Tagged Status", Category = Cat, Order = 5, Default = true, Notify = false, OnEnable = nameof(TagOn), OnDisable = nameof(TagOff), Tooltip = "Shows if you are tagged or running in tag modes")]
        public static void TagStatus() { }
        public static void TagOn() => Set(ref DiscordRPC.ShowTagStatus, true);
        public static void TagOff() => Set(ref DiscordRPC.ShowTagStatus, false);

        [MenuButton("Show Current Song", Category = Cat, Order = 6, Default = true, Notify = false, OnEnable = nameof(SongOn), OnDisable = nameof(SongOff), Tooltip = "Shows the Spotify song you are listening to")]
        public static void CurrentSong() { }
        public static void SongOn() => Set(ref DiscordRPC.ShowSong, true);
        public static void SongOff() => Set(ref DiscordRPC.ShowSong, false);

        [MenuButton("Show Time Played", Category = Cat, Order = 7, Default = true, Notify = false, OnEnable = nameof(TimeOn), OnDisable = nameof(TimeOff), Tooltip = "Shows how long you have been in the room or playing")]
        public static void TimePlayed() { }
        public static void TimeOn() => Set(ref DiscordRPC.ShowTime, true);
        public static void TimeOff() => Set(ref DiscordRPC.ShowTime, false);

        [MenuButton("Show VR or PC", Category = Cat, Order = 8, Default = true, Notify = false, OnEnable = nameof(PlatformOn), OnDisable = nameof(PlatformOff), Tooltip = "Shows if you are playing in VR or on PC when people hover the logo")]
        public static void Platform() { }
        public static void PlatformOn() => Set(ref DiscordRPC.ShowPlatform, true);
        public static void PlatformOff() => Set(ref DiscordRPC.ShowPlatform, false);

        [MenuButton("Show Status Buttons", Category = Cat, Order = 9, Default = true, Notify = false, OnEnable = nameof(ButtonsOn), OnDisable = nameof(ButtonsOff), Tooltip = "Adds Get Riven Menu and Join Discord buttons to your status")]
        public static void StatusButtons() { }
        public static void ButtonsOn() => Set(ref DiscordRPC.ShowButtons, true);
        public static void ButtonsOff() => Set(ref DiscordRPC.ShowButtons, false);

        [MenuButton("Get Riven Menu", ButtonType.Button, Category = Cat, Order = 10, Notify = false, Tooltip = "Opens the Riven Menu GitHub")]
        public static void GetRiven() => Application.OpenURL(DiscordRPC.GitHubUrl);

        [MenuButton("Join Discord Server", ButtonType.Button, Category = Cat, Order = 11, Notify = false, Tooltip = "Opens the Riven Menu Discord server")]
        public static void JoinServer() => Application.OpenURL(DiscordRPC.DiscordUrl);

        [MenuButton("Refresh Status", ButtonType.Button, Category = Cat, Order = 12, Notify = false, Tooltip = "Updates your Discord status right now")]
        public static void Refresh()
        {
            DiscordRPC.RefreshNow();
            Notify.Push(DiscordRPC.Connected ? "Discord status updated" : "Discord not found, open Discord and try again");
        }
    }
}
