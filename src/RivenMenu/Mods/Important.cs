using System;
using GorillaNetworking;
using UnityEngine;

namespace RivenMenu.Mods
{
    public static class Important
    {
        private static NetworkSystem Net => NetworkSystem.Instance;
        public static bool InRoom => Net != null && Net.InRoom;
        public static string RoomCode => InRoom ? Net.RoomName : "";
        public static int RoomPlayers => InRoom ? Net.RoomPlayerCount : 0;
        public static string LastRoom => lastRoom;
        private static string lastRoom = "";

        [MenuButton("Disconnect", ButtonType.Button, Category = "Important", Order = 0, Notify = false, Tooltip = "Leaves the current room, also pinned to the top of the menu")]
        public static void Disconnect()
        {
            if (!InRoom) { Notify.Push("Not in a room"); return; }
            lastRoom = Net.RoomName;
            _ = Net.ReturnToSinglePlayer();
            Notify.Push("Left " + lastRoom);
        }

        [MenuButton("Rejoin Last Room", ButtonType.Button, Category = "Important", Order = 1, Notify = false, Tooltip = "Joins the last room you left with Disconnect")]
        public static void RejoinLastRoom()
        {
            if (string.IsNullOrEmpty(lastRoom)) { Notify.Push("No room to rejoin"); return; }
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(lastRoom, JoinType.Solo);
            Notify.Push("Rejoining " + lastRoom);
        }

        [MenuButton("Copy Room Code", ButtonType.Button, Category = "Important", Order = 2, Notify = false, Tooltip = "Copies the room code to your clipboard")]
        public static void CopyRoomCode()
        {
            if (!InRoom) { Notify.Push("Not in a room"); return; }
            GUIUtility.systemCopyBuffer = Net.RoomName;
            Notify.Push("Copied " + Net.RoomName);
        }

        [MenuButton("Room Info", ButtonType.Button, Category = "Important", Order = 3, Notify = false, Tooltip = "Shows room code, players and mode")]
        public static void RoomInfo()
        {
            if (!InRoom) { Notify.Push("Not in a room"); return; }
            Notify.Push($"{Net.RoomName}  -  {Net.RoomPlayerCount}/10  -  {Net.GameModeString}");
        }

        private static string RoomLine() => InRoom ? $"Room {Net.RoomName}  {Net.RoomPlayerCount}/10  {Net.CurrentRegion}" : "Not in a room";

        [MenuButton("Room Info HUD", Category = "Important", Order = 4, OnEnable = nameof(RoomHudOn), OnDisable = nameof(RoomHudOff), Tooltip = "Keeps room info on your screen")]
        public static void RoomInfoHud() { }
        public static void RoomHudOn() => Hud.Show("room", RoomLine);
        public static void RoomHudOff() => Hud.Hide("room");

        [MenuButton("FPS Counter", Category = "Important", Order = 5, OnEnable = nameof(FpsOn), OnDisable = nameof(FpsOff), Tooltip = "Shows your frame rate")]
        public static void FpsCounter() { }
        public static void FpsOn() => Hud.Show("fps", () => $"{Mathf.RoundToInt(Hud.Fps)} FPS");
        public static void FpsOff() => Hud.Hide("fps");

        [MenuButton("Clock", Category = "Important", Order = 6, OnEnable = nameof(ClockOn), OnDisable = nameof(ClockOff), Tooltip = "Shows the time")]
        public static void Clock() { }
        public static void ClockOn() => Hud.Show("clock", () => DateTime.Now.ToString("h:mm:ss tt"));
        public static void ClockOff() => Hud.Hide("clock");

        [MenuButton("Coordinates", Category = "Important", Order = 7, OnEnable = nameof(CoordsOn), OnDisable = nameof(CoordsOff), Tooltip = "Shows your position")]
        public static void Coordinates() { }
        public static void CoordsOn() => Hud.Show("coords", () =>
        {
            Vector3 p = Util.BodyT != null ? Util.BodyT.position : Vector3.zero;
            return $"X {p.x:0.0}  Y {p.y:0.0}  Z {p.z:0.0}";
        });
        public static void CoordsOff() => Hud.Hide("coords");

        [MenuButton("Speedometer", Category = "Important", Order = 8, OnEnable = nameof(SpeedOn), OnDisable = nameof(SpeedOff), Tooltip = "Shows how fast you are going")]
        public static void Speedometer() { }
        public static void SpeedOn() => Hud.Show("speed", () => $"{(Util.Body != null ? Util.Body.velocity.magnitude : 0f):0.0} m/s");
        public static void SpeedOff() => Hud.Hide("speed");

        [MenuButton("Save Preferences", ButtonType.Button, Category = "Important", Order = 10, Tooltip = "Saves which mods and options are on")]
        public static void SavePreferences() => Buttons.SavePrefs();

        [MenuButton("Load Preferences", ButtonType.Button, Category = "Important", Order = 11, Tooltip = "Loads your saved mods and options")]
        public static void LoadPreferences() => Buttons.LoadPrefs();

        [MenuButton("Clear Preferences", ButtonType.Button, Category = "Important", Order = 12, Tooltip = "Deletes your saved preferences")]
        public static void ClearPreferences() => Buttons.ClearPrefs();

        [MenuButton("Auto Load Preferences", Category = "Important", Order = 13, OnEnable = nameof(AutoLoadOn), OnDisable = nameof(AutoLoadOff), Tooltip = "Loads your preferences every time the game starts")]
        public static void AutoLoadPreferences() { }
        public static void AutoLoadOn() { PlayerPrefs.SetInt("RivenMenu.AutoLoad", 1); PlayerPrefs.Save(); }
        public static void AutoLoadOff() { PlayerPrefs.SetInt("RivenMenu.AutoLoad", 0); PlayerPrefs.Save(); }

        private static readonly int[] FpsCaps = { -1, 144, 120, 90, 72, 60 };

        [MenuButton("FPS Cap", ButtonType.Option, Category = "Important", Order = 20, Options = new[] { "Default", "144", "120", "90", "72", "60" }, Tooltip = "Limits frame rate on PC")]
        public static void FpsCap()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = FpsCaps[Menu.Index("FPS Cap")];
        }

        private static int savedQuality = -1;

        [MenuButton("Low Graphics", Category = "Important", Order = 21, OnEnable = nameof(LowOn), OnDisable = nameof(LowOff), Tooltip = "Lowest quality settings for more FPS")]
        public static void LowGraphics() { }
        public static void LowOn()
        {
            savedQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);
        }
        public static void LowOff()
        {
            if (savedQuality >= 0) QualitySettings.SetQualityLevel(savedQuality, true);
        }

        [MenuButton("Clear Notifications", ButtonType.Button, Category = "Important", Order = 30, Notify = false, Tooltip = "Removes every popup")]
        public static void ClearNotifications() => Notify.Clear();

        [MenuButton("Disable All Mods", ButtonType.Button, Category = "Important", Order = 31, Tooltip = "Turns every mod off")]
        public static void DisableAllMods() => Buttons.DisableAll();

        [MenuButton("Quit Game", ButtonType.Button, Category = "Important", Order = 40, Tooltip = "Closes Gorilla Tag")]
        public static void QuitGame() => Application.Quit();
    }
}
