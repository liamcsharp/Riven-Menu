using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RivenMenu
{
    public enum ButtonType
    {
        Toggle,
        Button,
        Option
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MenuButtonAttribute : Attribute
    {
        public string Name { get; }
        public ButtonType Type { get; }

        public string Category { get; set; } = "Main";
        public int Order { get; set; }
        public string[] Options { get; set; }
        public string OptionsFrom { get; set; }
        public int DefaultOption { get; set; }
        public bool Notify { get; set; } = true;
        public string Tooltip { get; set; } = "";
        public string OnEnable { get; set; }
        public string OnDisable { get; set; }
        public bool Default { get; set; }

        public MenuButtonAttribute(string name, ButtonType type = ButtonType.Toggle)
        {
            Name = name;
            Type = type;
        }
    }

    public class ButtonInfo
    {
        public string Name;
        public ButtonType Type;
        public string Category;
        public int Order;
        public string[] Options;
        public bool Notify;
        public string Tooltip;

        public bool Enabled;
        public int OptionIndex;

        public Action Method;
        public Action EnableMethod;
        public Action DisableMethod;

        public bool IsActive => Type == ButtonType.Toggle && Enabled;
        public string Current => Options != null && Options.Length > 0 ? Options[OptionIndex] : "";
    }

    public static class Buttons
    {
        public static readonly List<ButtonInfo> All = new List<ButtonInfo>();
        private static readonly Dictionary<string, ButtonInfo> byName =
            new Dictionary<string, ButtonInfo>(StringComparer.OrdinalIgnoreCase);

        public static event Action<ButtonInfo> Pressed;

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const string PrefKey = "RivenMenu.";

        public static void Load()
        {
            All.Clear();
            byName.Clear();

            Type[] types;
            try { types = typeof(Buttons).Assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

            foreach (Type type in types)
            {
                foreach (MethodInfo m in type.GetMethods(Flags))
                {
                    MenuButtonAttribute attr = m.GetCustomAttribute<MenuButtonAttribute>();
                    if (attr == null) continue;

                    if (m.GetParameters().Length != 0 || m.ReturnType != typeof(void))
                    {
                        Debug.LogWarning($"[Riven] {type.Name}.{m.Name} must be static void with no parameters");
                        continue;
                    }
                    if (byName.ContainsKey(attr.Name))
                    {
                        Debug.LogWarning($"[Riven] Duplicate button name {attr.Name}");
                        continue;
                    }

                    ButtonInfo info = new ButtonInfo
                    {
                        Name = attr.Name,
                        Type = attr.Type,
                        Category = string.IsNullOrEmpty(attr.Category) ? "Main" : attr.Category,
                        Order = attr.Order,
                        Options = attr.Options ?? FindOptions(type, attr.OptionsFrom),
                        Notify = attr.Notify,
                        Tooltip = attr.Tooltip ?? "",
                        Enabled = attr.Type == ButtonType.Toggle && attr.Default,
                        Method = (Action)Delegate.CreateDelegate(typeof(Action), m),
                        EnableMethod = Find(type, attr.OnEnable),
                        DisableMethod = Find(type, attr.OnDisable),
                    };

                    if (info.Type == ButtonType.Option)
                    {
                        if (info.Options == null || info.Options.Length == 0) info.Options = new[] { "-" };
                        info.OptionIndex = Mathf.Clamp(attr.DefaultOption, 0, info.Options.Length - 1);
                    }

                    All.Add(info);
                    byName[info.Name] = info;
                }
            }

            All.Sort((a, c) => a.Order != c.Order
                ? a.Order.CompareTo(c.Order)
                : string.Compare(a.Name, c.Name, StringComparison.OrdinalIgnoreCase));

            foreach (string cat in All.Select(x => x.Category).Distinct())
                if (!Menu.Tabs.Any(t => string.Equals(t.Name, cat, StringComparison.OrdinalIgnoreCase)))
                    Menu.Tabs.Add(new Tab(cat));

            foreach (ButtonInfo info in All)
            {
                if (info.IsActive) SafeRun(info.EnableMethod, info.Name);
                if (info.Type == ButtonType.Option && info.OptionIndex != 0) SafeRun(info.Method, info.Name);
            }
        }

        private static Action Find(Type type, string methodName)
        {
            if (string.IsNullOrEmpty(methodName)) return null;
            MethodInfo m = type.GetMethod(methodName, Flags, null, Type.EmptyTypes, null);
            if (m == null || m.ReturnType != typeof(void))
            {
                Debug.LogWarning($"[Riven] Couldn't find static void {type.Name}.{methodName}()");
                return null;
            }
            return (Action)Delegate.CreateDelegate(typeof(Action), m);
        }

        private static string[] FindOptions(Type type, string memberName)
        {
            if (string.IsNullOrEmpty(memberName)) return null;
            FieldInfo f = type.GetField(memberName, Flags);
            if (f != null && f.FieldType == typeof(string[])) return (string[])f.GetValue(null);
            PropertyInfo p = type.GetProperty(memberName, Flags);
            if (p != null && p.PropertyType == typeof(string[])) return (string[])p.GetValue(null);
            Debug.LogWarning($"[Riven] Couldn't find static string[] {type.Name}.{memberName}");
            return null;
        }

        public static ButtonInfo Get(string name) =>
            name != null && byName.TryGetValue(name, out ButtonInfo found) ? found : null;

        public static IEnumerable<ButtonInfo> InCategory(string category) =>
            All.Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase));

        public static bool AnyActive => All.Any(x => x.IsActive && !IsSetting(x));

        private static bool IsSetting(ButtonInfo info) =>
            string.Equals(info.Category, "Settings", StringComparison.OrdinalIgnoreCase);

        public static void Press(ButtonInfo info, bool reverse = false)
        {
            switch (info.Type)
            {
                case ButtonType.Toggle:
                    SetEnabled(info, !info.Enabled);
                    if (info.Notify) RivenMenu.Notify.Push($"{info.Name}  {(info.Enabled ? "ON" : "OFF")}");
                    break;

                case ButtonType.Button:
                    SafeRun(info.Method, info.Name);
                    if (info.Notify) RivenMenu.Notify.Push(info.Name);
                    break;

                case ButtonType.Option:
                    int n = info.Options.Length;
                    info.OptionIndex = ((info.OptionIndex + (reverse ? -1 : 1)) % n + n) % n;
                    SafeRun(info.Method, info.Name);
                    if (info.Notify) RivenMenu.Notify.Push($"{info.Name}: {info.Current}");
                    break;
            }

            Pressed?.Invoke(info);
        }

        public static void SetEnabled(ButtonInfo info, bool on)
        {
            if (info == null || info.Type != ButtonType.Toggle || info.Enabled == on) return;
            info.Enabled = on;
            SafeRun(on ? info.EnableMethod : info.DisableMethod, info.Name);
        }

        public static void SetEnabled(string name, bool on) => SetEnabled(Get(name), on);

        public static void RunActive()
        {
            for (int i = 0; i < All.Count; i++)
            {
                ButtonInfo info = All[i];
                if (!info.IsActive || info.Method == null) continue;

                try { info.Method(); }
                catch (Exception e)
                {
                    Debug.LogError($"[Riven] {info.Name} crashed and was turned off: {e}");
                    info.Enabled = false;
                    SafeRun(info.DisableMethod, info.Name);
                    RivenMenu.Notify.Push($"{info.Name} crashed - turned off");
                }
            }
        }

        public static void DisableAll()
        {
            foreach (ButtonInfo info in All.Where(x => x.IsActive && !IsSetting(x)).ToList())
                SetEnabled(info, false);
        }

        public static void SavePrefs()
        {
            foreach (ButtonInfo info in All)
            {
                if (info.Type == ButtonType.Toggle) PlayerPrefs.SetInt(PrefKey + info.Name, info.Enabled ? 1 : 0);
                else if (info.Type == ButtonType.Option) PlayerPrefs.SetInt(PrefKey + info.Name, info.OptionIndex);
            }
            PlayerPrefs.Save();
        }

        public static void LoadPrefs()
        {
            foreach (ButtonInfo info in All)
            {
                string key = PrefKey + info.Name;
                if (!PlayerPrefs.HasKey(key)) continue;
                int v = PlayerPrefs.GetInt(key);

                if (info.Type == ButtonType.Toggle) SetEnabled(info, v == 1);
                else if (info.Type == ButtonType.Option && info.OptionIndex != v)
                {
                    info.OptionIndex = Mathf.Clamp(v, 0, info.Options.Length - 1);
                    SafeRun(info.Method, info.Name);
                }
            }
        }

        public static void ClearPrefs()
        {
            foreach (ButtonInfo info in All) PlayerPrefs.DeleteKey(PrefKey + info.Name);
            PlayerPrefs.Save();
        }

        public static void SafeRun(Action action, string name)
        {
            if (action == null) return;
            try { action(); }
            catch (Exception e) { Debug.LogError($"[Riven] {name} failed: {e}"); }
        }
    }
}
