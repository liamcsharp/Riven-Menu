using Photon.Pun;
using UnityEngine;

namespace RivenMenu.Mods
{
    public static class Media
    {
        private static SystemMedia M => SystemMedia.Instance;

        public static int Ping => PhotonNetwork.InRoom ? PhotonNetwork.GetPing() : 0;

        [MenuButton("Now Playing", Category = "Media", Order = 0, OnEnable = nameof(HudOn), OnDisable = nameof(HudOff), Tooltip = "Shows a Spotify player on your screen with the song, artist and controls")]
        public static void NowPlaying() { }
        public static void HudOn() => MusicHud.Shown = true;
        public static void HudOff() => MusicHud.Shown = false;

        [MenuButton("Play / Pause", ButtonType.Button, Category = "Media", Order = 1, Notify = false, Tooltip = "Plays or pauses your music")]
        public static void PlayPause() => M?.PlayPause();

        [MenuButton("Next Track", ButtonType.Button, Category = "Media", Order = 2, Notify = false, Tooltip = "Skips to the next song")]
        public static void NextTrack() => M?.Next();

        [MenuButton("Previous Track", ButtonType.Button, Category = "Media", Order = 3, Notify = false, Tooltip = "Goes back to the previous song")]
        public static void PreviousTrack() => M?.Prev();

        [MenuButton("Volume Up", ButtonType.Button, Category = "Media", Order = 4, Notify = false, Tooltip = "Turns your system volume up")]
        public static void VolumeUp() => M?.VolumeUp();

        [MenuButton("Volume Down", ButtonType.Button, Category = "Media", Order = 5, Notify = false, Tooltip = "Turns your system volume down")]
        public static void VolumeDown() => M?.VolumeDown();

        [MenuButton("Mute", ButtonType.Button, Category = "Media", Order = 6, Notify = false, Tooltip = "Mutes or unmutes your system volume")]
        public static void MuteToggle() => M?.Mute();

        [MenuButton("Song on Notification", ButtonType.Button, Category = "Media", Order = 7, Notify = false, Tooltip = "Pops up what is playing right now")]
        public static void SongNotification()
        {
            if (M == null) return;
            SystemMedia.Request(5f);
            if (!M.Found) { Notify.Push("Checking for Spotify... press again in a sec"); return; }
            Notify.Push(M.Playing ? M.Track + " - " + M.Artist : M.App + " is paused");
        }

        public static void WristOn() => WristStats.NowPlaying = true;
        public static void WristOff() => WristStats.NowPlaying = false;

        [MenuButton("Now Playing on Wrist", Category = "Media", Order = 8, OnEnable = nameof(WristOn), OnDisable = nameof(WristOff), Tooltip = "Shows the current song on your left wrist")]
        public static void NowPlayingWrist() { }

        public static void StatsOn()
        {
            WristStats.Shown = true;
            Hud.Show("fpsping", () => $"{Mathf.RoundToInt(Hud.Fps)} FPS   {Ping} ms");
        }
        public static void StatsOff()
        {
            WristStats.Shown = false;
            Hud.Hide("fpsping");
        }

        [MenuButton("FPS and Ping Wrist", Category = "Media", Order = 10, OnEnable = nameof(StatsOn), OnDisable = nameof(StatsOff), Tooltip = "Shows your FPS and ping on your right wrist and on screen")]
        public static void FpsAndPingWrist() { }
    }
}
