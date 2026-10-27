using Photon.Pun;
using UnityEngine;

namespace RivenMenu.Mods
{
    public static class Media
    {
        private static RivenAudio Audio => RivenAudio.Instance;

        public static int Ping => PhotonNetwork.InRoom ? PhotonNetwork.GetPing() : 0;

        [MenuButton("Music Player", Category = "Media", Order = 0, OnEnable = nameof(PlayerOn), OnDisable = nameof(PlayerOff), Tooltip = "Shows a music player on your screen with the song, time and controls")]
        public static void MusicPlayer() { }
        public static void PlayerOn() => MusicHud.Shown = true;
        public static void PlayerOff() => MusicHud.Shown = false;

        [MenuButton("Play / Pause", ButtonType.Button, Category = "Media", Order = 1, Notify = false, Tooltip = "Starts or pauses the music")]
        public static void PlayPause() => Audio?.TogglePlay();

        [MenuButton("Next Song", ButtonType.Button, Category = "Media", Order = 2, Notify = false, Tooltip = "Skips to the next song")]
        public static void NextSong() => Audio?.Next();

        [MenuButton("Previous Song", ButtonType.Button, Category = "Media", Order = 3, Notify = false, Tooltip = "Goes back to the previous song")]
        public static void PreviousSong() => Audio?.Prev();

        [MenuButton("Skip Forward 10s", ButtonType.Button, Category = "Media", Order = 4, Notify = false, Tooltip = "Jumps ten seconds ahead")]
        public static void SkipForward()
        {
            if (Audio != null) Audio.Seek(Audio.Time + 10f);
        }

        [MenuButton("Skip Back 10s", ButtonType.Button, Category = "Media", Order = 5, Notify = false, Tooltip = "Jumps ten seconds back")]
        public static void SkipBack()
        {
            if (Audio != null) Audio.Seek(Audio.Time - 10f);
        }

        private static readonly float[] Volumes = { 0.6f, 0.8f, 1f, 0.1f, 0.2f, 0.4f };

        [MenuButton("Music Volume", ButtonType.Option, Category = "Media", Order = 6, Options = new[] { "60%", "80%", "100%", "10%", "20%", "40%" }, Notify = false, Tooltip = "How loud the music plays")]
        public static void MusicVolume() => RivenAudio.MusicVolume = Volumes[Menu.Index("Music Volume")];

        [MenuButton("Reload Song List", ButtonType.Button, Category = "Media", Order = 7, Tooltip = "Rescans the RivenMusic folder for new songs")]
        public static void ReloadSongs()
        {
            Audio?.ScanTracks();
            Notify.Push(Audio != null && Audio.HasMusic ? Audio.TrackCount + " songs found" : "No songs in RivenMusic");
        }

        [MenuButton("Open Music Folder", ButtonType.Button, Category = "Media", Order = 8, Notify = false, Tooltip = "Opens the folder where you put your songs")]
        public static void OpenMusicFolder()
        {
            System.IO.Directory.CreateDirectory(RivenAudio.MusicFolder);
            Application.OpenURL("file://" + RivenAudio.MusicFolder);
        }

        [MenuButton("FPS and Ping Wrist", Category = "Media", Order = 10, OnEnable = nameof(StatsOn), OnDisable = nameof(StatsOff), Tooltip = "Shows your FPS and ping on your right wrist and on screen")]
        public static void FpsAndPingWrist() { }
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
    }
}
