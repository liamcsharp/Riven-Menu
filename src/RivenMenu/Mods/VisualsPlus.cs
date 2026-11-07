using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class VisualsPlus
    {
        private const string Cat = "Visuals";

        private static Camera Cam => GunLib.DesktopCamera != null ? GunLib.DesktopCamera : Camera.main;

        private static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }

        private static void Dot(Vector2 center, float size, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), Tex.Circle);
        }

        private static void DrawRadar()
        {
            if (Event.current.type != EventType.Repaint || Util.Head == null) return;
            const float size = 180f, range = 30f;
            Rect box = new Rect(Screen.width - size - 20f, 20f, size, size);
            Vector2 c = box.center;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(box, Tex.Circle);
            GUI.color = Theme.WithA(Theme.Accent, 0.35f);
            GUI.DrawTexture(new Rect(c.x - size * 0.25f, c.y - size * 0.25f, size * 0.5f, size * 0.5f), Tex.DiamondOutline);
            Fill(new Rect(c.x - 0.5f, box.y + 10f, 1f, size - 20f), Theme.WithA(Theme.Accent, 0.25f));
            Fill(new Rect(box.x + 10f, c.y - 0.5f, size - 20f, 1f), Theme.WithA(Theme.Accent, 0.25f));

            Vector3 f = Util.Head.forward;
            f.y = 0f;
            Quaternion inv = Quaternion.Inverse(f.sqrMagnitude < 0.001f ? Quaternion.identity : Quaternion.LookRotation(f.normalized));
            Vector3 me = Util.Head.position;
            foreach (VRRig r in Util.OtherRigs)
            {
                if (r == null) continue;
                Vector3 d = inv * (r.transform.position - me);
                Vector2 p = new Vector2(d.x, d.z) / range * (size / 2f - 10f);
                if (p.magnitude > size / 2f - 10f) p = p.normalized * (size / 2f - 10f);
                Color col = Util.PlayerColor(r);
                col.a = 1f;
                Dot(new Vector2(c.x + p.x, c.y - p.y), Util.IsTagged(r) ? 12f : 9f, col);
            }
            Dot(c, 10f, Color.white);
            GUI.color = Color.white;
        }

        [MenuButton("Radar", Category = Cat, Order = 40, OnEnable = nameof(RadarOn), OnDisable = nameof(RadarOff), Tooltip = "A minimap in the corner of your PC screen with every player")]
        public static void Radar() { }
        public static void RadarOn() => Hud.Overlay += DrawRadar;
        public static void RadarOff() => Hud.Overlay -= DrawRadar;

        private static void DrawArrows()
        {
            if (Event.current.type != EventType.Repaint) return;
            Camera cam = Cam;
            if (cam == null) return;
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            float radius = Mathf.Min(Screen.width, Screen.height) * 0.42f;
            foreach (VRRig r in Util.OtherRigs)
            {
                if (r == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(r.transform.position);
                bool behind = sp.z < 0f;
                Vector2 p = new Vector2(sp.x, Screen.height - sp.y);
                bool onScreen = !behind && p.x > 0f && p.x < Screen.width && p.y > 0f && p.y < Screen.height;
                if (onScreen) continue;
                Vector2 dir = p - center;
                if (behind) dir = -dir;
                if (dir.sqrMagnitude < 1f) dir = Vector2.down;
                dir.Normalize();
                Vector2 at = center + dir * radius;
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Matrix4x4 saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang + 45f, at);
                Color col = Util.PlayerColor(r);
                col.a = 0.95f;
                GUI.color = col;
                GUI.DrawTexture(new Rect(at.x - 9f, at.y - 9f, 18f, 18f), Tex.Diamond);
                GUI.matrix = saved;
            }
            GUI.color = Color.white;
        }

        [MenuButton("Off-Screen Arrows", Category = Cat, Order = 41, OnEnable = nameof(ArrowsOn), OnDisable = nameof(ArrowsOff), Tooltip = "Arrows on your PC screen point at players you cannot see")]
        public static void OffScreenArrows() { }
        public static void ArrowsOn() => Hud.Overlay += DrawArrows;
        public static void ArrowsOff() => Hud.Overlay -= DrawArrows;

        private static void Draw2DBoxes()
        {
            if (Event.current.type != EventType.Repaint) return;
            Camera cam = Cam;
            if (cam == null) return;
            foreach (VRRig r in Util.OtherRigs)
            {
                if (r == null) continue;
                Vector3 top = cam.WorldToScreenPoint(GunLib.HeadOf(r) + Vector3.up * 0.25f);
                Vector3 bottom = cam.WorldToScreenPoint(r.transform.position - Vector3.up * 0.45f);
                if (top.z < 0f || bottom.z < 0f) continue;
                float h = Mathf.Abs(bottom.y - top.y);
                float w = h * 0.7f;
                float x = top.x - w / 2f, y = Screen.height - top.y;
                Color col = Util.PlayerColor(r);
                col.a = 1f;
                Fill(new Rect(x, y, w, 2f), col);
                Fill(new Rect(x, y + h - 2f, w, 2f), col);
                Fill(new Rect(x, y, 2f, h), col);
                Fill(new Rect(x + w - 2f, y, 2f, h), col);
            }
            GUI.color = Color.white;
        }

        [MenuButton("2D Box ESP", Category = Cat, Order = 42, OnEnable = nameof(Box2DOn), OnDisable = nameof(Box2DOff), Tooltip = "Flat boxes around players drawn on your PC screen")]
        public static void Box2D() { }
        public static void Box2DOn() => Hud.Overlay += Draw2DBoxes;
        public static void Box2DOff() => Hud.Overlay -= Draw2DBoxes;

        private static GUIStyle markStyle;

        private static void DrawWatermark()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (markStyle == null)
                markStyle = new GUIStyle { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), fontSize = 14, fontStyle = FontStyle.Bold, richText = true, normal = { textColor = Color.white } };
            string room = Important.InRoom ? Important.RoomCode : "no room";
            string text = "<color=#" + Theme.AccentHex + ">riven</color>.menu  <color=#AAAAAA>v" + Menu.Version + "  |  " + Mathf.RoundToInt(Hud.Fps) + " fps  |  " + room + "</color>";
            Vector2 size = markStyle.CalcSize(new GUIContent(text));
            Rect r = new Rect(Screen.width / 2f - size.x / 2f - 12f, 10f, size.x + 24f, 28f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            Tex.FillStyle(8f).Draw(r, false, false, false, false);
            Fill(new Rect(r.x + 8f, r.yMax - 2f, r.width - 16f, 2f), Theme.Accent);
            GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 12f, r.y + 5f, size.x, 20f), text, markStyle);
        }

        [MenuButton("Watermark", Category = Cat, Order = 43, OnEnable = nameof(MarkOn), OnDisable = nameof(MarkOff), Tooltip = "A clean Riven bar at the top of your PC screen with FPS and room")]
        public static void Watermark() { }
        public static void MarkOn() => Hud.Overlay += DrawWatermark;
        public static void MarkOff() => Hud.Overlay -= DrawWatermark;

        private static float nextBuzz;

        private static string TaggerWarning()
        {
            if (Util.BodyT == null || Util.LocalRig == null) return "";
            if (Util.IsTagged(Util.LocalRig)) return "";
            float best = float.MaxValue;
            foreach (VRRig r in Util.OtherRigs)
            {
                if (r == null || !Util.IsTagged(r)) continue;
                float d = Vector3.Distance(r.transform.position, Util.BodyT.position);
                if (d < best) best = d;
            }
            if (best > 8f) return "";
            if (Time.time >= nextBuzz && Util.Tagger != null)
            {
                nextBuzz = Time.time + Mathf.Lerp(0.15f, 0.8f, best / 8f);
                Util.Tagger.StartVibration(true, 0.25f, 0.05f);
                Util.Tagger.StartVibration(false, 0.25f, 0.05f);
            }
            return "<color=#FF4D4D>TAGGER " + best.ToString("0.0") + "m AWAY</color>";
        }

        [MenuButton("Tagger Warning", Category = Cat, Order = 44, OnEnable = nameof(WarnOn), OnDisable = nameof(WarnOff), Tooltip = "Your controllers buzz faster the closer a tagger gets")]
        public static void TaggerWarn() { }
        public static void WarnOn() => Hud.Show("taggerwarn", TaggerWarning);
        public static void WarnOff() => Hud.Hide("taggerwarn");

        private static readonly RigPool trails = new RigPool(NewTrail);

        private static GameObject NewTrail(VRRig r)
        {
            GameObject go = new GameObject("RivenPlayerTrail");
            TrailRenderer t = go.AddComponent<TrailRenderer>();
            t.time = 1.2f;
            t.startWidth = 0.12f;
            t.endWidth = 0f;
            t.material = Util.NewUnlit(Color.white);
            t.minVertexDistance = 0.05f;
            return go;
        }

        [MenuButton("Player Trails", Category = Cat, Order = 45, OnDisable = nameof(TrailsOff), Tooltip = "Colored trails follow every player")]
        public static void PlayerTrails() => trails.Sync((r, go) =>
        {
            go.transform.position = r.transform.position;
            TrailRenderer t = go.GetComponent<TrailRenderer>();
            Color c = Util.PlayerColor(r);
            t.startColor = c;
            t.endColor = Theme.WithA(c, 0f);
            t.material.color = c;
        });
        public static void TrailsOff() => trails.Clear();

        private static GameObject NewRing(float radius, float width)
        {
            LineRenderer lr = Util.NewLine(width, Color.white);
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 32;
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            return lr.gameObject;
        }

        private static readonly RigPool crowns = new RigPool(r => NewRing(0.16f, 0.025f));

        [MenuButton("Tagged Halos", Category = Cat, Order = 46, OnDisable = nameof(HalosOff), Tooltip = "A spinning red halo over every tagged player")]
        public static void TaggedHalos() => crowns.Sync((r, go) =>
        {
            bool it = Util.IsTagged(r);
            if (go.activeSelf != it) go.SetActive(it);
            if (!it) return;
            go.transform.position = GunLib.HeadOf(r) + Vector3.up * 0.32f;
            go.transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 3f) * 10f, Time.time * 120f, 0f);
            LineRenderer lr = go.GetComponent<LineRenderer>();
            Color c = new Color(1f, 0.25f, 0.25f);
            lr.material.color = c;
            lr.startColor = c;
            lr.endColor = c;
        });
        public static void HalosOff() => crowns.Clear();

        private static readonly RigPool rings = new RigPool(r => NewRing(0.45f, 0.02f));

        [MenuButton("Ground Rings", Category = Cat, Order = 47, OnDisable = nameof(RingsOff), Tooltip = "A glowing ring on the ground under every player")]
        public static void GroundRings() => rings.Sync((r, go) =>
        {
            Vector3 p = r.transform.position;
            if (Physics.Raycast(p, Vector3.down, out RaycastHit hit, 4f, Util.RaycastMask)) p = hit.point + Vector3.up * 0.03f;
            else p += Vector3.down * 0.6f;
            go.transform.position = p;
            go.transform.rotation = Quaternion.identity;
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            go.transform.localScale = new Vector3(pulse, 1f, pulse);
            LineRenderer lr = go.GetComponent<LineRenderer>();
            Color c = Util.PlayerColor(r);
            lr.material.color = c;
            lr.startColor = c;
            lr.endColor = c;
        });
        public static void RingsOff() => rings.Clear();

        private static readonly RigPool looks = new RigPool(r => Util.NewLine(0.008f, Color.white).gameObject);

        [MenuButton("Look Lines", Category = Cat, Order = 48, OnDisable = nameof(LooksOff), Tooltip = "Shows where every player is looking")]
        public static void LookLines() => looks.Sync((r, go) =>
        {
            Transform head = r.head != null && r.head.rigTarget != null ? r.head.rigTarget : r.transform;
            Vector3 a = head.position;
            Util.SetLine(go.GetComponent<LineRenderer>(), a, a + head.forward * 3f, Util.PlayerColor(r));
        });
        public static void LooksOff() => looks.Clear();

        private static LineRenderer closestLine;

        [MenuButton("Closest Player Line", Category = Cat, Order = 49, OnDisable = nameof(ClosestOff), Tooltip = "One line from your hand to the closest player")]
        public static void ClosestPlayerLine()
        {
            if (Util.RightHand == null) return;
            VRRig best = null;
            float bestD = float.MaxValue;
            foreach (VRRig r in Util.OtherRigs)
            {
                if (r == null) continue;
                float d = (r.transform.position - Util.RightHand.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = r; }
            }
            if (closestLine == null) closestLine = Util.NewLine(0.012f, Color.white);
            bool show = best != null;
            if (closestLine.gameObject.activeSelf != show) closestLine.gameObject.SetActive(show);
            if (show) Util.SetLine(closestLine, Util.RightHand.position, best.transform.position, Util.PlayerColor(best));
        }
        public static void ClosestOff()
        {
            if (closestLine != null) Object.Destroy(closestLine.gameObject);
            closestLine = null;
        }

        private static bool nightSaved;
        private static Color nightAmbient;
        private static UnityEngine.Rendering.AmbientMode nightMode;

        [MenuButton("Night Vision", Category = Cat, Order = 50, OnDisable = nameof(NightOff), Tooltip = "Lights the world up green like night vision goggles")]
        public static void NightVision()
        {
            if (!nightSaved)
            {
                nightSaved = true;
                nightAmbient = RenderSettings.ambientLight;
                nightMode = RenderSettings.ambientMode;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 1f, 0.45f);
        }
        public static void NightOff()
        {
            if (!nightSaved) return;
            RenderSettings.ambientMode = nightMode;
            RenderSettings.ambientLight = nightAmbient;
            nightSaved = false;
        }

        private static readonly Dictionary<SkinnedMeshRenderer, Material> rainbowSaved = new Dictionary<SkinnedMeshRenderer, Material>();
        private static readonly List<SkinnedMeshRenderer> rainbowGone = new List<SkinnedMeshRenderer>();

        [MenuButton("Rainbow Players", Category = Cat, Order = 51, OnDisable = nameof(RainbowOff), Tooltip = "Every player cycles through rainbow colors for you")]
        public static void RainbowPlayers()
        {
            List<VRRig> rigs = Util.OtherRigs;
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig r = rigs[i];
                if (r == null || r.mainSkin == null) continue;
                SkinnedMeshRenderer skin = r.mainSkin;
                if (!rainbowSaved.ContainsKey(skin))
                {
                    rainbowSaved[skin] = skin.sharedMaterial;
                    skin.material = Util.NewUnlit(Color.white);
                }
                skin.material.color = Theme.Hue(i * 0.11f);
            }
            rainbowGone.Clear();
            foreach (SkinnedMeshRenderer s in rainbowSaved.Keys) if (s == null) rainbowGone.Add(s);
            foreach (SkinnedMeshRenderer s in rainbowGone) rainbowSaved.Remove(s);
        }
        public static void RainbowOff()
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, Material> kv in rainbowSaved)
                if (kv.Key != null) kv.Key.sharedMaterial = kv.Value;
            rainbowSaved.Clear();
        }
    }
}
