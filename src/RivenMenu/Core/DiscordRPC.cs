using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RivenMenu.Mods;
using UnityEngine;
using UnityEngine.XR;

namespace RivenMenu
{
    public sealed class DiscordRPC : MonoBehaviour
    {
        public const string GitHubUrl = "https://github.com/liamcsharp/Riven-Menu";
        public const string DiscordUrl = "https://discord.gg/RhUymsys9e";
        public const string LogoUrl = "LargeImage";

        public static string ApplicationId = "1555649885645308087";
        public static string LargeImage = LogoUrl;

        private const int ReconnectMs = 15000;
        private const float RefreshSeconds = 3f;

        public static DiscordRPC Instance { get; private set; }

        public static bool Enabled = true;
        public static bool ShowRoomCode = true;
        public static bool ShowModCount = true;
        public static bool ShowModeAndMap = true;
        public static bool ShowPlayers = true;
        public static bool ShowTime = true;
        public static bool ShowSong = true;
        public static bool ShowTagStatus = true;
        public static bool ShowPlatform = true;
        public static bool ShowButtons = true;

        public static bool Connected => Instance != null && Instance.connected;
        public static bool WantsSong => Enabled && ShowSong && Connected;

        private Thread worker;
        private volatile bool running;
        private volatile bool connected;
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private readonly object gate = new object();
        private readonly object pipeGate = new object();

        private string wanted;
        private string sent;
        private bool dirty = true;
        private long startedAt;
        private long roomSince;
        private string lastRoom;
        private int pid;
        private float nextRefresh;
        private NamedPipeClientStream pipe;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            startedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            pid = Process.GetCurrentProcess().Id;
            running = true;
            worker = new Thread(Loop) { IsBackground = true, Name = "Riven-DiscordRPC" };
            worker.Start();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            Refresh();
        }

        public static void RefreshNow()
        {
            if (Instance == null) return;
            Instance.nextRefresh = 0f;
        }

        private void Refresh()
        {
            string json;
            try
            {
                json = Enabled ? Build().ToString(Formatting.None) : null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Riven] Discord RPC could not build status: " + ex.Message);
                return;
            }

            lock (gate)
            {
                if (json == wanted && !dirty) return;
                wanted = json;
                dirty = true;
            }
            wake.Set();
        }

        private static string Mode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string u = raw.ToUpperInvariant();
            if (u.Contains("SUPERINFECT")) return "Super Infection";
            if (u.Contains("SUPERCASUAL")) return "Super Casual";
            if (u.Contains("INFECTION")) return "Infection";
            if (u.Contains("CASUAL")) return "Casual";
            if (u.Contains("PROPHUNT")) return "Prop Hunt";
            if (u.Contains("HUNT")) return "Hunt";
            if (u.Contains("PAINTBRAWL") || u.Contains("BATTLE")) return "Paintbrawl";
            if (u.Contains("FREEZE")) return "Freeze Tag";
            if (u.Contains("GUARDIAN")) return "Guardian";
            if (u.Contains("AMBUSH")) return "Ambush";
            if (u.Contains("GHOST")) return "Ghost";
            return null;
        }

        private static readonly string[] Zones = { "forest", "canyon", "cave", "city", "basement", "mountain", "beach", "clouds", "rotating", "metropolis", "arcade", "bayou", "critters", "ghostreactor", "monkeblocks", "hoverboard", "mall" };

        private static string Zone(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            string l = raw.ToLowerInvariant();
            foreach (string z in Zones)
                if (l.StartsWith(z, StringComparison.Ordinal))
                    return char.ToUpperInvariant(z[0]) + z.Substring(1);
            return null;
        }

        private static int ModCount()
        {
            int n = 0;
            for (int i = 0; i < Buttons.All.Count; i++)
            {
                ButtonInfo b = Buttons.All[i];
                if (b.IsActive && !Buttons.IsSetting(b)) n++;
            }
            return n;
        }

        private JObject Build()
        {
            bool inRoom = Important.InRoom;
            string room = inRoom ? Important.RoomCode : null;
            if (room != lastRoom)
            {
                lastRoom = room;
                roomSince = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            string raw = inRoom && NetworkSystem.Instance != null ? NetworkSystem.Instance.GameModeString : null;
            string mode = Mode(raw);
            string zone = Zone(raw);

            string details;
            if (!inRoom) details = "In the lobby";
            else if (ShowModeAndMap && mode != null && zone != null) details = mode + " in " + zone;
            else if (ShowModeAndMap && mode != null) details = "Playing " + mode;
            else details = "In a room";

            if (inRoom && ShowRoomCode && !string.IsNullOrEmpty(room)) details += "  |  Code: " + room;

            string state = "";
            if (inRoom && ShowTagStatus && mode != null && (mode.Contains("Infection") || mode == "Casual" || mode == "Hunt" || mode == "Freeze Tag"))
            {
                bool it = Util.LocalRig != null && Util.IsTagged(Util.LocalRig);
                if (mode != "Casual") state = it ? "Tagged" : "Running";
            }
            if (ShowModCount)
            {
                int mods = ModCount();
                string m = mods == 0 ? "No mods on" : mods == 1 ? "1 mod on" : mods + " mods on";
                state = state.Length > 0 ? state + "  |  " + m : m;
            }
            if (ShowSong && SystemMedia.Instance != null && SystemMedia.Instance.Found && SystemMedia.Instance.Playing && !string.IsNullOrEmpty(SystemMedia.Instance.Track))
            {
                string song = "Listening to " + SystemMedia.Instance.Track;
                state = state.Length > 0 ? state + "  |  " + song : song;
            }
            if (state.Length == 0) state = "Using " + Menu.PluginName;

            string hover = Menu.PluginName + " v" + Menu.Version;
            if (ShowPlatform) hover += "  |  " + (XRSettings.isDeviceActive ? "VR" : "PC");

            JObject assets = new JObject
            {
                ["large_image"] = LargeImage,
                ["large_text"] = Clip(hover)
            };

            JObject activity = new JObject
            {
                ["type"] = 0,
                ["details"] = Clip(details),
                ["state"] = Clip(state),
                ["assets"] = assets
            };

            if (ShowTime) activity["timestamps"] = new JObject { ["start"] = inRoom ? roomSince : startedAt };

            if (ShowButtons)
            {
                activity["buttons"] = new JArray
                {
                    new JObject { ["label"] = "Get Riven Menu", ["url"] = GitHubUrl },
                    new JObject { ["label"] = "Join Discord", ["url"] = DiscordUrl }
                };
            }

            if (inRoom && ShowPlayers)
            {
                activity["party"] = new JObject
                {
                    ["id"] = "riven-" + pid.ToString("x") + "-" + roomSince.ToString("x"),
                    ["size"] = new JArray { Mathf.Clamp(Important.RoomPlayers, 1, 10), 10 }
                };
            }

            return activity;
        }

        private static string Clip(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Riven Menu";
            if (s.Length < 2) s += " ";
            return s.Length <= 120 ? s : s.Substring(0, 120);
        }

        private void Loop()
        {
            DateTime nextTry = DateTime.MinValue;
            while (running)
            {
                try
                {
                    if (!connected)
                    {
                        if (DateTime.UtcNow < nextTry)
                        {
                            wake.WaitOne(1000);
                            continue;
                        }
                        if (!Connect())
                        {
                            nextTry = DateTime.UtcNow.AddMilliseconds(ReconnectMs);
                            continue;
                        }
                        lock (gate)
                        {
                            sent = null;
                            dirty = true;
                        }
                    }

                    string json = null;
                    bool send;
                    lock (gate)
                    {
                        send = dirty;
                        if (send)
                        {
                            json = wanted;
                            dirty = false;
                        }
                    }

                    if (send && json != sent)
                    {
                        SetActivity(json);
                        sent = json;
                    }

                    wake.WaitOne(1000);
                }
                catch (Exception ex)
                {
                    if (running) UnityEngine.Debug.LogWarning("[Riven] Discord RPC disconnected: " + ex.Message);
                    Close();
                    lock (gate) dirty = true;
                    nextTry = DateTime.UtcNow.AddMilliseconds(ReconnectMs);
                }
            }
        }

        private bool Connect()
        {
            for (int i = 0; i < 10 && running; i++)
            {
                NamedPipeClientStream p = null;
                try
                {
                    p = new NamedPipeClientStream(".", "discord-ipc-" + i, PipeDirection.InOut, PipeOptions.None);
                    p.Connect(250);
                    lock (pipeGate) pipe = p;
                    Write(0, new JObject { ["v"] = 1, ["client_id"] = ApplicationId }.ToString(Formatting.None));
                    JObject ready = Read();
                    if (ready == null || (string)ready["evt"] != "READY") throw new IOException("no READY from Discord");
                    connected = true;
                    UnityEngine.Debug.Log("[Riven] Discord RPC connected on discord-ipc-" + i);
                    return true;
                }
                catch
                {
                    lock (pipeGate)
                    {
                        if (pipe == p) pipe = null;
                    }
                    try { p?.Dispose(); }
                    catch { }
                }
            }
            return false;
        }

        private void SetActivity(string json)
        {
            JObject payload = new JObject
            {
                ["cmd"] = "SET_ACTIVITY",
                ["args"] = new JObject
                {
                    ["pid"] = pid,
                    ["activity"] = json == null ? JValue.CreateNull() : JToken.Parse(json)
                },
                ["nonce"] = Guid.NewGuid().ToString()
            };
            Write(1, payload.ToString(Formatting.None));
            JObject reply = Read();
            if (reply != null && (string)reply["evt"] == "ERROR")
                UnityEngine.Debug.LogWarning("[Riven] Discord rejected the status: " + reply["data"]?["message"]);
        }

        private JObject Read()
        {
            while (true)
            {
                byte[] head = ReadBytes(8);
                int op = BitConverter.ToInt32(head, 0);
                int len = BitConverter.ToInt32(head, 4);
                if (len < 0 || len > 1 << 20) throw new IOException("bad frame " + len);
                string body = len > 0 ? Encoding.UTF8.GetString(ReadBytes(len)) : "";
                switch (op)
                {
                    case 1: return string.IsNullOrEmpty(body) ? null : JObject.Parse(body);
                    case 2: throw new IOException("Discord closed the pipe: " + body);
                    case 3: Write(4, body); break;
                }
            }
        }

        private void Write(int op, string json)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] frame = new byte[8 + body.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(op), 0, frame, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(body.Length), 0, frame, 4, 4);
            Buffer.BlockCopy(body, 0, frame, 8, body.Length);
            lock (pipeGate)
            {
                if (pipe == null || !pipe.IsConnected) throw new IOException("pipe not connected");
                pipe.Write(frame, 0, frame.Length);
                pipe.Flush();
            }
        }

        private byte[] ReadBytes(int count)
        {
            byte[] buffer = new byte[count];
            int got = 0;
            while (got < count)
            {
                NamedPipeClientStream p;
                lock (pipeGate) p = pipe;
                if (p == null) throw new IOException("pipe closed");
                int n = p.Read(buffer, got, count - got);
                if (n <= 0) throw new IOException("pipe closed while reading");
                got += n;
            }
            return buffer;
        }

        private void Close()
        {
            connected = false;
            lock (pipeGate)
            {
                try { pipe?.Dispose(); }
                catch { }
                pipe = null;
            }
        }

        private void OnDestroy() => Shutdown();
        private void OnApplicationQuit() => Shutdown();

        private void Shutdown()
        {
            if (!running) return;
            running = false;
            try
            {
                if (connected) SetActivity(null);
            }
            catch { }
            Close();
            wake.Set();
            if (Instance == this) Instance = null;
        }
    }
}
