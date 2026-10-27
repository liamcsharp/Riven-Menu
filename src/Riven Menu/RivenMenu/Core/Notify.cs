using System.Collections.Generic;
using UnityEngine;

namespace RivenMenu
{
    public static class Notify
    {
        public class Toast
        {
            public string Text;
            public float Time;
        }

        public const float Life = 3f;
        public static readonly List<Toast> Toasts = new List<Toast>();

        public static void Push(string text)
        {
            ButtonInfo setting = Buttons.Get("Notifications");
            if (setting != null && !setting.Enabled) return;

            Toasts.Add(new Toast { Text = text, Time = UnityEngine.Time.unscaledTime });
            if (Toasts.Count > 6) Toasts.RemoveAt(0);
        }

        public static void Clear() => Toasts.Clear();
    }
}
