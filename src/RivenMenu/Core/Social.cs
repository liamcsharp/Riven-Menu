using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Photon.Pun;
using Photon.Realtime;
using RivenMenu.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace RivenMenu
{
    public class Social : MonoBehaviour
    {
        public const string KnownPeopleUrl = "https://aceutils.org/data/knownpeople.txt";
        public const string RivenKey = "RivenMenu";

        public static Social Instance;

        public static bool KnownAlerts = true;
        public static bool RivenEsp;
        public static bool RivenAlerts = true;

        public static readonly Dictionary<string, string> Known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static readonly HashSet<string> RivenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static bool ListLoaded { get; private set; }
        public static string ListStatus { get; private set; } = "Loading list...";

        private static readonly Regex Entry = new Regex("\"([0-9A-Za-z]{6,24})\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.Compiled);

        private readonly HashSet<string> alerted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> rivenSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string room;
        private float nextScan;
        private float nextListRefresh;
        private bool broadcasting;
        private bool loading;

        private static string CachePath => Path.Combine(BepInEx.Paths.ConfigPath, "RivenMenu", "knownpeople.txt");

        private void Awake()
        {
            Instance = this;
            LoadCache();
        }

        private void Start()
        {
            Reload();
            Webhook.Launched();
        }

        public static void Reload()
        {
            if (Instance == null || Instance.loading) return;
            Instance.StartCoroutine(Instance.Download());
        }

        private void LoadCache()
        {
            try
            {
                if (File.Exists(CachePath) && Parse(File.ReadAllText(CachePath)) > 0)
                    ListStatus = Known.Count + " known people (saved copy)";
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Riven] known people cache: " + e.Message);
            }
        }

        private static int Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            Dictionary<string, string> found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Entry.Matches(text))
            {
                string id = m.Groups[1].Value.Trim();
                string name = m.Groups[2].Value.Trim();
                if (id.Length > 0 && name.Length > 0) found[id] = name;
            }
            if (found.Count == 0) return 0;
            Known.Clear();
            foreach (KeyValuePair<string, string> kv in found) Known[kv.Key] = kv.Value;
            ListLoaded = true;
            return found.Count;
        }

        private IEnumerator Download()
        {
            loading = true;
            nextListRefresh = Time.unscaledTime + 3600f;
            using (UnityWebRequest req = UnityWebRequest.Get(KnownPeopleUrl))
            {
                req.timeout = 15;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && Parse(req.downloadHandler.text) > 0)
                {
                    ListStatus = Known.Count + " known people loaded";
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
                        File.WriteAllText(CachePath, req.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[Riven] could not save known people: " + e.Message);
                    }
                    alerted.Clear();
                }
                else
                {
                    ListStatus = ListLoaded ? Known.Count + " known people (saved copy)" : "Could not load the known people list";
                    Debug.LogWarning("[Riven] known people download failed: " + req.error);
                }
            }
            loading = false;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextListRefresh && !loading) Reload();
            UpdateBroadcast();
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            Scan();
        }

        private string broadcastRoom;

        private void UpdateBroadcast()
        {
            if (PhotonNetwork.LocalPlayer == null) return;
            if (RivenEsp)
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;
                string name = PhotonNetwork.CurrentRoom.Name;
                if (broadcasting && broadcastRoom == name) return;
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { [RivenKey] = Menu.Version });
                broadcasting = true;
                broadcastRoom = name;
            }
            else if (broadcasting)
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { [RivenKey] = null });
                broadcasting = false;
                broadcastRoom = null;
            }
        }

        private void Scan()
        {
            NetworkSystem net = NetworkSystem.Instance;
            bool inRoom = net != null && net.InRoom;
            string now = inRoom ? net.RoomName : null;

            if (now != room)
            {
                if (room != null) Webhook.RoomLeft(room);
                room = now;
                alerted.Clear();
                rivenSeen.Clear();
                RivenUsers.Clear();
                if (room != null) Webhook.RoomJoined(room, net.RoomPlayerCount, net.GameModeString);
            }
            if (!inRoom) return;

            if (KnownAlerts && ListLoaded)
            {
                foreach (NetPlayer p in net.AllNetPlayers)
                {
                    if (p == null || p.IsLocal || string.IsNullOrEmpty(p.UserId)) continue;
                    if (!Known.TryGetValue(p.UserId, out string who) || !alerted.Add(p.UserId)) continue;
                    Notify.Push("You're in a lobby with " + who + "!");
                    if (RivenAudio.Instance != null) RivenAudio.Instance.PlayOn();
                    Webhook.KnownPerson(who, p.NickName, room);
                }
            }

            RivenUsers.Clear();
            if (PhotonNetwork.InRoom)
            {
                foreach (Player p in PhotonNetwork.PlayerListOthers)
                {
                    if (p == null || p.CustomProperties == null || !p.CustomProperties.ContainsKey(RivenKey) || p.CustomProperties[RivenKey] == null) continue;
                    if (string.IsNullOrEmpty(p.UserId)) continue;
                    RivenUsers.Add(p.UserId);
                    if (rivenSeen.Add(p.UserId) && RivenEsp && RivenAlerts)
                    {
                        Notify.Push(p.NickName + " is using Riven Menu");
                        Webhook.RivenUser(p.NickName, room);
                    }
                }
            }
        }

        public static void CheckLobby()
        {
            NetworkSystem net = NetworkSystem.Instance;
            if (net == null || !net.InRoom) { Notify.Push("Not in a room"); return; }
            if (!ListLoaded) { Notify.Push(ListStatus); return; }
            List<string> here = new List<string>();
            foreach (NetPlayer p in net.AllNetPlayers)
                if (p != null && !p.IsLocal && !string.IsNullOrEmpty(p.UserId) && Known.TryGetValue(p.UserId, out string who)) here.Add(who);
            Notify.Push(here.Count == 0 ? "No known people in this lobby" : "In this lobby: " + string.Join(", ", here));
        }

        public static bool IsRiven(VRRig rig)
        {
            if (rig == null || RivenUsers.Count == 0) return false;
            NetPlayer p = rig.Creator;
            return p != null && !string.IsNullOrEmpty(p.UserId) && RivenUsers.Contains(p.UserId);
        }

        public static string KnownName(VRRig rig)
        {
            if (rig == null) return null;
            NetPlayer p = rig.Creator;
            if (p == null || string.IsNullOrEmpty(p.UserId)) return null;
            return Known.TryGetValue(p.UserId, out string who) ? who : null;
        }

        private void OnDestroy()
        {
            if (broadcasting && PhotonNetwork.LocalPlayer != null)
                PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { [RivenKey] = null });
        }
    }
}
