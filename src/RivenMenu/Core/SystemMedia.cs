using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace RivenMenu
{
    public class SystemMedia : MonoBehaviour
    {
        public static SystemMedia Instance;
        private static float wantUntil;

        public static bool Wanted => MusicHud.Shown || WristStats.NowPlaying || DiscordRPC.WantsSong || Time.unscaledTime < wantUntil;

        public static void Request(float seconds)
        {
            wantUntil = Mathf.Max(wantUntil, Time.unscaledTime + seconds);
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte VK_NEXT = 0xB0, VK_PREV = 0xB1, VK_PLAY = 0xB3;
        private const byte VK_MUTE = 0xAD, VK_VOL_DOWN = 0xAE, VK_VOL_UP = 0xAF;
        private const uint EXT = 0x0001, KEYUP = 0x0002;

        private static readonly string[] Players = { "Spotify", "foobar2000", "AIMP", "MusicBee", "Deezer", "TIDAL" };
        private static readonly string[] IdleTitles = { "spotify", "spotify premium", "spotify free", "advertisement", "foobar2000", "musicbee", "aimp", "tidal", "deezer" };

        public string Artist { get; private set; } = "";
        public string Track { get; private set; } = "";
        public string App { get; private set; } = "";
        public bool Playing { get; private set; }
        public bool Found { get; private set; }
        public float Elapsed { get; private set; }

        private readonly object gate = new object();
        private string polledTitle;
        private string polledApp;
        private bool polledFresh;
        private volatile bool running = true;
        private volatile bool wantedFlag;
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private Thread worker;

        private string lastTitle = "";
        private float trackStart;
        private int preferIndex;

        private void Awake()
        {
            Instance = this;
            worker = new Thread(Loop) { IsBackground = true, Name = "Riven-Media" };
            worker.Start();
        }

        private void OnDestroy()
        {
            running = false;
            wake.Set();
        }

        private void OnApplicationQuit()
        {
            running = false;
            wake.Set();
        }

        private void Loop()
        {
            while (running)
            {
                if (!wantedFlag)
                {
                    wake.WaitOne(1000);
                    continue;
                }

                string title = null, app = null;
                try { Scan(out title, out app); }
                catch { }

                lock (gate)
                {
                    polledTitle = title;
                    polledApp = app;
                    polledFresh = true;
                }
                wake.WaitOne(1500);
            }
        }

        private void Scan(out string title, out string app)
        {
            title = null;
            app = null;
            for (int k = 0; k < Players.Length; k++)
            {
                int i = (preferIndex + k) % Players.Length;
                Process[] procs;
                try { procs = Process.GetProcessesByName(Players[i].Replace(" ", "")); }
                catch { continue; }

                for (int p = 0; p < procs.Length; p++)
                {
                    string t = null;
                    try { t = procs[p].MainWindowTitle; }
                    catch { }
                    finally
                    {
                        try { procs[p].Dispose(); }
                        catch { }
                    }
                    if (title == null && !string.IsNullOrEmpty(t))
                    {
                        title = t;
                        app = Players[i];
                    }
                }
                if (title != null)
                {
                    preferIndex = i;
                    return;
                }
            }
        }

        private void Update()
        {
            wantedFlag = Wanted;
            if (!Wanted) return;

            string title = null, app = null;
            bool fresh = false;
            lock (gate)
            {
                if (polledFresh)
                {
                    title = polledTitle;
                    app = polledApp;
                    fresh = true;
                    polledFresh = false;
                }
            }
            if (fresh) Apply(title, app);
            if (Playing) Elapsed = Time.unscaledTime - trackStart;
        }

        private void Apply(string title, string app)
        {
            if (title == null)
            {
                Found = false;
                Playing = false;
                Artist = "";
                Track = "";
                App = "";
                return;
            }

            Found = true;
            App = app;
            string lower = title.Trim().ToLowerInvariant();
            bool idle = Array.IndexOf(IdleTitles, lower) >= 0 || !title.Contains(" - ");
            if (idle)
            {
                Playing = false;
                return;
            }

            Playing = true;
            int split = title.IndexOf(" - ", StringComparison.Ordinal);
            Artist = split > 0 ? title.Substring(0, split).Trim() : app;
            Track = split > 0 ? title.Substring(split + 3).Trim() : title.Trim();

            if (title != lastTitle)
            {
                lastTitle = title;
                trackStart = Time.unscaledTime;
                Elapsed = 0f;
            }
        }

        private static void Tap(byte vk)
        {
            try
            {
                keybd_event(vk, 0, EXT, UIntPtr.Zero);
                keybd_event(vk, 0, EXT | KEYUP, UIntPtr.Zero);
            }
            catch { }
        }

        private void Poke()
        {
            Request(4f);
            wake.Set();
        }

        public void PlayPause() { Tap(VK_PLAY); Poke(); }
        public void Next() { Tap(VK_NEXT); Poke(); }
        public void Prev() { Tap(VK_PREV); Poke(); }
        public void VolumeUp() => Tap(VK_VOL_UP);
        public void VolumeDown() => Tap(VK_VOL_DOWN);
        public void Mute() => Tap(VK_MUTE);

        public string Status => !Found ? "Open Spotify to control it" : Playing ? Track : "Paused";

        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
