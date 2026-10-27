using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RivenMenu
{
    public class WristMenu : MonoBehaviour
    {
        public static WristMenu Instance;
        public static bool Enabled = true;

        private const int PerPage = 6;
        private const float Width = 300f, RowH = 50f, RowGap = 4f, HeadH = 64f, FootH = 44f, Pad = 12f;
        private const float Scale = 0.00075f;

        private float Height => HeadH + PerPage * (RowH + RowGap) + FootH + Pad;

        private Canvas canvas;
        private RectTransform root;
        private Font font;

        private Image panel, glow, headBar, markL, markR, star, selBar;
        private Text headText, footText;
        private readonly List<Row> rows = new List<Row>();

        private bool open, lastY, lastClick;
        private float openT, navCooldown;
        private int tab = -1, page, sel;

        private class Row
        {
            public Image Bg;
            public Image RiftL, RiftR, Star;
            public Image Chip;
            public Text Num, Name, Value;
            public bool Active;
            public bool On;
        }

        private void Awake() => Instance = this;

        private void Update()
        {
            if (!Enabled)
            {
                if (canvas != null) canvas.enabled = false;
                return;
            }

            bool y = Util.Y;
            if (y && !lastY && !MenuGUI.IsTyping) open = !open;
            lastY = y;

            openT = Mathf.MoveTowards(openT, open ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            bool show = openT > 0.001f && Util.Ready && Util.LeftHand != null;

            if (canvas == null)
            {
                if (!show) return;
                Build();
            }
            canvas.enabled = show;
            if (!show) return;

            if (open) Navigate();
            Anchor();
            Paint();
        }

        private void Navigate()
        {
            Vector2 stick = Util.LeftStick;
            bool ready = Time.unscaledTime >= navCooldown;
            var items = Current(out int count, out int pages);

            if (ready && Mathf.Abs(stick.y) > 0.6f)
            {
                sel += stick.y < 0f ? 1 : -1;
                navCooldown = Time.unscaledTime + 0.18f;
                Wrap(count);
            }
            if (ready && stick.x < -0.6f)
            {
                Back();
                navCooldown = Time.unscaledTime + 0.22f;
            }
            if (ready && stick.x > 0.6f && pages > 1)
            {
                page = (page + 1) % pages;
                sel = page * PerPage;
                navCooldown = Time.unscaledTime + 0.22f;
            }

            bool click = Util.LeftTrigger;
            if (click && !lastClick) Select(items, count);
            lastClick = click;
        }

        private void Wrap(int count)
        {
            if (count == 0) return;
            sel = (sel % count + count) % count;
            page = sel / PerPage;
        }

        private void Back()
        {
            if (tab < 0) { if (page > 0) page--; }
            else { tab = -1; sel = 0; page = 0; }
            Click();
        }

        private void Select(List<ButtonInfo> items, int count)
        {
            if (sel >= count) return;
            if (tab < 0)
            {
                tab = sel;
                sel = 0;
                page = 0;
            }
            else
            {
                Buttons.Press(items[sel]);
            }
            Click();
            if (Util.Tagger != null) Util.Tagger.StartVibration(false, 0.3f, 0.05f);
        }

        private void Click()
        {
            if (MenuGUI.SoundsOn && RivenAudio.Instance != null) RivenAudio.Instance.PlayClick();
        }

        private List<ButtonInfo> Current(out int count, out int pages)
        {
            List<ButtonInfo> items = tab < 0 ? null : Menu.Tabs[tab].Items;
            count = tab < 0 ? Menu.Tabs.Count : items.Count;
            pages = Mathf.Max(1, Mathf.CeilToInt(count / (float)PerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            sel = Mathf.Clamp(sel, 0, Mathf.Max(0, count - 1));
            return items;
        }

        private void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject go = new GameObject("RivenWristCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root = canvas.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(Width, Height);
            root.pivot = new Vector2(0.5f, 0.5f);

            glow = Img(root, "Glow", Sprites.Soft);
            Stretch(glow.rectTransform, -28f);
            panel = Img(root, "Panel", Sprites.Rounded);
            Stretch(panel.rectTransform, 0f);

            headBar = Img(root, "HeadBar", Sprites.Rounded);
            Place(headBar.rectTransform, Pad, Pad, Width - Pad * 2f, HeadH - Pad);

            markL = Img(root, "RiftL", null); markL.sprite = SpriteFrom(Tex.RiftLeft);
            markR = Img(root, "RiftR", null); markR.sprite = SpriteFrom(Tex.RiftRight);
            star = Img(root, "Star", null); star.sprite = SpriteFrom(Tex.Star);
            SetRect(markL.rectTransform, Pad + 26f, HeadH / 2f + 2f, 30f, 30f);
            SetRect(markR.rectTransform, Pad + 26f, HeadH / 2f + 2f, 30f, 30f);
            SetRect(star.rectTransform, Pad + 26f, HeadH / 2f + 2f, 13f, 13f);

            headText = Txt(root, 22, TextAnchor.MiddleLeft);
            Place(headText.rectTransform, Pad + 50f, Pad, Width - Pad * 2f - 50f, HeadH - Pad);

            selBar = Img(root, "Sel", Sprites.Rounded);

            for (int i = 0; i < PerPage; i++)
            {
                float y = HeadH + i * (RowH + RowGap);
                Row row = new Row();
                row.Bg = Img(root, "Row", Sprites.Rounded);
                Place(row.Bg.rectTransform, Pad, y, Width - Pad * 2f, RowH);
                row.Num = Txt(row.Bg.transform, 13, TextAnchor.MiddleLeft);
                Place(row.Num.rectTransform, 12f, 0f, 26f, RowH);
                row.Name = Txt(row.Bg.transform, 15, TextAnchor.MiddleLeft);
                Place(row.Name.rectTransform, 38f, 0f, Width - Pad * 2f - 110f, RowH);
                row.Chip = Img(row.Bg.transform, "Chip", Sprites.Rounded);
                row.Value = Txt(row.Chip.transform, 13, TextAnchor.MiddleCenter);
                Stretch(row.Value.rectTransform, 0f);
                row.RiftL = Img(row.Bg.transform, "rl", null); row.RiftL.sprite = SpriteFrom(Tex.RiftLeft);
                row.RiftR = Img(row.Bg.transform, "rr", null); row.RiftR.sprite = SpriteFrom(Tex.RiftRight);
                row.Star = Img(row.Bg.transform, "rs", null); row.Star.sprite = SpriteFrom(Tex.Star);
                rows.Add(row);
            }

            footText = Txt(root, 13, TextAnchor.MiddleCenter);
            Place(footText.rectTransform, Pad, Height - FootH, Width - Pad * 2f, FootH - Pad);
        }

        private void Anchor()
        {
            Transform hand = Util.LeftHand;
            Camera cam = Camera.main;
            Vector3 pos = hand.position + hand.up * 0.03f - hand.forward * 0.02f;
            root.position = pos;

            Vector3 faceFrom = cam != null ? cam.transform.position : pos + Vector3.back;
            Vector3 normal = (pos - faceFrom).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, normal);
            if (right.sqrMagnitude < 0.001f) right = hand.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(normal, right);
            root.rotation = Quaternion.LookRotation(normal, up);

            float s = Scale * Mathf.Lerp(0.82f, 1f, EaseOutBack(openT));
            root.localScale = new Vector3(s, s, s);
            SetLayer(root.gameObject, Util.VisualLayer);
        }

        private void Paint()
        {
            MenuTheme th = Theme.Current;
            Color acc = Theme.Accent, acc2 = Theme.Accent2, text = th.Text;
            float a = Mathf.Clamp01(openT);
            float t = Time.unscaledTime;

            panel.color = Fade(Theme.WithA(th.Panel, 0.97f), a);
            glow.color = Fade(Theme.WithA(acc, 0.22f + 0.1f * Mathf.Sin(t * 2.2f)), a);
            headBar.color = Fade(Theme.WithA(Theme.Lighten(th.Row, 0.12f), 0.9f), a);

            float split = 2f + Mathf.Sin(t * 2.4f) * 1.2f;
            PaintMark(markL, markR, star, acc, acc2, split, a, true);
            headText.color = Fade(text, a);
            headText.text = tab < 0 ? Menu.Name : Menu.Tabs[tab].Name.ToLowerInvariant();

            List<ButtonInfo> items = Current(out int count, out int pages);

            int selRow = sel - page * PerPage;
            if (selRow >= 0 && selRow < PerPage)
            {
                selBar.enabled = true;
                float y = HeadH + selRow * (RowH + RowGap);
                Place(selBar.rectTransform, Pad, y, Width - Pad * 2f, RowH);
                selBar.color = Fade(Theme.WithA(Color.Lerp(th.Row, acc, 0.5f), 0.95f), a);
                selBar.transform.SetSiblingIndex(rows[0].Bg.transform.GetSiblingIndex());
            }
            else selBar.enabled = false;

            for (int i = 0; i < PerPage; i++)
            {
                int idx = page * PerPage + i;
                Row row = rows[i];
                if (idx >= count)
                {
                    SetRow(row, false);
                    continue;
                }
                SetRow(row, true);
                bool selected = idx == sel;
                row.Num.text = (idx + 1).ToString("00");
                row.Num.color = Fade(Theme.WithA(text, 0.35f), a);
                row.Name.color = Fade(text, a);

                if (tab < 0)
                {
                    Tab tb = Menu.Tabs[idx];
                    int on = 0;
                    foreach (ButtonInfo b in tb.Items) if (b.IsActive) on++;
                    row.Name.text = tb.Name;
                    row.Bg.color = Fade(selected ? Theme.WithA(Color.Lerp(th.Row, acc, 0.3f), 0.5f) : Theme.WithA(th.Row, 0.55f), a);
                    ShowChip(row, on > 0, on > 0 ? on + " on" : "", acc, text, a);
                    ShowSwitch(row, false, acc, acc2, a, split);
                }
                else
                {
                    ButtonInfo b = items[idx];
                    row.Name.text = b.Name;
                    row.Bg.color = Fade(b.IsActive ? Theme.WithA(Color.Lerp(th.Row, acc, 0.42f), 0.85f) : Theme.WithA(th.Row, 0.55f), a);
                    if (b.Type == ButtonType.Toggle)
                    {
                        ShowChip(row, false, "", acc, text, a);
                        ShowSwitch(row, true, acc, acc2, a, split, b.Enabled);
                    }
                    else if (b.Type == ButtonType.Option)
                    {
                        ShowChip(row, true, "< " + b.Current + " >", acc, text, a);
                        ShowSwitch(row, false, acc, acc2, a, split);
                    }
                    else
                    {
                        ShowChip(row, true, "run", acc, text, a);
                        ShowSwitch(row, false, acc, acc2, a, split);
                    }
                }
            }

            footText.color = Fade(Theme.WithA(text, 0.55f), a);
            footText.text = tab < 0 ? "stick: move   trigger: open   Y: close"
                : (pages > 1 ? "stick: move/page   left: back   trigger: use" : "stick: move   left: back   trigger: use");
        }

        private void ShowChip(Row row, bool show, string value, Color acc, Color text, float a)
        {
            row.Chip.enabled = show;
            row.Value.enabled = show;
            if (!show) return;
            float w = Mathf.Clamp(value.Length * 8f + 20f, 40f, 110f);
            SetRect(row.Chip.rectTransform, Width - Pad * 2f - 14f - w / 2f, RowH / 2f, w, 24f);
            row.Chip.color = Fade(new Color(0f, 0f, 0f, 0.4f), a);
            row.Value.text = value;
            row.Value.color = Fade(text, a);
        }

        private void ShowSwitch(Row row, bool show, Color acc, Color acc2, float a, float split, bool on = false)
        {
            row.RiftL.enabled = show;
            row.RiftR.enabled = show;
            row.Star.enabled = show && on;
            if (!show) return;
            float cx = Width - Pad * 2f - 30f, cy = RowH / 2f, size = 26f;
            float gap = on ? split : 0.3f;
            Vector2 off = Tex.RiftAxis * gap;
            SetRect(row.RiftL.rectTransform, cx - off.x, cy + off.y, size, size);
            SetRect(row.RiftR.rectTransform, cx + off.x, cy - off.y, size, size);
            SetRect(row.Star.rectTransform, cx, cy, 10f, 10f);
            Color dull = Theme.WithA(Theme.Current.Text, 0.25f);
            row.RiftL.color = Fade(on ? acc : dull, a);
            row.RiftR.color = Fade(on ? acc2 : dull, a);
            row.Star.color = Fade(Color.white, a);
        }

        private void PaintMark(Image l, Image r, Image s, Color acc, Color acc2, float split, float a, bool star)
        {
            Vector2 off = Tex.RiftAxis * split;
            Vector2 c = new Vector2(Pad + 26f, HeadH / 2f + 2f);
            SetRect(l.rectTransform, c.x - off.x, c.y - off.y, 30f, 30f);
            SetRect(r.rectTransform, c.x + off.x, c.y + off.y, 30f, 30f);
            SetRect(s.rectTransform, c.x, c.y, 13f, 13f);
            l.color = Fade(acc, a);
            r.color = Fade(acc2, a);
            s.color = Fade(Color.white, star ? a : 0f);
        }

        private static void SetRow(Row row, bool active)
        {
            if (row.Active == active && !active) return;
            row.Active = active;
            row.Bg.gameObject.SetActive(active);
        }

        private Image Img(Transform parent, string name, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            if (sprite == Sprites.Rounded) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        private Text Txt(Transform parent, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Sprite SpriteFrom(Texture2D tex) =>
            Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

        private void Stretch(RectTransform rt, float margin)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(-margin, -margin);
            rt.offsetMax = new Vector2(margin, margin);
        }

        private void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private void SetRect(RectTransform rt, float cx, float cyFromTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, -cyFromTop);
        }

        private static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
        }

        private static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
