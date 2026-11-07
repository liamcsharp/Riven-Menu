using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

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
        public static bool DrawInVR;

        private const float W = 640f, H = 440f;
        private const float HeadH = 62f;
        private const float FootH = 30f;
        private const float SideX = 12f, SideW = 158f, BodyY = HeadH + 10f;
        private const float BodyH = H - BodyY - FootH - 8f;
        private const float SideItemH = 30f;
        private const float ContentX = SideX + SideW + 12f;
        private const float ContentW = W - ContentX - 12f;
        private const float TitleH = 48f;
        private const float RowH = 46f, RowGap = 6f;
        private const float SearchW = 190f;
        private const string SearchControl = "riven_search";

        private Vector2 pos = new Vector2(40f, 40f);
        private Vector2 origin;
        private float drawScale = 1f;
        private bool visible = true;
        private float openT, ga = 1f, dt;
        private int lastFrame = -1;

        private int tab = 3;
        private string query = "";
        private bool searchFocused, wantFocus, clearFocus;
        private readonly List<ButtonInfo> results = new List<ButtonInfo>();
        private string resultsFor;

        private float scroll, scrollTarget, sideScroll, sideScrollTarget;
        private bool dragging;
        private Vector2 dragGrab;
        private float switchTime;

        private int[] tabCounts = new int[0];
        private int activeCount;
        private float nextCount;
        private string statusText = "";

        private readonly Dictionary<ButtonInfo, float> hoverOf = new Dictionary<ButtonInfo, float>();
        private readonly Dictionary<ButtonInfo, float> onOf = new Dictionary<ButtonInfo, float>();
        private readonly Dictionary<ButtonInfo, float> popOf = new Dictionary<ButtonInfo, float>();
        private readonly List<ButtonInfo> popKeys = new List<ButtonInfo>();
        private float[] sideHover = new float[0];
        private float sideMarkY = -1f;
        private float pwrHover, pwrPop, searchHover, searchFocusA;

        private Font font;
        private GUIStyle sTitle, sTitleSmall, sCat, sCatDim, sHead, sSub, sRow, sDesc, sChip, sBadge, sFoot, sToast, sSearch;

        public static bool IsTyping => Instance != null && Instance.isActiveAndEnabled && Instance.visible && Instance.searchFocused;
        public static bool Visible => Instance != null && Instance.visible;

        public static bool MouseOver
        {
            get
            {
                if (Instance == null || !Instance.isActiveAndEnabled || !Instance.visible || Mouse.current == null) return false;
                if (XRSettings.isDeviceActive && !DrawInVR) return false;
                Vector2 m = Mouse.current.position.ReadValue();
                m.y = Screen.height - m.y;
                float s = Instance.drawScale;
                return new Rect(Instance.origin.x, Instance.origin.y, W * s, H * s).Contains(m);
            }
        }

        public static void ResetPosition()
        {
            if (Instance != null) Instance.pos = new Vector2(40f, 40f);
        }

        public static void Toggle()
        {
            if (Instance != null) Instance.SetOpenLocal(!Instance.visible);
        }

        public static void SetOpen(bool open)
        {
            if (Instance != null) Instance.SetOpenLocal(open);
        }

        private void Awake()
        {
            Instance = this;
            useGUILayout = false;
            Buttons.Pressed += OnPressed;
            tab = Mathf.Clamp(tab, 0, Menu.Tabs.Count - 1);
        }

        private void OnDestroy() => Buttons.Pressed -= OnPressed;

        private void SetOpenLocal(bool open)
        {
            if (visible == open) return;
            visible = open;
            if (open)
            {
                clearFocus = true;
                switchTime = Time.unscaledTime;
            }
            else
            {
                searchFocused = false;
                GUIUtility.keyboardControl = 0;
            }
            dragging = false;
            Click();
        }

        private void OnPressed(ButtonInfo info)
        {
            nextCount = 0f;
            popOf[info] = 1f;
            if (!SoundsOn || RivenAudio.Instance == null) return;
            if (info.Type == ButtonType.Toggle) { if (info.Enabled) RivenAudio.Instance.PlayOn(); else RivenAudio.Instance.PlayOff(); }
            else RivenAudio.Instance.PlayClick();
        }

        private static void Click()
        {
            if (SoundsOn && RivenAudio.Instance != null) RivenAudio.Instance.PlayClick();
        }

        private float Speed => AnimSpeed <= 0f ? 1000f : AnimSpeed;

        private float Approach(float v, float goal, float rate = 14f)
        {
            if (AnimSpeed <= 0f) return goal;
            return Mathf.Lerp(v, goal, 1f - Mathf.Exp(-rate * Mathf.Min(Speed, 4f) * dt));
        }

        private float Approach(float v, bool on, float rate = 14f) => Approach(v, on ? 1f : 0f, rate);

        private void Tick()
        {
            dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float target = visible ? 1f : 0f;
            openT = AnimSpeed <= 0f ? target : Mathf.MoveTowards(openT, target, dt * 4.5f * Speed);

            scroll = Approach(scroll, scrollTarget, 16f);
            sideScroll = Approach(sideScroll, sideScrollTarget, 16f);
            pwrPop = Mathf.Max(0f, pwrPop - dt * 3f * Speed);

            if (popOf.Count > 0)
            {
                popKeys.Clear();
                popKeys.AddRange(popOf.Keys);
                for (int i = 0; i < popKeys.Count; i++)
                {
                    float v = popOf[popKeys[i]] - dt * 3.5f * Speed;
                    if (v <= 0f) popOf.Remove(popKeys[i]);
                    else popOf[popKeys[i]] = v;
                }
            }

            float now = Time.unscaledTime;
            for (int i = Notify.Toasts.Count - 1; i >= 0; i--)
                if (now - Notify.Toasts[i].Time > Notify.Life) Notify.Toasts.RemoveAt(i);

            if (Mouse.current != null && !Mouse.current.leftButton.isPressed) dragging = false;

            if (now >= nextCount)
            {
                nextCount = now + 0.25f;
                int count = Menu.Tabs.Count;
                if (tabCounts.Length != count) tabCounts = new int[count];
                if (sideHover.Length != count) sideHover = new float[count];
                for (int i = 0; i < count; i++)
                {
                    int on = 0;
                    List<ButtonInfo> list = Menu.Tabs[i].Items;
                    for (int j = 0; j < list.Count; j++) if (list[j].IsActive) on++;
                    tabCounts[i] = on;
                }
                int total = 0;
                List<ButtonInfo> all = Buttons.All;
                for (int i = 0; i < all.Count; i++)
                    if (all[i].IsActive && !Buttons.IsSetting(all[i])) total++;
                activeCount = total;
                string room = Mods.Important.InRoom ? Mods.Important.RoomCode + "  " + Mods.Important.RoomPlayers + "/10" : "not in a room";
                statusText = total + (total == 1 ? " mod on" : " mods on") + "     " + room + "     " + Mathf.RoundToInt(Hud.Fps) + " fps";
            }
        }

        private bool Searching => !string.IsNullOrEmpty(query) && query.Trim().Length > 0;

        private void RefreshResults()
        {
            if (resultsFor == query) return;
            resultsFor = query;
            results.Clear();
            if (!Searching) return;
            string q = query.Trim();
            List<ButtonInfo> all = Buttons.All;
            for (int i = 0; i < all.Count; i++)
            {
                ButtonInfo b = all[i];
                if (b.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    b.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (b.Tooltip ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    results.Add(b);
            }
        }

        private List<ButtonInfo> CurrentItems
        {
            get
            {
                if (Searching) return results;
                tab = Mathf.Clamp(tab, 0, Menu.Tabs.Count - 1);
                return Menu.Tabs[tab].Items;
            }
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void OnGUI()
        {
            if (XRSettings.isDeviceActive && !DrawInVR) return;

            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldCursor = GUI.skin.settings.cursorColor;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -1000;

            try
            {
                if (lastFrame != Time.frameCount)
                {
                    lastFrame = Time.frameCount;
                    Tick();
                }
                EnsureStyles();
                RefreshResults();

                if (openT > 0.004f || visible)
                {
                    GUI.skin.settings.cursorColor = Theme.Lighten(Theme.Accent, 0.4f);
                    float baseScale = Mathf.Clamp(Scale, 0.5f, 2f);
                    pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, Screen.width - W * baseScale));
                    pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, Screen.height - H * baseScale));

                    float ease = visible ? EaseOutBack(openT) : EaseOutCubic(openT);
                    ga = EaseOutCubic(openT);
                    float s = baseScale * Mathf.LerpUnclamped(0.94f, 1f, ease);
                    drawScale = s;
                    Vector2 center = pos + new Vector2(W, H) * (baseScale / 2f);
                    origin = center - new Vector2(W, H) * (s / 2f) + new Vector2(0f, (1f - ease) * 14f);
                    GUI.matrix = Matrix4x4.TRS(new Vector3(origin.x, origin.y, 0f), Quaternion.identity, new Vector3(s, s, 1f));
                    DrawMenu();
                    GUI.matrix = Matrix4x4.identity;
                }

                DrawToasts();
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
                GUI.skin.settings.cursorColor = oldCursor;
            }
        }

        private MenuTheme T => Theme.Current;
        private Color Acc => Theme.Accent;
        private Color Acc2 => Theme.Accent2;
        private Color TextC => T.Text;
        private Color Dim => Theme.WithA(Color.Lerp(T.Text, Acc, 0.25f), 0.6f);
        private Color Faint => Theme.WithA(Color.Lerp(T.Text, Acc, 0.25f), 0.36f);
        private Color Card => Theme.WithA(Color.Lerp(T.Panel, T.Row, 0.55f), 0.92f);
        private Color CardHover => Theme.WithA(Color.Lerp(Color.Lerp(T.Panel, T.Row, 0.8f), Acc, 0.12f), 0.96f);
        private Color Line => Theme.WithA(Theme.Lighten(Acc, 0.3f), 0.12f + T.Edge * 0.25f);

        private Color CatColor(int i)
        {
            int n = Mathf.Max(1, Menu.Tabs.Count - 1);
            return Color.Lerp(Acc, Acc2, i / (float)n);
        }

        private static readonly Rect PanelRect = new Rect(0f, 0f, W, H);
        private static readonly Rect HeaderRect = new Rect(0f, 0f, W, HeadH);
        private static readonly Rect PowerRect = new Rect(W - 14f - 38f, 12f, 38f, 38f);
        private static readonly Rect SearchRect = new Rect(W - 14f - 38f - 10f - SearchW, 16f, SearchW, 30f);
        private static readonly Rect SideRect = new Rect(SideX, BodyY, SideW, BodyH);
        private static readonly Rect ContentRect = new Rect(ContentX, BodyY, ContentW, BodyH);
        private static readonly Rect ListRect = new Rect(ContentX, BodyY + TitleH, ContentW, BodyH - TitleH);
        private static readonly Rect FootRect = new Rect(0f, H - FootH, W, FootH);

        private void DrawMenu()
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            bool overMenu = PanelRect.Contains(mouse);

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
            HandleScroll(e, mouse);

            if (e.type == EventType.Repaint)
            {
                if (GlowOn) Glow(PanelRect, Theme.Radius, 20, Theme.WithA(Acc, 0.14f + 0.06f * Mathf.Sin(Time.unscaledTime * 1.8f)));
                Fill(PanelRect, Theme.Radius, Theme.WithA(T.Panel, Theme.Opacity));
                Tint(new Rect(Theme.Radius, 0f, W - Theme.Radius * 2f, 120f), TopGlow(), Theme.WithA(Acc, 0.07f));
                if (T.Scanlines) GUI.DrawTextureWithTexCoords(PanelRect, Tex.Scanline, new Rect(0f, 0f, W / 4f, H / 4f));
                if (ParticlesOn && T.Particles) DrawParticles();
            }

            DrawHeader(e, mouse);
            DrawSidebar(e, mouse);
            DrawContent(e, mouse);
            DrawFooter(e);

            if (e.type == EventType.Repaint) Edge(PanelRect, Theme.Radius, Line, 1.2f);
            if (overMenu && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel)) e.Use();
        }

        private void DrawParticles()
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < 16; i++)
            {
                float seed = i * 12.9898f;
                float rx = Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, 1f);
                float speed = 8f + Mathf.Repeat(Mathf.Sin(seed * 1.7f) * 9631.13f, 1f) * 14f;
                float y = H - Mathf.Repeat(t * speed + rx * H * 3f, H + 20f);
                float x = rx * (W - 30f) + 15f + Mathf.Sin(t * 0.7f + i) * 10f;
                float size = 3f + Mathf.Repeat(rx * 7.3f, 1f) * 5f;
                float edge = Mathf.Clamp01(Mathf.Min(y - 10f, H - y) / 60f);
                Tint(new Rect(x, y, size, size), Tex.SoftDot, Theme.WithA(i % 2 == 0 ? Acc : Acc2, 0.35f * edge));
            }
        }

        private static Texture2D flipped;

        private static Texture2D TopGlow()
        {
            if (flipped != null) return flipped;
            flipped = new Texture2D(2, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[2 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 2; x++) px[y * 2 + x] = new Color(1f, 1f, 1f, y / 63f);
            flipped.SetPixels(px);
            flipped.Apply();
            return flipped;
        }

        private void HandleDrag(Event e, Vector2 mouse)
        {
            bool onHeader = HeaderRect.Contains(mouse) && !SearchRect.Contains(mouse) && !PowerRect.Contains(mouse);
            if (e.type == EventType.MouseDown && e.button == 0 && onHeader)
            {
                dragging = true;
                dragGrab = mouse;
                e.Use();
                return;
            }
            if (dragging && e.type == EventType.MouseDrag)
            {
                Vector2 screen = origin + mouse * drawScale;
                pos = screen - dragGrab * Mathf.Clamp(Scale, 0.5f, 2f);
                e.Use();
            }
            if (e.rawType == EventType.MouseUp) dragging = false;
        }

        private float ContentHeight => CurrentItems.Count * (RowH + RowGap);
        private float SideHeight => Menu.Tabs.Count * (SideItemH + 2f);

        private void HandleScroll(Event e, Vector2 mouse)
        {
            if (e.type != EventType.ScrollWheel) return;
            float delta = e.delta.y * 22f;
            if (SideRect.Contains(mouse))
            {
                sideScrollTarget = Mathf.Clamp(sideScrollTarget + delta, 0f, Mathf.Max(0f, SideHeight - SideRect.height));
                e.Use();
            }
            else if (ContentRect.Contains(mouse))
            {
                scrollTarget = Mathf.Clamp(scrollTarget + delta, 0f, Mathf.Max(0f, ContentHeight - ListRect.height + 6f));
                e.Use();
            }
        }

        private void DrawHeader(Event e, Vector2 mouse)
        {
            bool hp = PowerRect.Contains(mouse);
            if (e.type == EventType.Repaint)
            {
                pwrHover = Approach(pwrHover, hp);
                Rect logo = new Rect(16f, 13f, 36f, 36f);
                if (Logo.Texture != null)
                {
                    Tint(Inflate(logo, 10f), Tex.SoftDot, Theme.WithA(Acc, 0.35f + 0.1f * Mathf.Sin(Time.unscaledTime * 2.2f)));
                    Tint(logo, Logo.Texture, Color.white);
                }
                else Tint(logo, Tex.Diamond, Acc);

                Label(new Rect(62f, 10f, 200f, 26f), "Riven", sTitle, TextC);
                Label(new Rect(63f, 34f, 220f, 18f), "<color=#" + ColorUtility.ToHtmlStringRGB(Acc) + ">MENU</color>   v" + Menu.Version, sTitleSmall, Dim);

                Rect line = new Rect(14f, HeadH - 1f, W - 28f, 1.5f);
                Tint(line, Texture2D.whiteTexture, Theme.WithA(Acc, 0.55f));
                Tint(line, Tex.FadeRight, Theme.WithA(Acc2, 0.8f));

                Fill(PowerRect, 11f, Color.Lerp(Card, CardHover, pwrHover));
                Edge(PowerRect, 11f, Theme.WithA(Line, Mathf.Lerp(Line.a, 0.8f, pwrHover)));
                bool any = activeCount > 0;
                if (any) Tint(Inflate(PowerRect, 6f), Tex.SoftDot, Theme.WithA(Acc, 0.18f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f)));
                Matrix4x4 saved = GUI.matrix;
                if (pwrPop > 0f) GUIUtility.RotateAroundPivot(pwrPop * 180f, PowerRect.center);
                Tint(Centered(PowerRect, 20f), Tex.Power, any ? Theme.Lighten(Acc, 0.45f) : Theme.WithA(TextC, 0.45f));
                GUI.matrix = saved;
            }
            if (hp && e.type == EventType.MouseDown && e.button == 0)
            {
                pwrPop = 1f;
                Buttons.DisableAll();
                Notify.Push("All mods off");
                nextCount = 0f;
                e.Use();
            }
            DrawSearch(e, mouse);
        }

        private void DrawSearch(Event e, Vector2 mouse)
        {
            Rect sr = SearchRect;
            if (e.type == EventType.Repaint)
            {
                searchHover = Approach(searchHover, sr.Contains(mouse));
                searchFocusA = Approach(searchFocusA, searchFocused);
                Fill(sr, 10f, Theme.WithA(Color.Lerp(Color.black, T.Row, 0.25f), 0.55f + 0.1f * searchHover));
                Edge(sr, 10f, Theme.WithA(Color.Lerp(Line, Theme.Lighten(Acc, 0.3f), searchFocusA), Mathf.Lerp(Line.a, 0.9f, Mathf.Max(searchFocusA, searchHover * 0.5f))));
                Tint(new Rect(sr.x + 10f, sr.center.y - 6f, 12f, 12f), Tex.DiamondOutline, Theme.WithA(Acc, 0.75f));
            }

            if (searchFocused && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ClearSearch();
                e.Use();
            }

            Rect field = new Rect(sr.x + 28f, sr.y, sr.width - 52f, sr.height);
            GUI.SetNextControlName(SearchControl);
            GUI.color = new Color(1f, 1f, 1f, ga);
            string next = GUI.TextField(field, query ?? "", 32, sSearch);
            GUI.color = Color.white;
            if (e.type != EventType.Layout && e.type != EventType.Used)
                searchFocused = GUI.GetNameOfFocusedControl() == SearchControl;

            if (next != query)
            {
                query = next;
                RefreshResults();
                scrollTarget = scroll = 0f;
                switchTime = Time.unscaledTime;
            }

            Rect x = new Rect(sr.xMax - 24f, sr.y + 3f, 22f, 24f);
            if (e.type == EventType.Repaint)
            {
                if (string.IsNullOrEmpty(query) && !searchFocused) Label(field, "Search mods...", sSub, Faint);
                if (!string.IsNullOrEmpty(query)) Label(x, "x", sChip, x.Contains(mouse) ? TextC : Dim);
            }
            if (!string.IsNullOrEmpty(query) && x.Contains(mouse) && e.type == EventType.MouseDown && e.button == 0)
            {
                ClearSearch();
                e.Use();
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
            RefreshResults();
            scrollTarget = scroll = 0f;
            switchTime = Time.unscaledTime;
            GUIUtility.keyboardControl = 0;
            searchFocused = false;
        }

        private void DrawSidebar(Event e, Vector2 mouse)
        {
            bool repaint = e.type == EventType.Repaint;
            if (repaint) Fill(SideRect, 12f, Theme.WithA(Color.Lerp(T.Panel, Color.black, 0.25f), 0.55f));

            GUI.BeginGroup(SideRect);
            Vector2 local = mouse - SideRect.position;
            bool inSide = new Rect(0f, 0f, SideRect.width, SideRect.height).Contains(local);
            int count = Menu.Tabs.Count;
            if (sideHover.Length != count) sideHover = new float[count];

            float targetMark = 4f + tab * (SideItemH + 2f) - sideScroll;
            if (sideMarkY < 0f) sideMarkY = targetMark;
            if (repaint) sideMarkY = Approach(sideMarkY, targetMark, 18f);
            if (repaint && !Searching)
            {
                Rect mark = new Rect(4f, sideMarkY, SideRect.width - 8f, SideItemH);
                Fill(mark, 9f, Theme.WithA(Color.Lerp(T.Row, CatColor(tab), 0.3f), 0.85f));
                Fill(new Rect(mark.x + 2f, mark.y + 7f, 3f, mark.height - 14f), 1.5f, CatColor(tab));
            }

            for (int i = 0; i < count; i++)
            {
                Rect r = new Rect(4f, 4f + i * (SideItemH + 2f) - sideScroll, SideRect.width - 8f, SideItemH);
                if (r.yMax < 0f || r.y > SideRect.height) continue;
                bool hover = inSide && r.Contains(local) && !dragging;
                bool active = i == tab && !Searching;
                if (repaint)
                {
                    sideHover[i] = Approach(sideHover[i], hover);
                    if (!active && sideHover[i] > 0.01f) Fill(r, 9f, Theme.WithA(CardHover, 0.6f * sideHover[i]));
                    Color dot = CatColor(i);
                    Tint(new Rect(r.x + 12f, r.center.y - 4f, 8f, 8f), Tex.Circle, active ? dot : Theme.WithA(dot, 0.55f + 0.4f * sideHover[i]));
                    Label(new Rect(r.x + 28f + sideHover[i] * 2f, r.y, r.width - 60f, r.height), Menu.Tabs[i].Name, active ? sCat : sCatDim, active ? TextC : Color.Lerp(Dim, TextC, sideHover[i]));
                    int on = i < tabCounts.Length ? tabCounts[i] : 0;
                    if (on > 0)
                    {
                        Rect b = new Rect(r.xMax - 28f, r.center.y - 9f, 22f, 18f);
                        Fill(b, 9f, Theme.WithA(dot, active ? 0.9f : 0.35f));
                        Label(b, on > 99 ? "99" : on.ToString(), sBadge, active ? Color.white : TextC);
                    }
                }
                if (hover && e.type == EventType.MouseDown && e.button == 0)
                {
                    if (Searching) ClearSearch();
                    if (tab != i)
                    {
                        tab = i;
                        scrollTarget = scroll = 0f;
                        switchTime = Time.unscaledTime;
                        Click();
                    }
                    e.Use();
                }
            }
            GUI.EndGroup();

            if (repaint && SideHeight > SideRect.height)
            {
                float frac = SideRect.height / SideHeight;
                float h = Mathf.Max(24f, SideRect.height * frac);
                float y = SideRect.y + (SideRect.height - h) * (sideScroll / Mathf.Max(1f, SideHeight - SideRect.height));
                Fill(new Rect(SideRect.xMax - 3f, y, 2f, h), 1f, Theme.WithA(Acc, 0.35f));
            }
        }

        private void DrawContent(Event e, Vector2 mouse)
        {
            bool repaint = e.type == EventType.Repaint;
            List<ButtonInfo> items = CurrentItems;
            float since = Time.unscaledTime - switchTime;

            if (repaint)
            {
                float ti = EaseOutCubic(since * Speed / 0.25f);
                float saved = ga;
                ga *= ti;
                Rect title = new Rect(ContentX + 4f + (1f - ti) * 10f, BodyY, ContentW - 8f, 26f);
                string name = Searching ? "Search" : Menu.Tabs[tab].Name;
                string sub = Searching ? items.Count + (items.Count == 1 ? " result for \"" : " results for \"") + query.Trim() + "\"" : Menu.Tabs[tab].Tooltip;
                Label(title, name, sHead, TextC);
                Label(new Rect(title.x, BodyY + 24f, title.width, 18f), sub, sSub, Dim);
                if (!Searching)
                {
                    int on = tab < tabCounts.Length ? tabCounts[tab] : 0;
                    if (on > 0) Label(new Rect(ContentX, BodyY + 2f, ContentW - 6f, 22f), on + " ON", sBadge, CatColor(tab));
                }
                ga = saved;
            }

            if (items.Count == 0 && repaint)
                Label(new Rect(ContentX, ListRect.y + 50f, ContentW, 24f), Searching ? "Nothing matches that" : "Nothing here yet", sChip, Faint);

            scrollTarget = Mathf.Clamp(scrollTarget, 0f, Mathf.Max(0f, ContentHeight - ListRect.height + 6f));

            GUI.BeginGroup(ListRect);
            Vector2 local = mouse - ListRect.position;
            bool inList = new Rect(0f, 0f, ListRect.width, ListRect.height).Contains(local);
            for (int i = 0; i < items.Count; i++)
            {
                float y = i * (RowH + RowGap) - scroll;
                if (y + RowH < 0f || y > ListRect.height) continue;
                ButtonInfo btn = items[i];
                Rect r = new Rect(0f, y, ListRect.width - 8f, RowH);
                bool hover = inList && !dragging && r.Contains(local);

                if (repaint)
                {
                    float ti = EaseOutCubic((since - Mathf.Min(i, 10) * 0.025f) * Speed / 0.28f);
                    float saved = ga;
                    ga *= ti;
                    Rect dr = new Rect(r.x + (1f - ti) * 16f, r.y, r.width, r.height);
                    PaintRow(dr, btn, hover, local);
                    ga = saved;
                }

                if (hover && e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
                {
                    bool back = e.button == 1;
                    if (btn.Type == ButtonType.Option && e.button == 0) back = local.x < OptionRect(r).center.x;
                    if (e.button == 0 || btn.Type == ButtonType.Option) Buttons.Press(btn, back);
                    e.Use();
                }
            }
            GUI.EndGroup();

            if (repaint && ContentHeight > ListRect.height)
            {
                float frac = ListRect.height / ContentHeight;
                float h = Mathf.Max(28f, ListRect.height * frac);
                float y = ListRect.y + (ListRect.height - h) * (scroll / Mathf.Max(1f, ContentHeight - ListRect.height + 6f));
                Fill(new Rect(ListRect.xMax - 3f, y, 3f, h), 1.5f, Theme.WithA(Acc, 0.45f));
            }
        }

        private static Rect OptionRect(Rect r) => new Rect(r.xMax - 12f - 124f, r.center.y - 13f, 124f, 26f);

        private float Get(Dictionary<ButtonInfo, float> d, ButtonInfo b) => d.TryGetValue(b, out float v) ? v : 0f;

        private void PaintRow(Rect r, ButtonInfo btn, bool hover, Vector2 mouse)
        {
            float h = Approach(Get(hoverOf, btn), hover);
            hoverOf[btn] = h;
            float o = Approach(Get(onOf, btn), btn.IsActive, 12f);
            onOf[btn] = o;
            float pop = Get(popOf, btn);
            int catIndex = CategoryIndex(btn.Category);
            Color cat = CatColor(catIndex);

            Rect pr = Inflate(r, -Mathf.Sin(pop * Mathf.PI) * 2.5f);
            Fill(pr, Theme.RowRadius, Color.Lerp(Color.Lerp(Card, CardHover, h), Theme.WithA(Color.Lerp(T.Row, cat, 0.35f), 0.95f), o * 0.75f));
            if (o > 0.01f) Tint(new Rect(pr.x, pr.y, pr.width * 0.6f, pr.height), Flip2(), Theme.WithA(cat, 0.16f * o));
            Edge(pr, Theme.RowRadius, Theme.WithA(Color.Lerp(Line, cat, o), Mathf.Lerp(Line.a * 0.8f, 0.85f, Mathf.Max(h * 0.6f, o))));
            Fill(new Rect(pr.x + 6f, pr.y + 10f, 3f, pr.height - 20f), 1.5f, Theme.WithA(cat, 0.25f + 0.75f * Mathf.Max(o, h * 0.6f)));

            float reserve;
            switch (btn.Type)
            {
                case ButtonType.Toggle:
                {
                    Rect track = new Rect(r.xMax - 14f - 40f, r.center.y - 11f, 40f, 22f);
                    Tint(track, Pill, Color.Lerp(new Color(0f, 0f, 0f, 0.45f), cat, o));
                    if (o > 0.02f) Tint(Inflate(track, 6f), Tex.SoftDot, Theme.WithA(cat, 0.25f * o));
                    float kx = Mathf.Lerp(track.x + 3f, track.xMax - 19f, EaseOutBack(o));
                    Tint(new Rect(kx, track.y + 3f, 16f, 16f), Tex.Circle, Color.white);
                    reserve = 66f;
                    break;
                }
                case ButtonType.Option:
                {
                    Rect chip = OptionRect(r);
                    bool inChip = chip.Contains(mouse);
                    bool left = inChip && mouse.x < chip.center.x;
                    Fill(chip, 9f, new Color(0f, 0f, 0f, 0.38f + 0.15f * h));
                    Edge(chip, 9f, Theme.WithA(cat, 0.25f + 0.4f * h));
                    Label(new Rect(chip.x, chip.y, 24f, chip.height), "<", sChip, left ? Theme.Lighten(cat, 0.4f) : Dim);
                    Label(new Rect(chip.xMax - 24f, chip.y, 24f, chip.height), ">", sChip, inChip && !left ? Theme.Lighten(cat, 0.4f) : Dim);
                    Label(new Rect(chip.x + 22f, chip.y, chip.width - 44f, chip.height), btn.Current, sChip, TextC);
                    reserve = 146f;
                    break;
                }
                default:
                {
                    Rect chip = new Rect(r.xMax - 14f - 52f, r.center.y - 12f, 52f, 24f);
                    Fill(chip, 9f, Color.Lerp(new Color(0f, 0f, 0f, 0.38f + 0.15f * h), Theme.WithA(cat, 0.9f), Mathf.Max(pop, h * 0.35f)));
                    Label(chip, "RUN", sChip, Color.Lerp(Dim, Color.white, Mathf.Max(h, pop)));
                    reserve = 76f;
                    break;
                }
            }

            float tx = r.x + 18f + h * 2f;
            float tw = r.width - 22f - reserve;
            Label(new Rect(tx, r.y + 6f, tw, 20f), btn.Name, sRow, TextC);
            string desc = Searching ? btn.Category + "  -  " + btn.Tooltip : btn.Tooltip;
            Label(new Rect(tx, r.y + 24f, tw, 16f), desc, sDesc, Theme.WithA(Dim, Dim.a * (0.8f + 0.2f * h)));
        }

        private int CategoryIndex(string cat)
        {
            for (int i = 0; i < Menu.Tabs.Count; i++)
                if (string.Equals(Menu.Tabs[i].Name, cat, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }

        private static Texture2D flip2;

        private static Texture2D Flip2()
        {
            if (flip2 != null) return flip2;
            flip2 = new Texture2D(64, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[64 * 2];
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 64; x++) px[y * 64 + x] = new Color(1f, 1f, 1f, 1f - x / 63f);
            flip2.SetPixels(px);
            flip2.Apply();
            return flip2;
        }

        private void DrawFooter(Event e)
        {
            if (e.type != EventType.Repaint) return;
            Tint(new Rect(14f, FootRect.y, W - 28f, 1f), Texture2D.whiteTexture, Theme.WithA(Line, Line.a * 0.8f));
            Tint(new Rect(18f, FootRect.center.y - 3f, 6f, 6f), Tex.Circle, activeCount > 0 ? Acc : Faint);
            Label(new Rect(30f, FootRect.y, W * 0.6f, FootRect.height), statusText, sFoot, Dim);
            Label(new Rect(W * 0.5f, FootRect.y, W * 0.5f - 18f, FootRect.height), Searching ? "ESC clears search" : "M hide   /   / search   /   drag the top bar", sFoot, Faint, TextAnchor.MiddleRight);
        }

        private void DrawToasts()
        {
            if (Event.current.type != EventType.Repaint || Notify.Toasts.Count == 0) return;
            float now = Time.unscaledTime;
            const float w = 300f, h = 52f, gap = 8f;
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
                Glow(r, 12f, 10, Theme.WithA(Acc, 0.12f));
                Fill(r, 12f, Theme.WithA(T.Panel, 0.97f));
                Rect icon = new Rect(r.x + 12f, r.center.y - 14f, 28f, 28f);
                if (Logo.Texture != null) Tint(icon, Logo.Texture, Color.white);
                else Tint(icon, Tex.Diamond, Acc);
                Label(new Rect(r.x + 50f, r.y + 7f, r.width - 60f, 16f), "RIVEN", sDesc, Acc);
                Label(new Rect(r.x + 50f, r.y + 22f, r.width - 60f, 22f), t.Text, sToast, TextC);
                float life = 1f - Mathf.Clamp01(age / Notify.Life);
                Tint(new Rect(r.x + 12f, r.yMax - 3f, (r.width - 24f) * life, 2f), Texture2D.whiteTexture, Theme.WithA(Acc, 0.85f));
                Edge(r, 12f, Theme.WithA(Line, 0.6f));
            }
            ga = saved;
        }

        private Texture2D pill;
        private Texture2D Pill => pill != null ? pill : pill = Tex.Pill(80, 44);

        private static Rect Inflate(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + d * 2f, r.height + d * 2f);

        private static Rect Centered(Rect r, float size) =>
            new Rect(r.center.x - size / 2f, r.center.y - size / 2f, size, size);

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
            if (Event.current.type != EventType.Repaint || tex == null) return;
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

        private void Label(Rect r, string text, GUIStyle s, Color c, TextAnchor anchor)
        {
            TextAnchor old = s.alignment;
            s.alignment = anchor;
            Label(r, text, s, c);
            s.alignment = old;
        }

        private void EnsureStyles()
        {
            if (sTitle != null && font != null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            sTitle = Style(22, FontStyle.Bold, TextAnchor.MiddleLeft);
            sTitleSmall = Style(10, FontStyle.Bold, TextAnchor.MiddleLeft);
            sCat = Style(12, FontStyle.Bold, TextAnchor.MiddleLeft);
            sCatDim = Style(12, FontStyle.Normal, TextAnchor.MiddleLeft);
            sHead = Style(18, FontStyle.Bold, TextAnchor.MiddleLeft);
            sSub = Style(11, FontStyle.Normal, TextAnchor.MiddleLeft);
            sRow = Style(13, FontStyle.Bold, TextAnchor.MiddleLeft);
            sDesc = Style(10, FontStyle.Normal, TextAnchor.MiddleLeft);
            sChip = Style(11, FontStyle.Bold, TextAnchor.MiddleCenter);
            sBadge = Style(10, FontStyle.Bold, TextAnchor.MiddleCenter);
            sFoot = Style(10, FontStyle.Bold, TextAnchor.MiddleLeft);
            sToast = Style(12, FontStyle.Bold, TextAnchor.MiddleLeft);
            sSearch = Style(12, FontStyle.Bold, TextAnchor.MiddleLeft);
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
