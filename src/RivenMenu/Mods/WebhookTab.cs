using UnityEngine;

namespace RivenMenu.Mods
{
    public static class WebhookTab
    {
        private const string Cat = "Webhook";

        [MenuButton("Webhook", Category = Cat, Order = 0, Default = true, Notify = false, OnEnable = nameof(On), OnDisable = nameof(Off), Tooltip = "Sends your events to your own Discord webhook")]
        public static void Master() { }
        public static void On() => Webhook.Enabled = true;
        public static void Off() => Webhook.Enabled = false;

        [MenuButton("Paste Webhook URL", ButtonType.Button, Category = Cat, Order = 1, Notify = false, Tooltip = "Copy your Discord webhook URL, then press this to save it")]
        public static void Paste()
        {
            string clip = GUIUtility.systemCopyBuffer;
            if (Webhook.SetUrl(clip)) Notify.Push("Webhook saved");
            else Notify.Push("Copy a Discord webhook URL first");
        }

        [MenuButton("Send Test Message", ButtonType.Button, Category = Cat, Order = 2, Notify = false, Tooltip = "Sends a test message to your webhook")]
        public static void Test()
        {
            if (!Webhook.HasUrl) { Notify.Push("No webhook saved yet"); return; }
            Webhook.Send("Webhook connected", "Riven Menu can post to this channel now", Theme.Accent, true);
            Notify.Push("Test sent");
        }

        [MenuButton("Webhook Status", ButtonType.Button, Category = Cat, Order = 3, Notify = false, Tooltip = "Shows if a webhook is saved")]
        public static void Status() => Notify.Push(Webhook.HasUrl ? (Webhook.Enabled ? "Webhook saved and on" : "Webhook saved but turned off") : "No webhook saved");

        [MenuButton("Open Webhook Folder", ButtonType.Button, Category = Cat, Order = 4, Notify = false, Tooltip = "Opens the folder with webhook.txt so you can paste the URL in by hand")]
        public static void OpenFolder()
        {
            System.IO.Directory.CreateDirectory(Webhook.Folder);
            Application.OpenURL("file:///" + Webhook.Folder.Replace('\\', '/'));
        }

        [MenuButton("Forget Webhook", ButtonType.Button, Category = Cat, Order = 5, Notify = false, Tooltip = "Deletes your saved webhook")]
        public static void Forget()
        {
            Webhook.Forget();
            Notify.Push("Webhook removed");
        }

        [MenuButton("Post Room Joins", Category = Cat, Order = 6, Default = true, Notify = false, OnEnable = nameof(JoinOn), OnDisable = nameof(JoinOff), Tooltip = "Posts the room code, players and mode when you join a room")]
        public static void Joins() { }
        public static void JoinOn() => Webhook.OnJoin = true;
        public static void JoinOff() => Webhook.OnJoin = false;

        [MenuButton("Post Room Leaves", Category = Cat, Order = 7, Notify = false, OnEnable = nameof(LeaveOn), OnDisable = nameof(LeaveOff), Tooltip = "Posts when you leave a room")]
        public static void Leaves() { }
        public static void LeaveOn() => Webhook.OnLeave = true;
        public static void LeaveOff() => Webhook.OnLeave = false;

        [MenuButton("Post Known People", Category = Cat, Order = 8, Default = true, Notify = false, OnEnable = nameof(KnownOn), OnDisable = nameof(KnownOff), Tooltip = "Posts when a known person is in your lobby")]
        public static void Known() { }
        public static void KnownOn() => Webhook.OnKnown = true;
        public static void KnownOff() => Webhook.OnKnown = false;

        [MenuButton("Post Riven Users", Category = Cat, Order = 9, Default = true, Notify = false, OnEnable = nameof(RivenOn), OnDisable = nameof(RivenOff), Tooltip = "Posts when another Riven user is in your lobby")]
        public static void Riven() { }
        public static void RivenOn() => Webhook.OnRiven = true;
        public static void RivenOff() => Webhook.OnRiven = false;

        [MenuButton("Post Game Launch", Category = Cat, Order = 10, Notify = false, OnEnable = nameof(LaunchOn), OnDisable = nameof(LaunchOff), Tooltip = "Posts when you start the game with Riven")]
        public static void Launch() { }
        public static void LaunchOn() => Webhook.OnLaunch = true;
        public static void LaunchOff() => Webhook.OnLaunch = false;

        [MenuButton("Post Current Room", ButtonType.Button, Category = Cat, Order = 11, Notify = false, Tooltip = "Posts your current room to the webhook right now")]
        public static void PostRoom()
        {
            NetworkSystem net = NetworkSystem.Instance;
            if (net == null || !net.InRoom) { Notify.Push("Not in a room"); return; }
            if (!Webhook.HasUrl) { Notify.Push("No webhook saved yet"); return; }
            Webhook.Send("Current room", "Room **" + net.RoomName + "**\nPlayers: " + net.RoomPlayerCount + "/10\nMode: " + net.GameModeString, Theme.Accent, true);
            Notify.Push("Posted room " + net.RoomName);
        }
    }
}
