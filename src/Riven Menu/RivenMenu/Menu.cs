using System.Collections.Generic;
using System.Linq;

namespace RivenMenu
{
    public static class Menu
    {
        public const string PluginName = "Riven Menu";
        public const string Name = "riven.menu";
        public const string Version = "2.1.0";

        public static readonly List<Tab> Tabs = new List<Tab>
        {
            new Tab("Settings",     "Menu look, sounds and mod settings"),
            new Tab("Enabled Mods", "Everything you have turned on", () => Buttons.All.Where(x => x.IsActive)),
            new Tab("Important",    "Room, game and preference tools"),
            new Tab("Movement",     "Fly, platforms, speed and more"),
            new Tab("Advantages",   "Tag tools, presets and movement tech"),
            new Tab("Guns",         "Teleport, grapple and player guns"),
            new Tab("Visuals",      "ESP, world and camera visuals"),
            new Tab("Desktop",      "Mouse and keyboard mods for PC players"),
            new Tab("Troll",        "Ghost, invisible, orbit, copy and more"),
            new Tab("Fun",          "Silly stuff for you and your rig"),
        };

        public static bool IsOn(string button)     => Buttons.Get(button)?.Enabled ?? false;
        public static int Index(string button)     => Buttons.Get(button)?.OptionIndex ?? 0;
        public static string Option(string button) => Buttons.Get(button)?.Current ?? "";
    }
}
