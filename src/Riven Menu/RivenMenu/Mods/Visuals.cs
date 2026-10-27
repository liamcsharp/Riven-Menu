using System;
using System.Collections.Generic;
using System.Linq;
using GorillaTag.Rendering;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public class RigPool
    {
        private readonly Dictionary<VRRig, GameObject> objects = new Dictionary<VRRig, GameObject>();
        private readonly Func<VRRig, GameObject> create;

        public RigPool(Func<VRRig, GameObject> create) => this.create = create;

        public void Sync(Action<VRRig, GameObject> update)
        {
            List<VRRig> rigs = Util.OtherRigs.ToList();
            foreach (VRRig gone in objects.Keys.Where(r => r == null || !rigs.Contains(r)).ToList())
            {
                if (objects[gone] != null) Object.Destroy(objects[gone]);
                objects.Remove(gone);
            }
            foreach (VRRig rig in rigs)
            {
                if (!objects.TryGetValue(rig, out GameObject go) || go == null)
                {
                    go = create(rig);
                    objects[rig] = go;
                }
                go.layer = Util.VisualLayer;
                foreach (Transform child in go.transform) child.gameObject.layer = Util.VisualLayer;
                update(rig, go);
            }
        }

        public void Clear()
        {
            foreach (GameObject go in objects.Values)
                if (go != null) Object.Destroy(go);
            objects.Clear();
        }
    }

    public static class Visuals
    {
        private static Vector3 HeadOf(VRRig r) => r.head != null && r.head.rigTarget != null ? r.head.rigTarget.position : r.transform.position + Vector3.up * 0.3f;
        private static Vector3 LookPoint => Util.Head != null ? Util.Head.position : Vector3.zero;

        private static void Face(Transform t)
        {
            if (Util.Head == null) return;
            t.LookAt(LookPoint);
        }

        private static readonly RigPool boxes = new RigPool(r => Util.Primitive(PrimitiveType.Cube, r.transform.position, new Vector3(0.5f, 0.5f, 0f), Color.white));

        [MenuButton("Casual Box ESP", Category = "Visuals", Order = 0, OnDisable = nameof(BoxOff), Tooltip = "Draws a box on every player")]
        public static void CasualBoxESP() => boxes.Sync((r, go) =>
        {
            go.GetComponent<Renderer>().material.color = Util.PlayerColor(r);
            go.transform.position = r.transform.position;
            Face(go.transform);
        });
        public static void BoxOff() => boxes.Clear();

        private static readonly RigPool tagBoxes = new RigPool(r => Util.Primitive(PrimitiveType.Cube, r.transform.position, new Vector3(0.5f, 0.5f, 0f), Color.white));

        [MenuButton("Tag ESP", Category = "Visuals", Order = 1, OnDisable = nameof(TagOff), Tooltip = "Red box on tagged players, green on everyone else")]
        public static void TagESP() => tagBoxes.Sync((r, go) =>
        {
            Color c = Util.IsTagged(r) ? new Color(1f, 0.2f, 0.25f) : new Color(0.3f, 1f, 0.4f);
            if (Menu.IsOn("Transparent Theme")) c.a = 0.45f;
            go.GetComponent<Renderer>().material.color = c;
            go.transform.position = r.transform.position;
            Face(go.transform);
        });
        public static void TagOff() => tagBoxes.Clear();

        private static GameObject NewOutline(VRRig r)
        {
            GameObject root = new GameObject("RivenOutline");
            LineRenderer lr = root.AddComponent<LineRenderer>();
            lr.material = Util.NewUnlit(Color.white);
            lr.loop = true;
            lr.positionCount = 4;
            lr.startWidth = lr.endWidth = 0.025f;
            lr.useWorldSpace = false;
            lr.SetPositions(new[] { new Vector3(-0.3f, -0.35f, 0f), new Vector3(0.3f, -0.35f, 0f), new Vector3(0.3f, 0.45f, 0f), new Vector3(-0.3f, 0.45f, 0f) });
            return root;
        }

        private static readonly RigPool outlines = new RigPool(NewOutline);

        [MenuButton("Hollow Box ESP", Category = "Visuals", Order = 2, OnDisable = nameof(OutlineOff), Tooltip = "An outline box around every player")]
        public static void HollowBoxESP() => outlines.Sync((r, go) =>
        {
            LineRenderer lr = go.GetComponent<LineRenderer>();
            Color c = Util.PlayerColor(r);
            lr.material.color = c;
            lr.startColor = lr.endColor = c;
            go.transform.position = r.transform.position;
            Face(go.transform);
        });
        public static void OutlineOff() => outlines.Clear();

        private static readonly RigPool tracers = new RigPool(r => Util.NewLine(0.01f, Color.white).gameObject);

        [MenuButton("Tracers", Category = "Visuals", Order = 3, OnDisable = nameof(TracersOff), Tooltip = "Lines from your hand to every player")]
        public static void Tracers() => tracers.Sync((r, go) =>
        {
            if (Util.RightHand == null) return;
            Util.SetLine(go.GetComponent<LineRenderer>(), Util.RightHand.position, r.transform.position, Util.PlayerColor(r));
        });
        public static void TracersOff() => tracers.Clear();

        private static readonly RigPool beacons = new RigPool(r => Util.NewLine(0.05f, Color.white).gameObject);

        [MenuButton("Beacons", Category = "Visuals", Order = 4, OnDisable = nameof(BeaconsOff), Tooltip = "Tall light beams above every player")]
        public static void Beacons() => beacons.Sync((r, go) =>
        {
            Vector3 p = r.transform.position;
            Util.SetLine(go.GetComponent<LineRenderer>(), p, p + Vector3.up * 40f, Theme.WithA(Util.PlayerColor(r), 0.5f));
        });
        public static void BeaconsOff() => beacons.Clear();

        private static GameObject NewSkeleton(VRRig r)
        {
            GameObject root = new GameObject("RivenSkeleton");
            for (int i = 0; i < 3; i++)
            {
                LineRenderer lr = Util.NewLine(0.015f, Color.white);
                lr.transform.SetParent(root.transform, false);
            }
            return root;
        }

        private static readonly RigPool skeletons = new RigPool(NewSkeleton);

        [MenuButton("Skeleton ESP", Category = "Visuals", Order = 5, OnDisable = nameof(SkeletonOff), Tooltip = "Lines between head, body and hands")]
        public static void SkeletonESP() => skeletons.Sync((r, go) =>
        {
            LineRenderer[] lines = go.GetComponentsInChildren<LineRenderer>();
            if (lines.Length < 3) return;
            Color c = Util.PlayerColor(r);
            Vector3 body = r.bodyTransform != null ? r.bodyTransform.position : r.transform.position;
            Util.SetLine(lines[0], HeadOf(r), body, c);
            Util.SetLine(lines[1], body, r.leftHandTransform.position, c);
            Util.SetLine(lines[2], body, r.rightHandTransform.position, c);
        });
        public static void SkeletonOff() => skeletons.Clear();

        private static GameObject NewHands(VRRig r)
        {
            GameObject root = new GameObject("RivenHands");
            for (int i = 0; i < 2; i++)
            {
                GameObject s = Util.Primitive(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.12f, Color.white);
                s.transform.SetParent(root.transform, false);
            }
            return root;
        }

        private static readonly RigPool hands = new RigPool(NewHands);

        [MenuButton("Hand ESP", Category = "Visuals", Order = 6, OnDisable = nameof(HandsOff), Tooltip = "Glowing balls on everyone's hands")]
        public static void HandESP() => hands.Sync((r, go) =>
        {
            Color c = Util.PlayerColor(r);
            Transform a = go.transform.GetChild(0), b = go.transform.GetChild(1);
            a.position = r.leftHandTransform.position;
            b.position = r.rightHandTransform.position;
            a.GetComponent<Renderer>().material.color = c;
            b.GetComponent<Renderer>().material.color = c;
        });
        public static void HandsOff() => hands.Clear();

        private static readonly RigPool heads = new RigPool(r => Util.Primitive(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.35f, Color.white));

        [MenuButton("Head ESP", Category = "Visuals", Order = 7, OnDisable = nameof(HeadsOff), Tooltip = "A bubble around every head")]
        public static void HeadESP() => heads.Sync((r, go) =>
        {
            go.transform.position = HeadOf(r);
            go.GetComponent<Renderer>().material.color = Theme.WithA(Util.PlayerColor(r), 0.4f);
        });
        public static void HeadsOff() => heads.Clear();

        private static Font tagFont;

        private static GameObject NewTag(VRRig r)
        {
            if (tagFont == null) tagFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject go = new GameObject("RivenTag");
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = tagFont;
            tm.fontSize = 48;
            tm.characterSize = 0.012f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().material = tagFont.material;
            return go;
        }

        private static readonly RigPool nameTags = new RigPool(NewTag);

        private static void Billboard(Transform t)
        {
            if (Util.Head == null) return;
            t.rotation = Quaternion.LookRotation(t.position - Util.Head.position);
        }

        [MenuButton("Name Tags", Category = "Visuals", Order = 8, OnDisable = nameof(NamesOff), Tooltip = "Shows names above everyone")]
        public static void NameTags() => nameTags.Sync((r, go) =>
        {
            TextMesh tm = go.GetComponent<TextMesh>();
            tm.text = r.OwningNetPlayer != null ? r.OwningNetPlayer.NickName : "?";
            tm.color = Util.PlayerColor(r);
            go.transform.position = HeadOf(r) + Vector3.up * 0.45f;
            Billboard(go.transform);
        });
        public static void NamesOff() => nameTags.Clear();

        private static readonly RigPool distTags = new RigPool(NewTag);

        [MenuButton("Distance Tags", Category = "Visuals", Order = 9, OnDisable = nameof(DistOff), Tooltip = "Shows how far away everyone is")]
        public static void DistanceTags() => distTags.Sync((r, go) =>
        {
            TextMesh tm = go.GetComponent<TextMesh>();
            float d = Util.BodyT != null ? Vector3.Distance(Util.BodyT.position, r.transform.position) : 0f;
            tm.text = d.ToString("0.0") + "m";
            tm.color = Color.white;
            go.transform.position = HeadOf(r) + Vector3.up * 0.3f;
            Billboard(go.transform);
        });
        public static void DistOff() => distTags.Clear();

        private static readonly Dictionary<SkinnedMeshRenderer, Material> chamsSaved = new Dictionary<SkinnedMeshRenderer, Material>();

        [MenuButton("Chams", Category = "Visuals", Order = 10, OnDisable = nameof(ChamsOff), Tooltip = "Players glow through walls")]
        public static void Chams()
        {
            foreach (VRRig r in Util.OtherRigs)
            {
                SkinnedMeshRenderer skin = r.mainSkin;
                if (skin == null) continue;
                if (!chamsSaved.ContainsKey(skin))
                {
                    chamsSaved[skin] = skin.sharedMaterial;
                    skin.material = Util.NewUnlit(Color.white);
                }
                skin.material.color = Util.PlayerColor(r);
            }
        }

        public static void ChamsOff()
        {
            foreach (KeyValuePair<SkinnedMeshRenderer, Material> kv in chamsSaved)
                if (kv.Key != null) kv.Key.sharedMaterial = kv.Value;
            chamsSaved.Clear();
        }

        private static LineRenderer velocityLine;

        [MenuButton("Velocity Line", Category = "Visuals", Order = 11, OnDisable = nameof(VelocityOff), Tooltip = "Shows where your momentum is taking you")]
        public static void VelocityLine()
        {
            if (!Util.Ready) return;
            if (velocityLine == null) velocityLine = Util.NewLine(0.02f, Theme.Accent);
            Vector3 p = Util.BodyT.position;
            Util.SetLine(velocityLine, p, p + Util.Body.velocity * 0.5f, Theme.Accent);
        }
        public static void VelocityOff()
        {
            if (velocityLine != null) Object.Destroy(velocityLine.gameObject);
        }

        private static TrailRenderer leftTrail, rightTrail;

        private static TrailRenderer NewTrail(Transform hand)
        {
            GameObject go = new GameObject("RivenTrail");
            go.transform.SetParent(hand, false);
            TrailRenderer tr = go.AddComponent<TrailRenderer>();
            tr.material = Util.NewUnlit(Color.white);
            tr.time = 0.6f;
            tr.startWidth = 0.05f;
            tr.endWidth = 0f;
            return tr;
        }

        [MenuButton("Hand Trails", Category = "Visuals", Order = 12, OnDisable = nameof(TrailsOff), Tooltip = "Colored trails behind your hands")]
        public static void HandTrails()
        {
            if (!Util.Ready) return;
            if (leftTrail == null) leftTrail = NewTrail(Util.LeftHand);
            if (rightTrail == null) rightTrail = NewTrail(Util.RightHand);
            foreach (TrailRenderer tr in new[] { leftTrail, rightTrail })
            {
                tr.startColor = Theme.Accent;
                tr.endColor = Theme.WithA(Theme.Accent2, 0f);
                tr.material.color = Color.white;
                tr.gameObject.layer = Util.VisualLayer;
            }
        }
        public static void TrailsOff()
        {
            if (leftTrail != null) Object.Destroy(leftTrail.gameObject);
            if (rightTrail != null) Object.Destroy(rightTrail.gameObject);
        }

        private static Color savedAmbient;
        private static UnityEngine.Rendering.AmbientMode savedMode;
        private static bool ambientSaved;

        private static void SaveAmbient()
        {
            if (ambientSaved) return;
            savedAmbient = RenderSettings.ambientLight;
            savedMode = RenderSettings.ambientMode;
            ambientSaved = true;
        }

        public static void RestoreAmbient()
        {
            if (!ambientSaved) return;
            RenderSettings.ambientMode = savedMode;
            RenderSettings.ambientLight = savedAmbient;
            ambientSaved = false;
        }

        [MenuButton("Fullbright", Category = "Visuals", Order = 20, OnDisable = nameof(RestoreAmbient), Tooltip = "Lights up everything")]
        public static void Fullbright()
        {
            SaveAmbient();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
        }

        [MenuButton("Disco Lights", Category = "Visuals", Order = 21, OnDisable = nameof(RestoreAmbient), Tooltip = "The world flashes rainbow colors")]
        public static void DiscoLights()
        {
            SaveAmbient();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.5f, 1f), 0.8f, 1f);
        }

        [MenuButton("Enable Fog", ButtonType.Button, Category = "Visuals", Order = 22, Tooltip = "Turns ground fog on")]
        public static void EnableFog()
        {
            if (ZoneShaderSettings.activeInstance != null)
                ZoneShaderSettings.activeInstance.SetGroundFogValue(new Color(0.9569f, 0.6941f, 0.502f, 0.1216f), 40f, 10f, 40f);
        }

        [MenuButton("Disable Fog", ButtonType.Button, Category = "Visuals", Order = 23, Tooltip = "Turns ground fog off")]
        public static void DisableFog()
        {
            if (ZoneShaderSettings.activeInstance != null)
                ZoneShaderSettings.activeInstance.SetGroundFogValue(Color.clear, 0f, 0f, 0f);
        }

        private static void SetSnow(bool enable)
        {
            GameObject weather = GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest/Environment/WeatherDayNight");
            if (weather == null) { Notify.Push("Go to Forest for snow"); return; }
            Transform snow = weather.transform.Find("snow");
            if (snow == null) return;
            snow.gameObject.SetActive(enable);
            snow.position += Vector3.one * (enable ? 0.001f : -0.001f);
            TimeOfDayDependentAudio audio = snow.GetComponent<TimeOfDayDependentAudio>();
            if (audio != null) audio.enabled = !enable;
            Transform particles = snow.Find("snow partic");
            if (particles != null) particles.gameObject.SetActive(enable);
        }

        public static void SnowOn() => SetSnow(true);
        public static void SnowOff() => SetSnow(false);

        [MenuButton("Snow", Category = "Visuals", Order = 24, OnEnable = nameof(SnowOn), OnDisable = nameof(SnowOff), Tooltip = "Makes it snow in Forest")]
        public static void Snow() { }

        public static void RainOn()
        {
            if (BetterDayNightManager.instance != null)
                BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.Raining, true);
        }

        public static void RainOff()
        {
            if (BetterDayNightManager.instance != null)
                BetterDayNightManager.instance.ClearFixedWeather(true);
        }

        [MenuButton("Rain", Category = "Visuals", Order = 25, OnEnable = nameof(RainOn), OnDisable = nameof(RainOff), Tooltip = "Makes it rain for you")]
        public static void Rain() { }

        private static readonly int[] TimeIndexes = { 3, 1, 7, 0 };

        [MenuButton("Time Of Day", ButtonType.Option, Category = "Visuals", Order = 26, Options = new[] { "Day", "Morning", "Evening", "Night" }, Tooltip = "Changes the time for you")]
        public static void TimeOfDay()
        {
            if (BetterDayNightManager.instance != null)
                BetterDayNightManager.instance.SetTimeOfDay(TimeIndexes[Menu.Index("Time Of Day")], true);
        }

        private static readonly Dictionary<TimeOfDayDependentAudio, float> ambience = new Dictionary<TimeOfDayDependentAudio, float>();

        [MenuButton("Disable Ambience", Category = "Visuals", Order = 27, OnDisable = nameof(AmbienceOff), Tooltip = "Mutes background nature sounds")]
        public static void DisableAmbience()
        {
            foreach (TimeOfDayDependentAudio a in Object.FindObjectsOfType<TimeOfDayDependentAudio>())
            {
                if (!ambience.ContainsKey(a)) ambience[a] = a.currentVolume;
                a.currentVolume = 0f;
            }
        }

        public static void AmbienceOff()
        {
            foreach (KeyValuePair<TimeOfDayDependentAudio, float> kv in ambience)
                if (kv.Key != null) kv.Key.currentVolume = kv.Value;
            ambience.Clear();
        }

        private const string LeavesUrl = "https://raw.githubusercontent.com/ASTRA228b/No-Leaves.ASTRA-OBJNAME/main/OBJECTNSME.txt";
        private static readonly string[] FallbackLeaves = { "UnityTempFile-1558c798746eb914da02833dd537c391 (combined by EdMeshCombiner)" };
        private static HashSet<string> leafNames;
        private static UnityWebRequest leafRequest;
        private static readonly List<Renderer> hiddenLeaves = new List<Renderer>();
        private static float nextLeafScan;

        public static void NoLeavesOn()
        {
            nextLeafScan = 0f;
            if (leafNames != null || leafRequest != null) return;
            leafRequest = UnityWebRequest.Get(LeavesUrl);
            leafRequest.timeout = 10;
            leafRequest.SendWebRequest();
        }

        [MenuButton("No Leaves", Category = "Visuals", Order = 32, OnEnable = nameof(NoLeavesOn), OnDisable = nameof(NoLeavesOff), Tooltip = "Hides tree leaves, the object list is downloaded from GitHub")]
        public static void NoLeaves()
        {
            if (leafNames == null)
            {
                if (leafRequest == null || !leafRequest.isDone) return;
                string[] names = leafRequest.result == UnityWebRequest.Result.Success
                    ? leafRequest.downloadHandler.text.Trim('﻿').Split('\n', '\r')
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0 && !l.StartsWith("#") && !l.StartsWith("//"))
                        .ToArray()
                    : new string[0];
                if (names.Length == 0)
                {
                    names = FallbackLeaves;
                    Notify.Push("Couldn't download the leaves list, using the built in one");
                }
                leafNames = new HashSet<string>(names);
                leafRequest.Dispose();
                leafRequest = null;
            }

            if (Time.time < nextLeafScan) return;
            nextLeafScan = Time.time + 4f;
            hiddenLeaves.RemoveAll(r => r == null);

            foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                bool match = leafNames.Contains(mf.gameObject.name) || (mf.sharedMesh != null && leafNames.Contains(mf.sharedMesh.name));
                if (!match) continue;
                Renderer r = mf.GetComponent<Renderer>();
                if (r == null || !r.enabled) continue;
                r.enabled = false;
                hiddenLeaves.Add(r);
            }
        }

        public static void NoLeavesOff()
        {
            foreach (Renderer r in hiddenLeaves)
                if (r != null) r.enabled = true;
            hiddenLeaves.Clear();
        }

        private static float savedFov = -1f;
        private static readonly float[] Fovs = { 60f, 75f, 90f, 105f, 120f };

        [MenuButton("PC Field Of View", ButtonType.Option, Category = "Visuals", Order = 28, Options = new[] { "60", "75", "90", "105", "120" }, DefaultOption = 0, Tooltip = "Changes the desktop camera FOV")]
        public static void FieldOfView()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            if (savedFov < 0f) savedFov = cam.fieldOfView;
            cam.fieldOfView = Fovs[Menu.Index("PC Field Of View")];
        }

        private static void DrawCrosshair()
        {
            if (Event.current.type != EventType.Repaint) return;
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            GUI.color = Theme.Accent;
            GUI.DrawTexture(new Rect(cx - 9f, cy - 1f, 18f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 9f, 2f, 18f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void CrosshairOn() => Hud.Overlay += DrawCrosshair;
        public static void CrosshairOff() => Hud.Overlay -= DrawCrosshair;

        [MenuButton("Crosshair", Category = "Visuals", Order = 29, OnEnable = nameof(CrosshairOn), OnDisable = nameof(CrosshairOff), Tooltip = "A crosshair in the middle of your PC screen")]
        public static void Crosshair() { }

        private static string ClosestLine()
        {
            if (Util.BodyT == null) return "No players";
            VRRig best = null;
            float bestD = float.MaxValue;
            foreach (VRRig r in Util.OtherRigs)
            {
                float d = Vector3.Distance(r.transform.position, Util.BodyT.position);
                if (d < bestD) { bestD = d; best = r; }
            }
            if (best == null) return "No players nearby";
            string name = best.OwningNetPlayer != null ? best.OwningNetPlayer.NickName : "?";
            return $"Closest: {name}  {bestD:0.0}m";
        }

        public static void ClosestOn() => Hud.Show("closest", ClosestLine);
        public static void ClosestOff() => Hud.Hide("closest");

        [MenuButton("Closest Player HUD", Category = "Visuals", Order = 30, OnEnable = nameof(ClosestOn), OnDisable = nameof(ClosestOff), Tooltip = "Shows the closest player and distance")]
        public static void ClosestPlayer() { }

        public static void TaggedOn() => Hud.Show("tagged", () => $"Tagged: {Util.OtherRigs.Count(Util.IsTagged) + (Util.IsTagged(Util.LocalRig) ? 1 : 0)}");
        public static void TaggedOff() => Hud.Hide("tagged");

        [MenuButton("Tagged Count HUD", Category = "Visuals", Order = 31, OnEnable = nameof(TaggedOn), OnDisable = nameof(TaggedOff), Tooltip = "Shows how many players are tagged")]
        public static void TaggedCount() { }
    }
}
