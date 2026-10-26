using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivenMenu
{
    [BepInPlugin(GUID, Menu.PluginName, Menu.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.riven.gorillatag.menu";

        private GameObject menuInstance;
        private bool menuOpen = true;

        private void Start()
        {
            Buttons.Load();
            if (PlayerPrefs.GetInt("RivenMenu.AutoLoad", 0) == 1) Buttons.LoadPrefs();

            menuInstance = new GameObject("RivenMenu");
            menuInstance.AddComponent<MenuGUI>();
            DontDestroyOnLoad(menuInstance);
            menuInstance.SetActive(menuOpen);

            GameObject rpc = new GameObject("RivenDiscordRPC");
            rpc.AddComponent<DiscordRPC>();
            DontDestroyOnLoad(rpc);

            Logger.LogInfo($"{Menu.PluginName} {Menu.Version} loaded with {Buttons.All.Count} buttons");
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard?.mKey.wasPressedThisFrame == true && menuInstance != null && !MenuGUI.IsTyping)
            {
                menuOpen = !menuOpen;
                menuInstance.SetActive(menuOpen);
            }

            if (keyboard?.escapeKey.wasPressedThisFrame == true && !MenuGUI.IsTyping)
                Mods.Desktop.EscapeAll();

            Buttons.RunActive();
        }

        private void OnGUI() => Hud.Draw();
    }
}
