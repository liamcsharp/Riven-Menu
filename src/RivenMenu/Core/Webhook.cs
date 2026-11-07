using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RivenMenu
{
    public static class Webhook
    {
        public static bool Enabled = true;
        public static bool OnJoin = true;
        public static bool OnLeave;
        public static bool OnKnown = true;
        public static bool OnRiven = true;
        public static bool OnLaunch;

        private static string url;
        private static bool loaded;
        private static readonly Queue<JObject> queue = new Queue<JObject>();
        private static bool pumping;

        public static string Folder => Path.Combine(BepInEx.Paths.ConfigPath, "RivenMenu");
        private static string FilePath => Path.Combine(Folder, "webhook.txt");

        public static bool HasUrl
        {
            get
            {
                Load();
                return !string.IsNullOrEmpty(url);
            }
        }

        public static bool IsValid(string u)
        {
            if (string.IsNullOrEmpty(u)) return false;
            u = u.Trim();
            return u.StartsWith("https://discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase)
                || u.StartsWith("https://discordapp.com/api/webhooks/", StringComparison.OrdinalIgnoreCase)
                || u.StartsWith("https://canary.discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase)
                || u.StartsWith("https://ptb.discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase);
        }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                    if (IsValid(line)) { url = line.Trim(); return; }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Riven] webhook file: " + e.Message);
            }
        }

        public static bool SetUrl(string u)
        {
            if (!IsValid(u)) return false;
            url = u.Trim();
            loaded = true;
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(FilePath, url);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Riven] could not save webhook: " + e.Message);
            }
            return true;
        }

        public static void Forget()
        {
            url = null;
            loaded = true;
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Riven] could not delete webhook: " + e.Message);
            }
        }

        public static void Send(string title, string description, Color color, bool force = false)
        {
            if (!force && !Enabled) return;
            if (!HasUrl) return;
            JObject embed = new JObject
            {
                ["title"] = Clip(title, 250),
                ["description"] = Clip(description, 4000),
                ["color"] = (Mathf.RoundToInt(color.r * 255f) << 16) | (Mathf.RoundToInt(color.g * 255f) << 8) | Mathf.RoundToInt(color.b * 255f),
                ["footer"] = new JObject { ["text"] = Menu.PluginName + " v" + Menu.Version },
                ["timestamp"] = DateTime.UtcNow.ToString("o")
            };
            JObject payload = new JObject
            {
                ["username"] = Menu.PluginName,
                ["embeds"] = new JArray { embed }
            };
            if (queue.Count > 20) return;
            queue.Enqueue(payload);
            if (!pumping && Social.Instance != null) Social.Instance.StartCoroutine(Pump());
        }

        private static IEnumerator Pump()
        {
            pumping = true;
            while (queue.Count > 0)
            {
                JObject payload = queue.Dequeue();
                byte[] body = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));
                using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 15;
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success)
                        Debug.LogWarning("[Riven] webhook failed: " + req.responseCode + " " + req.error);
                }
                yield return new WaitForSecondsRealtime(1.5f);
            }
            pumping = false;
        }

        private static string Clip(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            return s.Length <= max ? s : s.Substring(0, max);
        }

        public static void RoomJoined(string room, int players, string mode)
        {
            if (OnJoin) Send("Joined a room", "Room **" + room + "**\nPlayers: " + players + "/10\nMode: " + (string.IsNullOrEmpty(mode) ? "?" : mode), Theme.Accent);
        }

        public static void RoomLeft(string room)
        {
            if (OnLeave) Send("Left a room", "Left room **" + room + "**", new Color(0.55f, 0.55f, 0.6f));
        }

        public static void KnownPerson(string who, string nick, string room)
        {
            if (OnKnown) Send("Known person in your lobby", "**" + who + "** (" + nick + ") is in room **" + room + "**", new Color(1f, 0.78f, 0.2f));
        }

        public static void RivenUser(string nick, string room)
        {
            if (OnRiven) Send("Riven user in your lobby", "**" + nick + "** is using Riven Menu in room **" + room + "**", Theme.Accent2);
        }

        public static void Launched()
        {
            if (OnLaunch) Send("Riven Menu started", "Gorilla Tag launched with Riven Menu v" + Menu.Version, Theme.Accent);
        }
    }
}
