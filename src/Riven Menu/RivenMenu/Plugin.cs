using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivenMenu
{
    [BepInPlugin(GUID, Menu.PluginName, Menu.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.riven.gorillatag.menu";

        private void Start()
        {
            Buttons.Load();

            GameObject audio = new GameObject("RivenAudio");
            audio.AddComponent<RivenAudio>();
            DontDestroyOnLoad(audio);

            GameObject menu = new GameObject("RivenMenu");
            menu.AddComponent<MenuGUI>();
            DontDestroyOnLoad(menu);

            GameObject wrist = new GameObject("RivenWrist");
            wrist.AddComponent<WristMenu>();
            DontDestroyOnLoad(wrist);

            GameObject stats = new GameObject("RivenWristStats");
            stats.AddComponent<WristStats>();
            DontDestroyOnLoad(stats);

            if (PlayerPrefs.GetInt("RivenMenu.AutoLoad", 0) == 1) Buttons.LoadPrefs();

            Splash.Show();

            Logger.LogInfo($"{Menu.PluginName} {Menu.Version} loaded with {Buttons.All.Count} buttons");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard?.mKey.wasPressedThisFrame == true && !MenuGUI.IsTyping)
                MenuGUI.Toggle();

            if (keyboard?.escapeKey.wasPressedThisFrame == true && !MenuGUI.IsTyping)
                Mods.Desktop.EscapeAll();

            Buttons.RunActive();
        }

        private void OnGUI()
        {
            Hud.Draw();
            MusicHud.Draw();
        }
    }
}
