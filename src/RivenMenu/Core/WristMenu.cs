using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivenMenu
{
    public class WristMenu : MonoBehaviour
    {
        public static WristMenu Instance;
        public static bool Enabled = true;
        public static float Height = 0.07f;
        public static float Size = 1f;

        private const float W = 360f, H = 556f, Pad = 14f;
        private const float HeadY = 14f, HeadH = 52f;
        private const float CatY = 80f, CatH = 50f, ArrowW = 54f;
        private const float RowsY = 142f, RowH = 50f, Gap = 6f;
        private const int PerPage = 6;
        private const float PagerY = RowsY + PerPage * (RowH + Gap);
        private const float PagerH = 40f;
        private const float RowW = W - Pad * 2f;
        private const float PanelMeters = 0.25f;

        private const int IdPower = 0, IdCatPrev = 1, IdCatNext = 2, IdPagePrev = 3, IdPageNext = 4, IdRow0 = 5, IdCount = IdRow0 + PerPage;

        private class Quad
        {
            public Transform T;
            public Material Mat;
            public Vector3 BaseScale;
            public Color LastColor = new Color(-1f, 0f, 0f, 0f);

            public void SetColor(Color c)
            {
                if (c == LastColor) return;
                LastColor = c;
                Mat.color = c;
            }

            public void Show(bool on)
            {
                if (T.gameObject.activeSelf != on) T.gameObject.SetActive(on);
            }
        }

        private class Label
        {
            public TextMesh Tm;
            public string Last;
            public Color LastColor = new Color(-1f, 0f, 0f, 0f);

            public void Set(string text, Color c)
            {
                if (text != Last) { Last = text; Tm.text = text; }
                if (c != LastColor) { LastColor = c; Tm.color = c; }
            }

            public void Show(bool on)
            {
                if (Tm.gameObject.activeSelf != on) Tm.gameObject.SetActive(on);
            }
        }

        private class Row
        {
            public Transform Holder;
            public Quad Bg, Bar, Glow, Track, Knob, Chip, WideChip;
            public Label Name, Desc, ChipText;
        }

        private Transform root, ui;
        private Font font;
        private Material textMat;
        private Shader shader;

        private Quad panel, border, sheen, logo, power, powerIcon, lineA, lineB, catPrev, catNext, catMid, catDot, pagePrev, pageNext;
        private Label title, subtitle, catName, catCount, arrowCL, arrowCR, arrowPL, arrowPR, pageText, footer;
        private readonly List<Row> rows = new List<Row>();
        private GameObject tip;
        private Material tipMat;

        private bool open, lastY;
        private float openT, openedAt, nextRepaint, cooldownUntil;
        private int tab = 3, page;
        private readonly int[] tabPage = new int[64];
        private int hovered = -1, lastHovered = -2;
        private readonly bool[] wasInside = new bool[IdCount];
        private int popId = -1;
        private float popT;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (tip != null) Destroy(tip);
        }

        private void Update()
        {
            bool ready = Enabled && Util.Ready && Util.LeftHand != null && Util.RightHand != null && Util.Head != null;
            bool y = Util.Y;
            if (ready && y && !lastY && !MenuGUI.IsTyping)
            {
                open = !open;
                if (open)
                {
                    openedAt = Time.unscaledTime;
                    nextRepaint = 0f;
                    for (int i = 0; i < wasInside.Length; i++) wasInside[i] = true;
                }
                if (MenuGUI.SoundsOn && RivenAudio.Instance != null) RivenAudio.Instance.PlayClick();
            }
            lastY = y;
            if (!ready) open = false;

            float speed = MenuGUI.AnimSpeed <= 0f ? 1000f : MenuGUI.AnimSpeed;
            openT = Mathf.MoveTowards(openT, open ? 1f : 0f, Time.unscaledDeltaTime * 6f * speed);
            bool show = ready && openT > 0.001f;

            if (root == null)
            {
                if (!show) return;
                Build();
            }

            if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
            bool showTip = show && open;
            if (tip.activeSelf != showTip) tip.SetActive(showTip);
            if (!show) return;

            Place();
            Interact();

            if (hovered != lastHovered || Time.unscaledTime >= nextRepaint)
            {
                lastHovered = hovered;
                nextRepaint = Time.unscaledTime + 0.3f;
                Paint();
            }
            AnimatePop();
            if (Theme.Current.Rainbow) border.SetColor(Theme.WithA(Theme.Accent, 0.6f));
        }

        private void Place()
        {
            Vector3 head = Util.Head.position;
            float half = H / 2f * PanelMeters * Size / W;
            Vector3 pos = Util.LeftHand.position + Vector3.up * (Height * Size + half);
            Vector3 look = pos - head;
            look.y *= 0.6f;
            if (look.sqrMagnitude < 0.0001f) look = Util.Head.forward;
            root.SetPositionAndRotation(pos, Quaternion.LookRotation(look.normalized, Vector3.up));
            float s = Mathf.LerpUnclamped(0.75f, 1f, EaseOutBack(openT));
            root.localScale = new Vector3(s, s, s);
        }

        private Vector3 Fingertip => Util.RightHand.TransformPoint(0f, -0.1f, 0f);

        private float UnitsToMeters => root.localScale.x * PanelMeters * Size / W;

        private void Interact()
        {
            hovered = -1;
            Vector3 finger = Fingertip;
            tip.transform.position = finger;
            float r = 0.012f * Size;
            tip.transform.localScale = new Vector3(r, r, r);

            Vector3 lp = Quaternion.Inverse(root.rotation) * (finger - root.position);
            float k = UnitsToMeters;
            Vector2 p = new Vector2(lp.x / k + W / 2f, H / 2f - lp.y / k);
            float z = lp.z;
            bool near = z > -0.08f && z < 0.06f;
            bool touching = z > -0.012f && z < 0.06f;
            bool canPress = open && Time.unscaledTime - openedAt > 0.35f;

            int count = ItemCount(out int pages);
            for (int id = 0; id < IdCount; id++)
            {
                Rect rect = ElementRect(id, count);
                if (rect.width <= 0f) { wasInside[id] = false; continue; }
                bool inside = rect.Contains(p);
                if (inside && near) hovered = id;
                bool pressNow = inside && touching;
                if (pressNow && !wasInside[id] && canPress && Time.unscaledTime >= cooldownUntil)
                {
                    cooldownUntil = Time.unscaledTime + 0.2f;
                    Press(id, p, rect, count, pages);
                    count = ItemCount(out pages);
                }
                wasInside[id] = pressNow;
            }

            tipMat.color = hovered >= 0 ? Theme.Lighten(Theme.Accent, 0.55f) : Theme.WithA(Theme.Accent, 0.85f);
        }

        private List<ButtonInfo> Items
        {
            get
            {
                tab = Mathf.Clamp(tab, 0, Menu.Tabs.Count - 1);
                return Menu.Tabs[tab].Items;
            }
        }

        private int ItemCount(out int pages)
        {
            int count = Items.Count;
            pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            return count;
        }

        private static Rect RowRect(int i) => new Rect(Pad, RowsY + i * (RowH + Gap), RowW, RowH);
        private static Rect OptionRect(Rect r) => new Rect(r.xMax - 10f - 118f, r.center.y - 14f, 118f, 28f);

        private Rect ElementRect(int id, int count)
        {
            switch (id)
            {
                case IdPower: return new Rect(W - Pad - HeadH, HeadY, HeadH, HeadH);
                case IdCatPrev: return new Rect(Pad, CatY, ArrowW, CatH);
                case IdCatNext: return new Rect(W - Pad - ArrowW, CatY, ArrowW, CatH);
                case IdPagePrev: return new Rect(Pad, PagerY, 96f, PagerH);
                case IdPageNext: return new Rect(W - Pad - 96f, PagerY, 96f, PagerH);
                default:
                    int i = id - IdRow0;
                    if (page * PerPage + i >= count) return new Rect();
                    return RowRect(i);
            }
        }

        private void SwitchTab(int dir)
        {
            int n = Menu.Tabs.Count;
            if (tab < tabPage.Length) tabPage[tab] = page;
            tab = ((tab + dir) % n + n) % n;
            page = tab < tabPage.Length ? tabPage[tab] : 0;
        }

        private void Press(int id, Vector2 p, Rect rect, int count, int pages)
        {
            popId = id;
            popT = 1f;
            if (Util.Tagger != null) Util.Tagger.StartVibration(false, 0.3f, 0.04f);

            switch (id)
            {
                case IdPower:
                    Buttons.DisableAll();
                    Notify.Push("All mods off");
                    Click();
                    break;
                case IdCatPrev: SwitchTab(-1); Click(); break;
                case IdCatNext: SwitchTab(1); Click(); break;
                case IdPagePrev:
                    if (page > 0) page--;
                    else page = pages - 1;
                    Click();
                    break;
                case IdPageNext:
                    if (page < pages - 1) page++;
                    else page = 0;
                    Click();
                    break;
                default:
                    int idx = page * PerPage + (id - IdRow0);
                    if (idx >= count) break;
                    ButtonInfo btn = Items[idx];
                    bool back = btn.Type == ButtonType.Option && p.x < OptionRect(rect).center.x;
                    Buttons.Press(btn, back);
                    if (btn.Type == ButtonType.Toggle && MenuGUI.SoundsOn && RivenAudio.Instance != null)
                    {
                        if (btn.Enabled) RivenAudio.Instance.PlayOn();
                        else RivenAudio.Instance.PlayOff();
                    }
                    else Click();
                    break;
            }
            nextRepaint = 0f;
            lastHovered = -2;
        }

        private static void Click()
        {
            if (MenuGUI.SoundsOn && RivenAudio.Instance != null) RivenAudio.Instance.PlayClick();
        }

        private void AnimatePop()
        {
            if (popId < 0) return;
            popT = Mathf.MoveTowards(popT, 0f, Time.unscaledDeltaTime * 6f);
            Quad q = QuadFor(popId);
            if (q != null)
            {
                float s = 1f - 0.07f * Mathf.Sin(popT * Mathf.PI);
                q.T.localScale = new Vector3(q.BaseScale.x * s, q.BaseScale.y * s, 1f);
            }
            if (popT <= 0f) popId = -1;
        }

        private Quad QuadFor(int id)
        {
            switch (id)
            {
                case IdPower: return power;
                case IdCatPrev: return catPrev;
                case IdCatNext: return catNext;
                case IdPagePrev: return pagePrev;
                case IdPageNext: return pageNext;
                default:
                    int i = id - IdRow0;
                    return i >= 0 && i < rows.Count ? rows[i].Bg : null;
            }
        }

        private Color CatColor(int i)
        {
            int n = Mathf.Max(1, Menu.Tabs.Count - 1);
            return Color.Lerp(Theme.Accent, Theme.Accent2, i / (float)n);
        }

        private static int TabCount(Tab t)
        {
            int on = 0;
            List<ButtonInfo> items = t.Items;
            for (int j = 0; j < items.Count; j++) if (items[j].IsActive) on++;
            return on;
        }

        private void Paint()
        {
            MenuTheme th = Theme.Current;
            Color acc = Theme.Accent, text = th.Text;
            Color dim = Theme.WithA(Color.Lerp(text, acc, 0.25f), 0.6f);
            Color card = Theme.WithA(Color.Lerp(th.Panel, th.Row, 0.6f), 0.95f);
            Color cardHover = Theme.WithA(Color.Lerp(Color.Lerp(th.Panel, th.Row, 0.85f), acc, 0.18f), 0.97f);
            Color cat = CatColor(tab);

            panel.SetColor(Theme.WithA(th.Panel, Mathf.Max(0.6f, Theme.Opacity)));
            border.SetColor(Theme.WithA(Theme.Lighten(acc, 0.25f), 0.45f));
            sheen.SetColor(Theme.WithA(acc, 0.1f));
            lineA.SetColor(Theme.WithA(acc, 0.7f));
            lineB.SetColor(Theme.WithA(Theme.Accent2, 0.9f));
            power.SetColor(hovered == IdPower ? cardHover : card);
            powerIcon.SetColor(Buttons.AnyActive ? Theme.Lighten(acc, 0.45f) : Theme.WithA(text, 0.45f));
            title.Set("Riven", text);
            subtitle.Set("<color=#" + ColorUtility.ToHtmlStringRGB(acc) + ">MENU</color>  v" + Menu.Version, dim);

            int count = ItemCount(out int pages);
            Tab t = Menu.Tabs[tab];
            int on = TabCount(t);
            catPrev.SetColor(hovered == IdCatPrev ? cardHover : card);
            catNext.SetColor(hovered == IdCatNext ? cardHover : card);
            catMid.SetColor(Theme.WithA(Color.Lerp(th.Row, cat, 0.28f), 0.95f));
            catDot.SetColor(cat);
            arrowCL.Set("<", text);
            arrowCR.Set(">", text);
            catName.Set(Fit(t.Name, 16), text);
            catCount.Set(on > 0 ? on + " ON" : (tab + 1) + " / " + Menu.Tabs.Count, on > 0 ? Theme.Lighten(cat, 0.3f) : dim);

            List<ButtonInfo> items = Items;
            string tipText = "";
            for (int i = 0; i < PerPage; i++)
            {
                Row row = rows[i];
                int idx = page * PerPage + i;
                bool exists = idx < count;
                if (row.Holder.gameObject.activeSelf != exists) row.Holder.gameObject.SetActive(exists);
                if (!exists) continue;

                ButtonInfo b = items[idx];
                bool hov = hovered == IdRow0 + i;
                bool active = b.IsActive;
                Color bc = CatColor(CategoryIndex(b.Category));
                row.Bg.SetColor(active ? Theme.WithA(Color.Lerp(th.Row, bc, 0.4f), 0.97f) : hov ? cardHover : card);
                row.Bar.SetColor(Theme.WithA(bc, active ? 1f : hov ? 0.7f : 0.3f));
                row.Glow.Show(active);
                if (active) row.Glow.SetColor(Theme.WithA(bc, 0.22f));

                int nameMax = b.Type == ButtonType.Option ? 15 : 22;
                row.Name.Set(Fit(b.Name, nameMax), text);
                row.Desc.Set(Fit(b.Tooltip, b.Type == ButtonType.Option ? 24 : 36), Theme.WithA(dim, hov ? 0.85f : 0.65f));
                if (hov) tipText = b.Tooltip;

                if (b.Type == ButtonType.Toggle) Mode(row, 0, "", text, hov, active, bc);
                else if (b.Type == ButtonType.Option) Mode(row, 2, "< " + Fit(b.Current, 9) + " >", text, hov, false, bc);
                else Mode(row, 1, "RUN", text, hov, false, bc);
            }

            pagePrev.SetColor(hovered == IdPagePrev ? cardHover : card);
            pageNext.SetColor(hovered == IdPageNext ? cardHover : card);
            arrowPL.Set("<", Theme.WithA(text, pages > 1 ? 0.92f : 0.3f));
            arrowPR.Set(">", Theme.WithA(text, pages > 1 ? 0.92f : 0.3f));
            pageText.Set("PAGE " + (page + 1) + " / " + pages, dim);

            string foot = !string.IsNullOrEmpty(tipText) ? tipText : "Y hides   -   tap with your right hand";
            footer.Set(Fit(foot, 52), dim);
        }

        private int CategoryIndex(string cat)
        {
            for (int i = 0; i < Menu.Tabs.Count; i++)
                if (string.Equals(Menu.Tabs[i].Name, cat, System.StringComparison.OrdinalIgnoreCase)) return i;
            return tab;
        }

        private void Mode(Row row, int kind, string chipLabel, Color text, bool hov, bool on, Color cat)
        {
            row.Track.Show(kind == 0);
            row.Knob.Show(kind == 0);
            row.Chip.Show(kind == 1);
            row.WideChip.Show(kind == 2);
            row.ChipText.Show(kind != 0);

            if (kind == 0)
            {
                row.Track.SetColor(on ? cat : new Color(0f, 0f, 0f, 0.45f));
                row.Knob.SetColor(Color.white);
                float trackX = RowW / 2f - 12f - 20f;
                row.Knob.T.localPosition = new Vector3(trackX + (on ? 10f : -10f), 0f, -3f);
                return;
            }

            Quad chip = kind == 1 ? row.Chip : row.WideChip;
            chip.SetColor(hov ? Theme.WithA(cat, 0.75f) : new Color(0f, 0f, 0f, 0.42f));
            float w = kind == 1 ? 56f : 118f;
            row.ChipText.Tm.transform.localPosition = new Vector3(RowW / 2f - 10f - w / 2f, 0f, -4f);
            row.ChipText.Set(chipLabel, text);
        }

        private static string Fit(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "...";
        }

        private static Vector3 P(float ux, float uy, float z) => new Vector3(ux - W / 2f, H / 2f - uy, z);

        private static Shader ColorShader()
        {
            foreach (string name in new[] { "Sprites/Default", "Unlit/Transparent", "UI/Default" })
            {
                Shader s = Shader.Find(name);
                if (s != null) return s;
            }
            return Shader.Find("GUI/Text Shader");
        }

        private void Build()
        {
            shader = Shader.Find("GUI/Text Shader");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMat = new Material(font.material) { renderQueue = 3110 };

            GameObject r = new GameObject("RivenWristMenu");
            DontDestroyOnLoad(r);
            root = r.transform;

            GameObject u = new GameObject("ui");
            u.transform.SetParent(root, false);
            ui = u.transform;
            float k = PanelMeters * Size / W;
            ui.localScale = new Vector3(k, k, k);

            panel = RectQuad("panel", new Rect(0f, 0f, W, H), 22f, 0f, 0);
            border = RectQuad("border", new Rect(0f, 0f, W, H), 22f, 2f, 1);
            sheen = TexQuad("sheen", new Rect(10f, 0f, W - 20f, 140f), TopFade(), 1);

            Rect logoRect = new Rect(Pad + 4f, HeadY + 6f, 40f, 40f);
            logo = TexQuad("logo", logoRect, Logo.Texture != null ? (Texture)Logo.Texture : Tex.Diamond, 4);
            if (Logo.Texture != null)
            {
                logo.Mat.shader = ColorShader();
                logo.Mat.mainTexture = Logo.Texture;
                logo.Mat.color = Color.white;
                logo.Mat.renderQueue = 3104;
            }
            title = Text(ui, "title", P(Pad + 54f, HeadY + 18f, -6f), 24f, TextAnchor.MiddleLeft);
            subtitle = Text(ui, "subtitle", P(Pad + 55f, HeadY + 40f, -6f), 10f, TextAnchor.MiddleLeft);
            power = RectQuad("power", new Rect(W - Pad - HeadH, HeadY, HeadH, HeadH), 14f, 0f, 2);
            powerIcon = TexQuad("powerIcon", new Rect(W - Pad - HeadH / 2f - 12f, HeadY + HeadH / 2f - 12f, 24f, 24f), Tex.Power, 4);
            lineA = TexQuad("lineA", new Rect(Pad, HeadY + HeadH + 6f, RowW, 2f), Texture2D.whiteTexture, 2);
            lineB = TexQuad("lineB", new Rect(Pad, HeadY + HeadH + 6f, RowW, 2f), Tex.FadeRight, 3);

            catPrev = RectQuad("catPrev", new Rect(Pad, CatY, ArrowW, CatH), 14f, 0f, 2);
            catNext = RectQuad("catNext", new Rect(W - Pad - ArrowW, CatY, ArrowW, CatH), 14f, 0f, 2);
            catMid = RectQuad("catMid", new Rect(Pad + ArrowW + 6f, CatY, RowW - ArrowW * 2f - 12f, CatH), 14f, 0f, 2);
            catDot = TexQuad("catDot", new Rect(Pad + ArrowW + 20f, CatY + CatH / 2f - 5f, 10f, 10f), Tex.Circle, 4);
            arrowCL = Text(ui, "arrowCL", P(Pad + ArrowW / 2f, CatY + CatH / 2f, -6f), 24f, TextAnchor.MiddleCenter);
            arrowCR = Text(ui, "arrowCR", P(W - Pad - ArrowW / 2f, CatY + CatH / 2f, -6f), 24f, TextAnchor.MiddleCenter);
            catName = Text(ui, "catName", P(Pad + ArrowW + 38f, CatY + CatH / 2f - 1f, -6f), 16f, TextAnchor.MiddleLeft);
            catCount = Text(ui, "catCount", P(W - Pad - ArrowW - 16f, CatY + CatH / 2f, -6f), 10.5f, TextAnchor.MiddleRight);

            for (int i = 0; i < PerPage; i++)
            {
                Rect rr = RowRect(i);
                Row row = new Row();
                GameObject h = new GameObject("row" + i);
                h.transform.SetParent(ui, false);
                h.transform.localPosition = P(rr.center.x, rr.center.y, 0f);
                row.Holder = h.transform;

                row.Bg = LocalQuad(row.Holder, "bg", Vector3.zero, new Vector2(RowW, RowH), Tex.RoundedRect(Mathf.RoundToInt(RowW * 2f), Mathf.RoundToInt(RowH * 2f), 24f, 0f), 2, -1f);
                row.Glow = LocalQuad(row.Holder, "glow", new Vector3(-RowW * 0.2f, 0f, 0f), new Vector2(RowW * 0.6f, RowH), LeftFade(), 3, -1.5f);
                row.Bar = LocalQuad(row.Holder, "bar", new Vector3(-RowW / 2f + 8f, 0f, 0f), new Vector2(3.5f, RowH - 20f), Tex.Pill(8, 60), 3, -2f);
                row.Chip = LocalQuad(row.Holder, "chip", new Vector3(RowW / 2f - 10f - 28f, 0f, 0f), new Vector2(56f, 28f), Tex.RoundedRect(112, 56, 16f, 0f), 3, -2f);
                row.WideChip = LocalQuad(row.Holder, "wide", OptionCenter(), new Vector2(118f, 28f), Tex.RoundedRect(236, 56, 16f, 0f), 3, -2f);
                row.Track = LocalQuad(row.Holder, "track", new Vector3(RowW / 2f - 12f - 20f, 0f, 0f), new Vector2(40f, 22f), Tex.Pill(80, 44), 3, -2f);
                row.Knob = LocalQuad(row.Holder, "knob", Vector3.zero, new Vector2(17f, 17f), Tex.Circle, 4, -3f);

                row.Name = Text(row.Holder, "name", new Vector3(-RowW / 2f + 20f, 7f, -5f), 14f, TextAnchor.MiddleLeft);
                row.Desc = Text(row.Holder, "desc", new Vector3(-RowW / 2f + 20f, -11f, -5f), 9f, TextAnchor.MiddleLeft);
                row.ChipText = Text(row.Holder, "chipText", Vector3.zero, 11f, TextAnchor.MiddleCenter);
                rows.Add(row);
            }

            pagePrev = RectQuad("pagePrev", new Rect(Pad, PagerY, 96f, PagerH), 14f, 0f, 2);
            pageNext = RectQuad("pageNext", new Rect(W - Pad - 96f, PagerY, 96f, PagerH), 14f, 0f, 2);
            arrowPL = Text(ui, "arrowPL", P(Pad + 48f, PagerY + PagerH / 2f, -6f), 22f, TextAnchor.MiddleCenter);
            arrowPR = Text(ui, "arrowPR", P(W - Pad - 48f, PagerY + PagerH / 2f, -6f), 22f, TextAnchor.MiddleCenter);
            pageText = Text(ui, "pageText", P(W / 2f, PagerY + PagerH / 2f, -6f), 11f, TextAnchor.MiddleCenter);
            footer = Text(ui, "footer", P(W / 2f, PagerY + PagerH + 18f, -6f), 9.5f, TextAnchor.MiddleCenter);

            tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(tip.GetComponent<Collider>());
            tip.name = "RivenFingertip";
            DontDestroyOnLoad(tip);
            tipMat = new Material(shader) { renderQueue = 3120, color = Theme.Accent };
            Renderer tr = tip.GetComponent<Renderer>();
            tr.material = tipMat;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tip.SetActive(false);
        }

        private static Vector3 OptionCenter() => new Vector3(RowW / 2f - 10f - 59f, 0f, 0f);

        private static Texture2D topFade, leftFade;

        private static Texture2D TopFade()
        {
            if (topFade != null) return topFade;
            topFade = new Texture2D(2, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[128];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 2; x++) px[y * 2 + x] = new Color(1f, 1f, 1f, y / 63f);
            topFade.SetPixels(px);
            topFade.Apply();
            return topFade;
        }

        private static Texture2D LeftFade()
        {
            if (leftFade != null) return leftFade;
            leftFade = new Texture2D(64, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[128];
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < 64; x++) px[y * 64 + x] = new Color(1f, 1f, 1f, 1f - x / 63f);
            leftFade.SetPixels(px);
            leftFade.Apply();
            return leftFade;
        }

        private Quad RectQuad(string name, Rect r, float radius, float borderWidth, int layer)
        {
            Texture2D tex = Tex.RoundedRect(Mathf.RoundToInt(r.width * 2f), Mathf.RoundToInt(r.height * 2f), radius * 2f, borderWidth * 2f);
            return TexQuad(name, r, tex, layer);
        }

        private Quad TexQuad(string name, Rect r, Texture tex, int layer) =>
            LocalQuad(ui, name, P(r.x + r.width / 2f, r.y + r.height / 2f, 0f), new Vector2(r.width, r.height), tex, layer, -layer);

        private Quad LocalQuad(Transform parent, string name, Vector3 pos, Vector2 size, Texture tex, int layer, float z)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, z);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            Material m = new Material(shader) { mainTexture = tex, renderQueue = 3100 + layer };
            Renderer rend = go.GetComponent<Renderer>();
            rend.material = m;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return new Quad { T = go.transform, Mat = m, BaseScale = go.transform.localScale };
        }

        private Label Text(Transform parent, string name, Vector3 pos, float size, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = 64;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleRight ? TextAlignment.Right : anchor == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
            tm.richText = true;
            tm.characterSize = size / 6.4f;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.material = textMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return new Label { Tm = tm };
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
