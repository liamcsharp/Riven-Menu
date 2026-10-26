using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivenMenu
{
    public class MenuGUI : MonoBehaviour
    {
        public static MenuGUI Instance;
        public static float AnimSpeed = 1f;
        public static float Scale = 1f;
        public static bool ParticlesOn = true;
        public static bool GlowOn = true;
        public static bool SoundsOn = true;

        private const float W = 600f, H = 460f, Rail = 170f, Pad = 14f;
        private const float HeaderH = 74f, FooterH = 34f, TileH = 52f, TileGap = 6f;
        private const float TabH = 28f, TabGap = 3f, TabsTop = 84f, RailBottom = 58f;
        private const string SearchControl = "riven_search";

        private class AnimValue
        {
            public float Value;
            public int Frame;
        }

        private class Ripple
        {
            public Vector2 Pos;
            public float Time;
        }

        private Vector2 pos = new Vector2(40f, 40f);
        private Vector2 origin;
        private float drawScale = 1f;

        private int tab;
        private string query = "";
        private bool searchFocused;
        private bool wantFocus;
        private bool clearFocus;

        private float scroll, scrollTarget, railScroll, railTarget;
        private float indicatorY = -1f;
        private float switchTime;
        private float openT;
        private float ga = 1f;
        private float dt;
        private int lastFrame = -1;

        private bool dragging;
        private Vector2 dragGrab;
        private bool barDragging;
        private float barGrab;

        private string tip = "";
        private int[] tabCounts = new int[0];
        private int activeCount;
        private List<ButtonInfo> items = new List<ButtonInfo>();

        private readonly Dictionary<string, AnimValue> anims = new Dictionary<string, AnimValue>();
        private readonly Dictionary<string, float> pops = new Dictionary<string, float>();
        private readonly Dictionary<string, Ripple> ripples = new Dictionary<string, Ripple>();

        private Font font;
        private GUIStyle sBrand, sBrandSmall, sTitle, sSub, sTab, sBadge, sRow, sDesc, sChip, sSmall, sSmallR, sCenter, sToast, sSearch;

        private AudioSource audioSource;
        private AudioClip clickClip, onClip, offClip;

        public static bool IsTyping => Instance != null && Instance.isActiveAndEnabled && Instance.searchFocused;

        public static bool MouseOver
        {
            get
            {
                if (Instance == null || !Instance.isActiveAndEnabled || Mouse.current == null) return false;
                Vector2 m = Mouse.current.position.ReadValue();
                m.y = Screen.height - m.y;
                float s = Instance.drawScale;
                return new Rect(Instance.origin.x, Instance.origin.y, W * s, H * s).Contains(m);
            }
        }

        public static void ResetPosition()
        {
            if (Instance == null) return;
            Instance.pos = new Vector2(40f, 40f);
        }

        private void Awake()
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.22f;
            clickClip = Tone("riven_click", 1300f, 1300f, 0.035f);
            onClip = Tone("riven_on", 850f, 1500f, 0.07f);
            offClip = Tone("riven_off", 1300f, 650f, 0.07f);
            Buttons.Pressed += OnPressed;
        }

        private void OnDestroy() => Buttons.Pressed -= OnPressed;

        private void OnEnable()
        {
            clearFocus = true;
            openT = 0f;
            switchTime = Time.unscaledTime;
            dragging = false;
            barDragging = false;
        }

        private void OnDisable()
        {
            searchFocused = false;
            dragging = false;
            barDragging = false;
        }

        private void OnPressed(ButtonInfo info)
        {
            if (info.Type == ButtonType.Toggle) Pop("o" + info.Name);
            if (!SoundsOn || audioSource == null) return;
            AudioClip clip = info.Type == ButtonType.Toggle ? (info.Enabled ? onClip : offClip) : clickClip;
            audioSource.PlayOneShot(clip);
        }

        private void Click()
        {
            if (SoundsOn && audioSource != null) audioSource.PlayOneShot(clickClip);
        }

        private static AudioClip Tone(string name, float f0, float f1, float length)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * length);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += Mathf.Lerp(f0, f1, t) / rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 8f)) * (1f - t);
                data[i] = Mathf.Sin(phase * Mathf.PI * 2f) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private float Speed => AnimSpeed <= 0f ? 1000f : AnimSpeed;

        private void Tick()
        {
            dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            openT = Mathf.Min(1f, openT + dt * 3.4f * Speed);

            foreach (string key in pops.Keys.ToList())
                pops[key] = Mathf.Max(0f, pops[key] - dt * 3.5f * Speed);

            foreach (string key in ripples.Keys.ToList())
                if (Time.unscaledTime - ripples[key].Time > 1.5f) ripples.Remove(key);

            Notify.Toasts.RemoveAll(t => Time.unscaledTime - t.Time > Notify.Life);

            if (Mouse.current != null && !Mouse.current.leftButton.isPressed)
            {
                dragging = false;
                barDragging = false;
            }

            int count = Menu.Tabs.Count;
            if (tabCounts.Length != count) tabCounts = new int[count];
            for (int i = 0; i < count; i++)
            {
                int on = 0;
                foreach (ButtonInfo b in Menu.Tabs[i].Items)
                    if (b.IsActive) on++;
                tabCounts[i] = on;
            }
            activeCount = Buttons.All.Count(b => b.IsActive && !string.Equals(b.Category, "Settings", StringComparison.OrdinalIgnoreCase));

            tab = Mathf.Clamp(tab, 0, Mathf.Max(0, count - 1));
            items = BuildItems();

            float k = 1f - Mathf.Exp(-16f * dt * Mathf.Min(Speed, 4f));
            scroll = AnimSpeed <= 0f ? scrollTarget : Mathf.Lerp(scroll, scrollTarget, k);
            railScroll = AnimSpeed <= 0f ? railTarget : Mathf.Lerp(railScroll, railTarget, k);
            float goal = TabsTop + tab * (TabH + TabGap);
            indicatorY = indicatorY < 0f || AnimSpeed <= 0f ? goal : Mathf.Lerp(indicatorY, goal, 1f - Mathf.Exp(-14f * dt * Mathf.Min(Speed, 4f)));
        }

        private List<ButtonInfo> BuildItems()
        {
            if (!string.IsNullOrEmpty(query))
            {
                string q = query.Trim();
                if (q.Length == 0) return Menu.Tabs.Count == 0 ? new List<ButtonInfo>() : Menu.Tabs[tab].Items;
                return Buttons.All.Where(b =>
                    b.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (b.Tooltip ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
            return Menu.Tabs.Count == 0 ? new List<ButtonInfo>() : Menu.Tabs[tab].Items;
        }

        private float Anim(string key, bool target, float speed = 12f)
        {
            float goal = target ? 1f : 0f;
            if (!anims.TryGetValue(key, out AnimValue a))
            {
                anims[key] = new AnimValue { Value = goal, Frame = Time.frameCount };
                return goal;
            }
            if (a.Frame != Time.frameCount)
            {
                a.Frame = Time.frameCount;
                a.Value = AnimSpeed <= 0f ? goal : Mathf.Lerp(a.Value, goal, 1f - Mathf.Exp(-speed * AnimSpeed * dt));
            }
            return a.Value;
        }

        private void Pop(string key) => pops[key] = 1f;
        private float PopOf(string key) => pops.TryGetValue(key, out float v) ? v : 0f;

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void OnGUI()
        {
            Color oldColor = GUI.color, oldBg = GUI.backgroundColor, oldContent = GUI.contentColor;
            Matrix4x4 oldMatrix = GUI.matrix;
            bool oldEnabled = GUI.enabled;
            Color oldCursor = GUI.skin.settings.cursorColor;

            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUI.matrix = Matrix4x4.identity;
            GUI.enabled = true;
            GUI.depth = -1000;

            try
            {
                if (lastFrame != Time.frameCount)
                {
                    lastFrame = Time.frameCount;
                    Tick();
                }
                EnsureStyles();
                GUI.skin.settings.cursorColor = Theme.Lighten(Theme.Accent, 0.4f);

                float baseScale = Mathf.Clamp(Scale, 0.5f, 2f);
                pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, Screen.width - W * baseScale));
                pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, Screen.height - H * baseScale));

                ga = EaseOutCubic(openT);
                float s = baseScale * Mathf.LerpUnclamped(0.93f, 1f, EaseOutBack(openT));
                drawScale = s;
                Vector2 center = pos + new Vector2(W, H) * (baseScale / 2f);
                origin = center - new Vector2(W, H) * (s / 2f);
                GUI.matrix = Matrix4x4.TRS(new Vector3(origin.x, origin.y, 0f), Quaternion.identity, new Vector3(s, s, 1f));

                DrawMenu();

                GUI.matrix = Matrix4x4.identity;
                DrawToasts();
            }
            finally
            {
                GUI.color = oldColor;
                GUI.backgroundColor = oldBg;
                GUI.contentColor = oldContent;
                GUI.matrix = oldMatrix;
                GUI.enabled = oldEnabled;
                GUI.skin.settings.cursorColor = oldCursor;
            }
        }

        private MenuTheme T => Theme.Current;
        private Color Acc => Theme.Accent;
        private Color Acc2 => Theme.Accent2;
        private Color TextC => T.Text;
        private Color Dim => Theme.WithA(Color.Lerp(T.Text, Acc, 0.3f), 0.62f);
        private Color Faint => Theme.WithA(Color.Lerp(T.Text, Acc, 0.3f), 0.38f);
        private Color RowC => Theme.WithA(T.Row, 0.62f);
        private Color RowHoverC => Theme.WithA(Color.Lerp(Theme.Lighten(T.Row, 0.08f), Acc, 0.2f), 0.85f);
        private Color RowOnC => Theme.WithA(Color.Lerp(T.Row, Acc, 0.42f), 0.9f);
        private Color EdgeC => Theme.WithA(Theme.Lighten(Acc, 0.3f), T.Edge);
        private float R => Mathf.Min(T.Radius, 22f);
        private float RR => Mathf.Min(T.RowRadius, 14f);
        private string Up(string s) => T.Upper ? s.ToUpperInvariant() : s;

        private void DrawMenu()
        {
            tip = "";
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            Rect panel = new Rect(0f, 0f, W, H);
            bool overMenu = panel.Contains(mouse);

            if (clearFocus)
            {
                GUIUtility.keyboardControl = 0;
                searchFocused = false;
                clearFocus = false;
            }

            if (e.type == EventType.MouseDown && searchFocused && !SearchRect.Contains(mouse))
            {
                GUIUtility.keyboardControl = 0;
                searchFocused = false;
            }

            HandleDrag(e, mouse);

            if (GlowOn)
            {
                float pulse = 0.18f + 0.1f * Mathf.Sin(Time.unscaledTime * 2.2f);
                Glow(panel, R, 22, Theme.WithA(Acc, pulse));
            }
            Fill(panel, R, Theme.WithA(T.Panel, T.Opacity));

            GUI.BeginGroup(panel);
            DrawBackdrop();
            DrawRail(mouse);
            DrawHeader(mouse);
            DrawList(mouse);
            DrawFooter();
            GUI.EndGroup();

            Edge(panel, R, EdgeC, 1.5f);

            if (overMenu && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel))
                e.Use();
        }

        private bool InDragZone(Vector2 m)
        {
            if (new Rect(0f, 0f, Rail, TabsTop - 8f).Contains(m)) return true;
            Rect head = new Rect(Rail, 0f, W - Rail, HeaderH - 8f);
            return head.Contains(m) && !SearchRect.Contains(m);
        }

        private void HandleDrag(Event e, Vector2 mouse)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && InDragZone(mouse))
            {
                dragging = true;
                dragGrab = mouse;
                e.Use();
                return;
            }
            if (dragging && e.type == EventType.MouseDrag)
            {
                Vector2 screen = origin + mouse * drawScale;
                float baseScale = Mathf.Clamp(Scale, 0.5f, 2f);
                pos = screen - dragGrab * baseScale;
                e.Use();
            }
            if (e.rawType == EventType.MouseUp) dragging = false;
        }

        private void DrawBackdrop()
        {
            if (Event.current.type != EventType.Repaint) return;

            float t = Time.unscaledTime;
            GUI.BeginGroup(new Rect(Rail, 0f, W - Rail, H));
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(-24f, new Vector2((W - Rail) * 0.5f, H * 0.5f));
            float sweep = Mathf.Repeat(t * 0.06f, 1.4f) - 0.2f;
            Tint(new Rect(sweep * (W - Rail) - 90f, -140f, 180f, H + 280f), Tex.Sheen, Theme.WithA(Acc2, 0.05f));
            GUI.matrix = saved;
            GUI.EndGroup();

            if (ParticlesOn && T.Particles)
            {
                for (int i = 0; i < 22; i++)
                {
                    float r1 = Frac(Mathf.Sin(i * 12.9898f) * 43758.5453f);
                    float r2 = Frac(Mathf.Sin(i * 78.233f) * 12345.678f);
                    float r3 = Frac(Mathf.Sin(i * 39.425f) * 24634.634f);
                    float speed = 5f + r2 * 13f;
                    float y = H + 10f - Mathf.Repeat(t * speed + r3 * (H + 20f), H + 20f);
                    float x = Rail + 10f + r1 * (W - Rail - 20f) + Mathf.Sin(t * 0.6f + i) * 8f;
                    float size = 4f + r3 * 6f;
                    float a = (0.05f + r2 * 0.12f) * (0.6f + 0.4f * Mathf.Sin(t * 2f + i));
                    Vector2 c = new Vector2(x, y);
                    Matrix4x4 m = GUI.matrix;
                    GUIUtility.RotateAroundPivot(t * (20f + r1 * 40f) + i * 30f, c);
                    Tint(new Rect(x - size / 2f, y - size / 2f, size, size), Tex.Diamond, Theme.WithA(Color.Lerp(Acc, Acc2, r1), a));
                    GUI.matrix = m;
                }
            }

            if (T.Scanlines)
            {
                GUI.color = new Color(1f, 1f, 1f, ga);
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, W, H), Tex.Scanline, new Rect(0f, t * 0.6f, W / 4f, H / 4f));
                GUI.color = Color.white;
            }
        }

        private void DrawMark(Vector2 c, float size, float gap, float starA)
        {
            Vector2 off = Tex.RiftAxis * gap;
            Rect r = new Rect(c.x - size / 2f, c.y - size / 2f, size, size);
            Tint(new Rect(r.x - off.x, r.y - off.y, size, size), Tex.RiftLeft, Acc);
            Tint(new Rect(r.x + off.x, r.y + off.y, size, size), Tex.RiftRight, Acc2);
            if (starA > 0.01f)
            {
                float ss = size * 0.34f;
                Tint(new Rect(c.x - ss / 2f, c.y - ss / 2f, ss, ss), Tex.Star, new Color(1f, 1f, 1f, starA));
            }
        }

        private void DrawRail(Vector2 mouse)
        {
            GUI.BeginGroup(new Rect(0f, 0f, Rail, H));
            Fill(new Rect(0f, 0f, Rail + R + 6f, H), R, Theme.WithA(Color.Lerp(T.Panel, Color.black, 0.35f), 0.55f * T.Opacity + 0.15f));
            GUI.EndGroup();

            float t = Time.unscaledTime;
            Fill(new Rect(Rail - 1f, 18f, 1.5f, H - 36f), 0.5f, Theme.WithA(Acc, 0.18f));
            GUI.BeginGroup(new Rect(Rail - 5f, 18f, 10f, H - 36f));
            float py = Mathf.Repeat(t * 90f, H + 40f) - 70f;
            Tint(new Rect(1f, py, 8f, 70f), Tex.SoftDot, Theme.WithA(Theme.Lighten(Acc, 0.3f), 0.55f));
            GUI.EndGroup();

            float breathe = 1.6f + Mathf.Sin(t * 2.4f) * 1.2f;
            Vector2 mc = new Vector2(34f, 40f);
            Tint(new Rect(mc.x - 26f, mc.y - 26f, 52f, 52f), Tex.SoftDot, Theme.WithA(Acc, 0.22f + 0.08f * Mathf.Sin(t * 2f)));
            DrawMark(mc, 34f, breathe, 0.85f + 0.15f * Mathf.Sin(t * 5f));

            string title = T.Title;
            GUIStyle brand = sBrand.CalcSize(new GUIContent(title)).x <= Rail - 66f ? sBrand : sBrandSmall;
            Label(new Rect(58f, 22f, Rail - 64f, 22f), WaveText(title), brand, TextC);
            Label(new Rect(58f, 44f, Rail - 64f, 16f), "v" + Menu.Version, sSmall, Faint);

            Rect area = new Rect(0f, TabsTop - 4f, Rail, H - TabsTop - RailBottom + 4f);
            float contentH = Menu.Tabs.Count * (TabH + TabGap);
            float maxRail = Mathf.Max(0f, contentH - area.height + 8f);
            Event e = Event.current;
            bool mouseInArea = area.Contains(mouse);
            if (e.type == EventType.ScrollWheel && mouseInArea)
            {
                railTarget = Mathf.Clamp(railTarget + e.delta.y * 18f, 0f, maxRail);
                e.Use();
            }
            railTarget = Mathf.Clamp(railTarget, 0f, maxRail);

            GUI.BeginGroup(area);
            float shift = area.y + railScroll;
            Vector2 local = new Vector2(mouse.x - area.x, mouse.y - area.y);
            bool searchMode = !string.IsNullOrEmpty(query);
            if (!searchMode && Menu.Tabs.Count > 0)
            {
                Rect ind = new Rect(10f, indicatorY - shift, Rail - 20f, TabH);
                Fill(ind, Mathf.Min(RR, 10f), RowOnC);
                Fill(new Rect(ind.x + 4f, ind.y + 8f, 3f, ind.height - 16f), 1.5f, Theme.Lighten(Acc, 0.35f));
            }

            for (int i = 0; i < Menu.Tabs.Count; i++)
            {
                Rect r = new Rect(10f, TabsTop + i * (TabH + TabGap) - shift, Rail - 20f, TabH);
                if (r.yMax < 0f || r.y > area.height) continue;
                bool hover = mouseInArea && r.Contains(local);
                bool active = i == tab && !searchMode;
                float h = Anim("tab" + i, hover && !active);
                if (h > 0.01f) Fill(r, Mathf.Min(RR, 10f), Theme.WithA(RowHoverC, RowHoverC.a * h * 0.7f));

                Color tc = active ? TextC : Color.Lerp(Dim, TextC, h);
                int on = i < tabCounts.Length ? tabCounts[i] : 0;
                Label(new Rect(r.x + 14f + h * 3f, r.y, r.width - (on > 0 ? 50f : 20f), r.height), Up(Menu.Tabs[i].Name), sTab, tc);

                if (on > 0)
                {
                    Rect b = new Rect(r.xMax - 30f, r.y + 7f, 24f, 16f);
                    Tint(b, Tex.Pill(48, 32), Theme.WithA(Acc, active ? 0.9f : 0.55f));
                    Label(b, on.ToString(), sBadge, Color.white);
                }

                if (hover) tip = string.IsNullOrEmpty(Menu.Tabs[i].Tooltip) ? Menu.Tabs[i].Name : Menu.Tabs[i].Tooltip;
                if (hover && e.type == EventType.MouseDown && e.button == 0)
                {
                    SelectTab(i);
                    e.Use();
                }
            }
            GUI.EndGroup();

            DrawRailButtons(mouse);
        }

        private void SelectTab(int i)
        {
            if (i == tab && string.IsNullOrEmpty(query)) return;
            tab = i;
            query = "";
            scroll = scrollTarget = 0f;
            switchTime = Time.unscaledTime;
            GUIUtility.keyboardControl = 0;
            searchFocused = false;
            items = BuildItems();
            Click();
        }

        private void DrawRailButtons(Vector2 mouse)
        {
            float y = H - RailBottom + 10f;
            Rect pwr = new Rect(14f, y, 38f, 38f);
            Rect door = new Rect(58f, y, 38f, 38f);
            Event e = Event.current;

            float hp = Anim("pwr", pwr.Contains(mouse));
            bool any = activeCount > 0;
            Fill(pwr, Mathf.Min(RR, 10f), Color.Lerp(RowC, RowHoverC, hp));
            Edge(pwr, Mathf.Min(RR, 10f), Theme.WithA(EdgeC, Mathf.Lerp(EdgeC.a * 0.6f, 0.9f, hp)));
            if (any) Tint(Centered(pwr, 34f), Tex.SoftDot, Theme.WithA(Acc, 0.22f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f)));
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(PopOf("pwr") * 180f, pwr.center);
            Tint(Centered(pwr, 20f), Tex.Power, any ? Theme.Lighten(Acc, 0.5f) : Theme.WithA(TextC, 0.4f));
            GUI.matrix = saved;
            if (pwr.Contains(mouse)) tip = "Turn every mod off";
            if (pwr.Contains(mouse) && e.type == EventType.MouseDown && e.button == 0)
            {
                Pop("pwr");
                Buttons.DisableAll();
                Notify.Push("All mods off");
                if (SoundsOn && audioSource != null) audioSource.PlayOneShot(offClip);
                e.Use();
            }

            bool inRoom = Mods.Important.InRoom;
            bool canRejoin = !inRoom && !string.IsNullOrEmpty(Mods.Important.LastRoom);
            bool doorOn = inRoom || canRejoin;
            float hd = Anim("door", doorOn && door.Contains(mouse));
            Fill(door, Mathf.Min(RR, 10f), Theme.WithA(Color.Lerp(RowC, RowHoverC, hd), RowC.a * (doorOn ? 1f : 0.5f)));
            Edge(door, Mathf.Min(RR, 10f), Theme.WithA(EdgeC, Mathf.Lerp(EdgeC.a * 0.6f, 0.9f, hd) * (doorOn ? 1f : 0.5f)));
            Color ic = inRoom ? Color.Lerp(Theme.WithA(TextC, 0.75f), new Color(1f, 0.4f, 0.45f), hd) : Theme.WithA(Theme.Lighten(Acc, 0.4f), doorOn ? 0.9f : 0.3f);
            Tint(Centered(door, 22f), inRoom ? Tex.Leave : Tex.Rejoin, ic);
            if (door.Contains(mouse)) tip = inRoom ? "Leave this room" : canRejoin ? "Rejoin " + Mods.Important.LastRoom : "Not in a room";
            if (doorOn && door.Contains(mouse) && e.type == EventType.MouseDown && e.button == 0)
            {
                if (inRoom) Mods.Important.Disconnect();
                else Mods.Important.RejoinLastRoom();
                Click();
                e.Use();
            }

            string on = activeCount == 0 ? Up("idle") : activeCount + Up(" on");
            Label(new Rect(102f, y, Rail - 110f, 38f), on, sSmallR, activeCount > 0 ? Acc : Faint);
        }

        private Rect SearchRect => new Rect(W - Pad - 178f, 20f, 178f, 32f);

        private void DrawHeader(Vector2 mouse)
        {
            bool searchMode = !string.IsNullOrEmpty(query);
            string title = searchMode ? "search" : Menu.Tabs.Count > 0 ? Menu.Tabs[tab].Name.ToLowerInvariant() : "riven";
            string sub = searchMode
                ? items.Count + (items.Count == 1 ? " result" : " results")
                : Menu.Tabs.Count > 0 && !string.IsNullOrEmpty(Menu.Tabs[tab].Tooltip) ? Menu.Tabs[tab].Tooltip : items.Count + " mods";

            float since = Time.unscaledTime - switchTime;
            float ti = EaseOutCubic(since * Speed / 0.3f);
            Label(new Rect(Rail + Pad + (1f - ti) * 10f, 16f, W - Rail - Pad * 2f - 186f, 30f), WaveText(title), sTitle, Theme.WithA(TextC, ti));
            Label(new Rect(Rail + Pad + 1f, 44f, W - Rail - Pad * 2f - 186f, 18f), sub, sSub, Theme.WithA(Dim, Dim.a * ti));

            Rect sr = SearchRect;
            float hf = Anim("searchF", searchFocused, 14f);
            float hh = Anim("searchH", sr.Contains(mouse));
            Fill(sr, Mathf.Min(RR, 16f), Theme.WithA(Color.Lerp(Color.black, T.Row, 0.25f), 0.45f + 0.1f * hh));
            Edge(sr, Mathf.Min(RR, 16f), Theme.WithA(Color.Lerp(EdgeC, Theme.Lighten(Acc, 0.4f), hf), Mathf.Lerp(EdgeC.a * 0.7f, 1f, Mathf.Max(hf, hh * 0.6f))));

            Rect field = new Rect(sr.x + 12f, sr.y, sr.width - 40f, sr.height);
            Event e = Event.current;
            if (searchFocused && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ClearSearch();
                e.Use();
            }

            GUI.SetNextControlName(SearchControl);
            GUI.color = new Color(1f, 1f, 1f, ga);
            string next = GUI.TextField(field, query ?? "", 32, sSearch);
            GUI.color = Color.white;
            if (e.type != EventType.Layout && e.type != EventType.Used)
                searchFocused = GUI.GetNameOfFocusedControl() == SearchControl;

            if (next != query)
            {
                query = next;
                scroll = scrollTarget = 0f;
                switchTime = Time.unscaledTime;
                items = BuildItems();
            }
            if (string.IsNullOrEmpty(query) && !searchFocused)
                Label(field, Up("search mods..."), sSub, Faint);

            Rect clear = new Rect(sr.xMax - 28f, sr.y + 4f, 24f, 24f);
            if (!string.IsNullOrEmpty(query))
            {
                float hc = Anim("clear", clear.Contains(mouse));
                Label(clear, "x", sChip, Color.Lerp(Dim, TextC, hc));
                if (clear.Contains(mouse) && e.type == EventType.MouseDown && e.button == 0)
                {
                    ClearSearch();
                    e.Use();
                }
            }
            else
            {
                Tint(Centered(clear, 12f), Tex.DiamondOutline, Theme.WithA(Acc, 0.6f));
            }

            if (wantFocus)
            {
                GUI.FocusControl(SearchControl);
                wantFocus = false;
            }
            if (!searchFocused && e.type == EventType.KeyDown && e.keyCode == KeyCode.Slash && MouseOver)
            {
                wantFocus = true;
                e.Use();
            }
        }

        private void ClearSearch()
        {
            query = "";
            scroll = scrollTarget = 0f;
            switchTime = Time.unscaledTime;
            GUIUtility.keyboardControl = 0;
            searchFocused = false;
            items = BuildItems();
        }

        private void DrawList(Vector2 mouse)
        {
            Rect area = new Rect(Rail + Pad, HeaderH, W - Rail - Pad * 2f, H - HeaderH - FooterH);
            float contentH = items.Count * (TileH + TileGap);
            float maxScroll = Mathf.Max(0f, contentH - area.height + 4f);
            Event e = Event.current;
            bool inArea = area.Contains(mouse);

            if (e.type == EventType.ScrollWheel && inArea)
            {
                scrollTarget = Mathf.Clamp(scrollTarget + e.delta.y * 22f, 0f, maxScroll);
                e.Use();
            }
            scrollTarget = Mathf.Clamp(scrollTarget, 0f, maxScroll);
            if (scroll > maxScroll) scroll = maxScroll;

            bool hasBar = maxScroll > 0.5f;
            float cw = area.width - (hasBar ? 12f : 0f);
            if (hasBar) DrawScrollbar(new Rect(area.xMax - 5f, area.y + 2f, 4f, area.height - 4f), area.height, contentH, maxScroll, mouse);

            if (items.Count == 0)
            {
                string msg = string.IsNullOrEmpty(query) ? "nothing here yet" : "no mods match " + query;
                Label(new Rect(area.x, area.y + 40f, area.width, 30f), Up(msg), sCenter, Faint);
                return;
            }

            GUI.BeginGroup(area);
            Vector2 local = new Vector2(mouse.x - area.x, mouse.y - area.y);
            float since = Time.unscaledTime - switchTime;
            bool searchMode = !string.IsNullOrEmpty(query);

            for (int i = 0; i < items.Count; i++)
            {
                float y = i * (TileH + TileGap) - scroll;
                if (y + TileH < -2f || y > area.height + 2f) continue;

                ButtonInfo btn = items[i];
                float ti = EaseOutCubic((since - Mathf.Min(i, 10) * 0.035f) * Speed / 0.3f);
                float gaSaved = ga;
                ga *= ti;

                Rect r = new Rect(0f, y + (1f - ti) * 14f, cw, TileH);
                bool hover = inArea && !barDragging && !dragging && r.Contains(local);
                DrawTile(btn, r, hover, local, searchMode);

                ga = gaSaved;
            }
            GUI.EndGroup();
        }

        private void DrawScrollbar(Rect bar, float viewH, float contentH, float maxScroll, Vector2 mouse)
        {
            float thumbH = Mathf.Max(28f, bar.height * viewH / Mathf.Max(1f, contentH));
            float thumbY = bar.y + (bar.height - thumbH) * Mathf.Clamp01(scroll / maxScroll);
            Rect thumb = new Rect(bar.x, thumbY, bar.width, thumbH);
            Rect grab = new Rect(bar.x - 6f, bar.y, bar.width + 10f, bar.height);
            Event e = Event.current;

            float h = Anim("bar", barDragging || grab.Contains(mouse));
            Fill(bar, 2f, Theme.WithA(TextC, 0.06f));
            Fill(new Rect(thumb.x - h, thumb.y, thumb.width + h * 2f, thumb.height), 3f, Theme.WithA(Acc, Mathf.Lerp(0.45f, 0.9f, h)));

            if (e.type == EventType.MouseDown && e.button == 0 && grab.Contains(mouse))
            {
                barDragging = true;
                barGrab = thumb.Contains(mouse) ? mouse.y - thumb.y : thumbH / 2f;
            }
            if (barDragging && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown))
            {
                float tY = Mathf.Clamp(mouse.y - barGrab - bar.y, 0f, bar.height - thumbH);
                scrollTarget = scroll = maxScroll * (tY / Mathf.Max(1f, bar.height - thumbH));
                e.Use();
            }
            if (e.rawType == EventType.MouseUp) barDragging = false;
        }

        private void DrawTile(ButtonInfo btn, Rect r, bool hover, Vector2 local, bool searchMode)
        {
            Event e = Event.current;
            bool on = btn.IsActive;
            float h = Anim("t" + btn.Name, hover);
            float o = Anim("o" + btn.Name, on, 10f);
            float pop = PopOf("o" + btn.Name);
            float rr = RR;

            Fill(r, rr, Color.Lerp(Color.Lerp(RowC, RowHoverC, h), RowOnC, o * 0.75f));

            if (o > 0.01f)
            {
                GUI.BeginGroup(r);
                float ph = Mathf.Repeat(Time.unscaledTime * 0.45f + r.y * 0.002f, 1.6f) - 0.3f;
                Tint(new Rect(ph * r.width - 70f, 0f, 140f, r.height), Tex.Sheen, new Color(1f, 1f, 1f, 0.07f * o));
                GUI.EndGroup();
            }

            if (ripples.TryGetValue(btn.Name, out Ripple rp))
            {
                float age = Mathf.Clamp01((Time.unscaledTime - rp.Time) * Mathf.Min(Speed, 4f) / 0.55f);
                if (age < 1f)
                {
                    float rad = Mathf.Lerp(8f, r.width * 1.05f, EaseOutCubic(age));
                    GUI.BeginGroup(r);
                    Tint(new Rect(rp.Pos.x - rad, rp.Pos.y - rad, rad * 2f, rad * 2f), Tex.Circle, new Color(1f, 1f, 1f, 0.16f * (1f - age)));
                    GUI.EndGroup();
                }
            }

            Edge(r, rr, Theme.WithA(Color.Lerp(EdgeC, Theme.Lighten(Acc, 0.5f), o), Mathf.Lerp(EdgeC.a * 0.5f, 0.95f, Mathf.Max(h * 0.8f, o))));

            float reserve;
            Rect ctrl;
            Rect optLeft = new Rect();
            switch (btn.Type)
            {
                case ButtonType.Toggle:
                    ctrl = new Rect(r.xMax - 50f, r.y, 44f, r.height);
                    DrawRiftSwitch(ctrl.center, o, pop, h);
                    reserve = 58f;
                    break;
                case ButtonType.Option:
                {
                    string text = btn.Current;
                    float vw = Mathf.Clamp(sChip.CalcSize(new GUIContent(text)).x, 24f, 110f);
                    float cwid = vw + 46f;
                    ctrl = new Rect(r.xMax - 10f - cwid, r.y + r.height / 2f - 13f, cwid, 26f);
                    optLeft = new Rect(ctrl.x, ctrl.y, 22f, ctrl.height);
                    Rect optRight = new Rect(ctrl.xMax - 22f, ctrl.y, 22f, ctrl.height);
                    bool hl = hover && optLeft.Contains(local), hr = hover && optRight.Contains(local);
                    float vp = PopOf("v" + btn.Name);
                    Fill(ctrl, Mathf.Min(rr, 8f), new Color(0f, 0f, 0f, hover ? 0.48f : 0.38f));
                    Edge(ctrl, Mathf.Min(rr, 8f), Theme.WithA(Acc, 0.35f + 0.4f * Mathf.Max(h, vp)));
                    Label(optLeft, "<", sChip, hl ? Theme.Lighten(Acc, 0.4f) : Dim);
                    Label(optRight, ">", sChip, hr ? Theme.Lighten(Acc, 0.4f) : Dim);
                    Label(new Rect(optLeft.xMax, ctrl.y - vp * 3f, ctrl.width - 44f, ctrl.height), text, sChip, Color.Lerp(TextC, Theme.Lighten(Acc, 0.5f), vp));
                    reserve = cwid + 18f;
                    break;
                }
                default:
                {
                    ctrl = new Rect(r.xMax - 46f, r.y + r.height / 2f - 13f, 36f, 26f);
                    float bp = PopOf("b" + btn.Name);
                    Fill(ctrl, Mathf.Min(rr, 8f), Color.Lerp(new Color(0f, 0f, 0f, 0.38f), Theme.WithA(Acc, 0.8f), bp));
                    Edge(ctrl, Mathf.Min(rr, 8f), Theme.WithA(Acc, 0.35f + 0.5f * h));
                    Label(new Rect(ctrl.x + h * 2f + bp * 3f, ctrl.y, ctrl.width, ctrl.height), ">", sChip, Color.Lerp(Dim, TextC, Mathf.Max(h, bp)));
                    reserve = 56f;
                    break;
                }
            }

            float textW = r.width - 18f - reserve;
            string desc = string.IsNullOrEmpty(btn.Tooltip) ? btn.Category : btn.Tooltip;
            if (searchMode) desc = btn.Category + "  -  " + desc;
            Label(new Rect(r.x + 16f + h * 2f, r.y + 7f, textW, 21f), btn.Name, sRow, TextC);
            Label(new Rect(r.x + 16f + h * 2f, r.y + 28f, textW, 17f), desc, sDesc, Theme.WithA(Dim, Dim.a * Mathf.Lerp(0.85f, 1f, h)));

            if (hover)
                tip = btn.Type == ButtonType.Toggle ? (on ? "Click to turn off" : "Click to turn on")
                    : btn.Type == ButtonType.Option ? "Click or use the arrows, right-click to go back"
                    : "Click to run";

            if (hover && e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
            {
                bool back = e.button == 1;
                if (btn.Type == ButtonType.Option)
                {
                    if (e.button == 0 && optLeft.Contains(local)) back = true;
                    Pop("v" + btn.Name);
                }
                else if (btn.Type == ButtonType.Button) Pop("b" + btn.Name);

                if (e.button == 0 || btn.Type == ButtonType.Option)
                {
                    ripples[btn.Name] = new Ripple { Pos = local - r.position, Time = Time.unscaledTime };
                    Buttons.Press(btn, back);
                }
                e.Use();
            }
        }

        private void DrawRiftSwitch(Vector2 c, float o, float pop, float hover)
        {
            float size = 26f + hover * 2f;
            float gap = Mathf.Lerp(0.3f, 4.8f, o) + pop * 2.5f;
            Vector2 off = Tex.RiftAxis * gap;

            if (o > 0.01f) Tint(new Rect(c.x - 24f, c.y - 24f, 48f, 48f), Tex.SoftDot, Theme.WithA(Acc, 0.38f * o));
            else Tint(new Rect(c.x - 15f, c.y - 15f, 30f, 30f), Tex.DiamondOutline, Theme.WithA(TextC, 0.1f + 0.15f * hover));

            Color dull = Theme.WithA(TextC, 0.22f + 0.15f * hover);
            Color lc = Color.Lerp(dull, Acc, o);
            Color rc = Color.Lerp(dull, Acc2, o);

            Matrix4x4 saved = GUI.matrix;
            if (pop > 0.01f) GUIUtility.RotateAroundPivot(Mathf.Sin(pop * Mathf.PI) * 18f, c);
            Rect d = new Rect(c.x - size / 2f, c.y - size / 2f, size, size);
            Tint(new Rect(d.x - off.x, d.y - off.y, size, size), Tex.RiftLeft, lc);
            Tint(new Rect(d.x + off.x, d.y + off.y, size, size), Tex.RiftRight, rc);
            if (o > 0.05f)
            {
                float ss = (8f + 3f * Mathf.Sin(Time.unscaledTime * 6f)) * o + pop * 6f;
                Tint(new Rect(c.x - ss / 2f, c.y - ss / 2f, ss, ss), Tex.Star, new Color(1f, 1f, 1f, o));
            }
            GUI.matrix = saved;
        }

        private void DrawFooter()
        {
            float y = H - FooterH + 6f;
            float full = W - Rail - Pad * 2f;
            Fill(new Rect(Rail + Pad, y - 6f, full, 1f), 0.5f, Theme.WithA(Acc, 0.12f));
            if (!string.IsNullOrEmpty(tip))
            {
                Label(new Rect(Rail + Pad, y, full, 22f), Up(tip), sSmall, Dim);
                return;
            }
            string def = !string.IsNullOrEmpty(query) ? "esc clears search" : "drag top to move  -  / search";
            Label(new Rect(Rail + Pad, y, full - 160f, 22f), Up(def), sSmall, Faint);
            Label(new Rect(W - Pad - 160f, y, 160f, 22f), Up("m hide  -  esc desktop off"), sSmallR, Faint);
        }

        private void DrawToasts()
        {
            float now = Time.unscaledTime;
            const float w = 290f, h = 48f, gap = 8f;
            float saved = ga;
            for (int i = 0; i < Notify.Toasts.Count; i++)
            {
                Notify.Toast t = Notify.Toasts[i];
                float age = now - t.Time;
                float inT = EaseOutBack(Mathf.Clamp01(age / 0.4f));
                float outA = Mathf.Clamp01((Notify.Life - age) / 0.45f);
                int fromBottom = Notify.Toasts.Count - 1 - i;
                Rect r = new Rect(Screen.width - w - 20f + (1f - inT) * (w + 40f), Screen.height - 20f - (fromBottom + 1) * (h + gap), w, h);

                ga = outA;
                Glow(r, Mathf.Min(RR, 12f), 12, Theme.WithA(Acc, 0.22f));
                Fill(r, Mathf.Min(RR, 12f), Theme.WithA(T.Panel, 0.96f));
                float split = 1.5f + Mathf.Sin(age * 6f) * 0.8f;
                DrawMark(new Vector2(r.x + 24f, r.center.y), 22f, split, 0.9f);
                Label(new Rect(r.x + 44f, r.y, r.width - 52f, r.height - 4f), t.Text, sToast, TextC);
                float life = 1f - Mathf.Clamp01(age / Notify.Life);
                Fill(new Rect(r.x + 44f, r.yMax - 7f, (r.width - 56f) * life, 2f), 1f, Theme.WithA(Acc, 0.8f));
                Edge(r, Mathf.Min(RR, 12f), Theme.WithA(EdgeC, 0.8f));
            }
            ga = saved;
        }

        private string WaveText(string text)
        {
            StringBuilder sb = new StringBuilder();
            float t = Time.unscaledTime;
            int n = Mathf.Max(1, text.Length - 1);
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '<' || ch == '>')
                {
                    sb.Append(ch);
                    continue;
                }
                float k = 0.5f + 0.5f * Mathf.Sin(t * 2.4f - i * 0.45f);
                Color c = Color.Lerp(T.Text, Theme.Lighten(Color.Lerp(Acc, Acc2, i / (float)n), 0.2f), k * 0.85f);
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>').Append(ch).Append("</color>");
            }
            return sb.ToString();
        }

        private static Rect Centered(Rect r, float size) =>
            new Rect(r.center.x - size / 2f, r.center.y - size / 2f, size, size);

        private static float Frac(float v) => v - Mathf.Floor(v);

        private Color Col(Color c) => new Color(c.r, c.g, c.b, c.a * ga);

        private void Fill(Rect r, float radius, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = Col(c);
            Tex.FillStyle(Mathf.Max(0f, Mathf.Round(radius))).Draw(r, false, false, false, false);
            GUI.color = Color.white;
        }

        private void Edge(Rect r, float radius, Color c, float width = 1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = Col(c);
            Tex.EdgeStyle(Mathf.Max(0f, Mathf.Round(radius)), width).Draw(r, false, false, false, false);
            GUI.color = Color.white;
        }

        private void Glow(Rect r, float radius, int spread, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = Col(c);
            Tex.GlowStyle(Mathf.Max(0f, Mathf.Round(radius)), spread).Draw(new Rect(r.x - spread, r.y - spread, r.width + spread * 2f, r.height + spread * 2f), false, false, false, false);
            GUI.color = Color.white;
        }

        private void Tint(Rect r, Texture tex, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = Col(c);
            GUI.DrawTexture(r, tex);
            GUI.color = Color.white;
        }

        private void Label(Rect r, string text, GUIStyle s, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = Col(c);
            GUI.Label(r, text, s);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (sTitle != null && font != null) return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            sBrand = Style(16, FontStyle.Bold, TextAnchor.MiddleLeft);
            sBrandSmall = Style(12, FontStyle.Bold, TextAnchor.MiddleLeft);
            sTitle = Style(24, FontStyle.Bold, TextAnchor.MiddleLeft);
            sSub = Style(12, FontStyle.Normal, TextAnchor.MiddleLeft);
            sTab = Style(12, FontStyle.Bold, TextAnchor.MiddleLeft);
            sBadge = Style(10, FontStyle.Bold, TextAnchor.MiddleCenter);
            sRow = Style(14, FontStyle.Bold, TextAnchor.MiddleLeft);
            sDesc = Style(11, FontStyle.Normal, TextAnchor.MiddleLeft);
            sChip = Style(12, FontStyle.Bold, TextAnchor.MiddleCenter);
            sSmall = Style(10, FontStyle.Bold, TextAnchor.MiddleLeft);
            sSmallR = Style(10, FontStyle.Bold, TextAnchor.MiddleRight);
            sCenter = Style(12, FontStyle.Bold, TextAnchor.MiddleCenter);
            sToast = Style(13, FontStyle.Bold, TextAnchor.MiddleLeft);
            sSearch = Style(13, FontStyle.Normal, TextAnchor.MiddleLeft);
            sSearch.richText = false;
            sSearch.focused.textColor = Color.white;
            sSearch.hover.textColor = Color.white;
            sSearch.active.textColor = Color.white;
        }

        private GUIStyle Style(int size, FontStyle style, TextAnchor anchor) =>
            new GUIStyle
            {
                font = font,
                fontSize = size,
                fontStyle = style,
                alignment = anchor,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
    }
}
