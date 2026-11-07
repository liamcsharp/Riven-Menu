using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class Desktop
    {
        private static float Dt => Time.deltaTime;
        private static Mouse M => Mouse.current;
        private static Vector2 MouseDelta => M != null ? M.delta.ReadValue() : Vector2.zero;

        private static int ScrollStep
        {
            get
            {
                if (M == null || MenuGUI.MouseOver) return 0;
                float y = M.scroll.ReadValue().y;
                return Mathf.Abs(y) < 0.01f ? 0 : (int)Mathf.Sign(y);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        private static Transform ViewT
        {
            get
            {
                Camera cam = GunLib.DesktopCamera;
                return cam != null ? cam.transform : Util.Head;
            }
        }

        private static Camera MakeCamera(string name, float depth)
        {
            Camera cam = new GameObject(name).AddComponent<Camera>();
            Camera src = GunLib.DesktopCamera;
            if (src != null)
            {
                cam.CopyFrom(src);
                cam.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            }
            cam.targetTexture = null;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.depth = depth;
            Object.DontDestroyOnLoad(cam.gameObject);
            return cam;
        }

        private class HandHome
        {
            public Vector3 Pos;
            public Quaternion Rot;
        }

        private static HandHome leftHome, rightHome;
        private static float reach = 0.55f;

        private static void MoveHand(Transform t, bool held, ref HandHome home, Vector3 target)
        {
            if (t == null) return;
            if (held)
            {
                if (home == null) home = new HandHome { Pos = t.localPosition, Rot = t.localRotation };
                t.position = Vector3.Lerp(t.position, target, 1f - Mathf.Exp(-25f * Dt));
            }
            else if (home != null)
            {
                t.localPosition = home.Pos;
                t.localRotation = home.Rot;
                home = null;
            }
        }

        [MenuButton("Desktop Hands", Category = "Desktop", Order = 0, OnDisable = nameof(HandsOff), Tooltip = "Hold Q for left hand, E for right, they follow your mouse. Scroll changes reach")]
        public static void DesktopHands()
        {
            if (!Util.Ready || !GunLib.MouseRay(out Ray ray)) return;
            bool l = Util.HeldKey(Key.Q), r = Util.HeldKey(Key.E);
            if ((l || r) && ScrollStep != 0) reach = Mathf.Clamp(reach + ScrollStep * 0.05f, 0.25f, 1.1f);

            Vector3 target = Util.Head.position + ray.direction * reach;
            MoveHand(Util.Player.LeftHand.controllerTransform, l, ref leftHome, target);
            MoveHand(Util.Player.RightHand.controllerTransform, r, ref rightHome, target);
        }

        public static void HandsOff()
        {
            if (Util.Player == null) return;
            MoveHand(Util.Player.LeftHand.controllerTransform, false, ref leftHome, Vector3.zero);
            MoveHand(Util.Player.RightHand.controllerTransform, false, ref rightHome, Vector3.zero);
        }

        private static float pitch;

        private static void Lock(bool on)
        {
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        public static void LookOn()
        {
            Lock(true);
            Notify.Push("Mouse Look on - Esc to stop, Alt frees cursor");
        }

        [MenuButton("Mouse Look", Category = "Desktop", Order = 1, OnEnable = nameof(LookOn), OnDisable = nameof(LookOff), Tooltip = "Turn and look around with the mouse, Left Alt frees the cursor")]
        public static void MouseLook()
        {
            if (!Util.Ready) return;
            if (Util.Hotkey(Key.LeftAlt)) Lock(Cursor.lockState != CursorLockMode.Locked);
            if (Cursor.lockState != CursorLockMode.Locked || freecam != null) return;

            Vector2 d = MouseDelta * 0.12f;
            if (Mathf.Abs(d.x) > 0.001f) Util.Player.Turn(d.x);
            pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
            Transform cam = Util.Tagger.mainCamera.transform;
            cam.localEulerAngles = new Vector3(pitch, cam.localEulerAngles.y, 0f);
        }

        public static void LookOff()
        {
            Lock(false);
            pitch = 0f;
            if (Util.Tagger == null) return;
            Transform cam = Util.Tagger.mainCamera.transform;
            cam.localEulerAngles = new Vector3(0f, cam.localEulerAngles.y, 0f);
        }

        [MenuButton("WASD Walk", Category = "Desktop", Order = 2, Tooltip = "WASD to walk, Shift to sprint, Space to jump")]
        public static void WasdWalk()
        {
            if (!Util.Ready || freecam != null) return;
            Transform view = ViewT;
            Vector3 dir = Vector3.zero;
            if (Util.HeldKey(Key.W)) dir += Flat(view.forward);
            if (Util.HeldKey(Key.S)) dir -= Flat(view.forward);
            if (Util.HeldKey(Key.D)) dir += Flat(view.right);
            if (Util.HeldKey(Key.A)) dir -= Flat(view.right);

            bool grounded = Util.Grounded;
            Vector3 v = Util.Body.velocity;
            if (dir.sqrMagnitude > 0.01f || grounded)
            {
                Vector3 target = dir.normalized * (Util.HeldKey(Key.LeftShift) ? 9f : 5f);
                float k = 1f - Mathf.Exp(-(grounded ? 12f : 3f) * Dt);
                v.x = Mathf.Lerp(v.x, target.x, k);
                v.z = Mathf.Lerp(v.z, target.z, k);
            }
            if (grounded && Util.Hotkey(Key.Space)) v.y = 6.5f;
            Util.Body.velocity = v;
        }

        private static Camera freecam;
        private static float camYaw, camPitch, camSpeed = 8f;

        public static void FreecamOn()
        {
            freecam = MakeCamera("RivenFreecam", 100f);
            Vector3 e = freecam.transform.eulerAngles;
            camYaw = e.y;
            camPitch = e.x > 180f ? e.x - 360f : e.x;
        }

        [MenuButton("Freecam", Category = "Desktop", Order = 3, OnEnable = nameof(FreecamOn), OnDisable = nameof(FreecamOff), Tooltip = "Fly the camera: WASD, Space up, Ctrl down, hold right mouse to look, scroll for speed")]
        public static void Freecam()
        {
            if (freecam == null) return;
            if (ScrollStep != 0) camSpeed = Mathf.Clamp(camSpeed * (ScrollStep > 0 ? 1.25f : 0.8f), 1f, 80f);
            if ((M != null && M.rightButton.isPressed) || Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = MouseDelta * 0.12f;
                camYaw += d.x;
                camPitch = Mathf.Clamp(camPitch - d.y, -89f, 89f);
            }

            Transform t = freecam.transform;
            t.rotation = Quaternion.Euler(camPitch, camYaw, 0f);
            Vector3 move = Vector3.zero;
            if (Util.HeldKey(Key.W)) move += t.forward;
            if (Util.HeldKey(Key.S)) move -= t.forward;
            if (Util.HeldKey(Key.D)) move += t.right;
            if (Util.HeldKey(Key.A)) move -= t.right;
            if (Util.HeldKey(Key.Space)) move += Vector3.up;
            if (Util.HeldKey(Key.LeftCtrl)) move -= Vector3.up;
            float sp = camSpeed * (Util.HeldKey(Key.LeftShift) ? 3f : 1f);
            t.position += move.normalized * (sp * Time.unscaledDeltaTime);
        }

        public static void FreecamOff()
        {
            if (freecam != null) Object.Destroy(freecam.gameObject);
            freecam = null;
        }

        private static Camera thirdCam;
        private static float thirdDist = 2.5f;

        public static void ThirdOn() => thirdCam = MakeCamera("RivenThirdPerson", 95f);

        [MenuButton("Third Person", Category = "Desktop", Order = 4, OnEnable = nameof(ThirdOn), OnDisable = nameof(ThirdOff), Tooltip = "Camera behind your gorilla, scroll to zoom")]
        public static void ThirdPerson()
        {
            if (thirdCam == null || !Util.Ready) return;
            if (ScrollStep != 0 && !Util.HeldKey(Key.Q) && !Util.HeldKey(Key.E)) thirdDist = Mathf.Clamp(thirdDist - ScrollStep * 0.3f, 0.8f, 8f);

            Vector3 head = Util.Head.position;
            Vector3 back = -Util.Head.forward + Vector3.up * 0.35f;
            float dist = thirdDist;
            if (Physics.SphereCast(head, 0.15f, back.normalized, out RaycastHit hit, thirdDist, Util.RaycastMask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.3f, hit.distance - 0.1f);

            Transform t = thirdCam.transform;
            t.position = Vector3.Lerp(t.position, head + back.normalized * dist, 1f - Mathf.Exp(-14f * Dt));
            t.rotation = Quaternion.LookRotation(head + Util.Head.forward * 2f - t.position);
        }

        public static void ThirdOff()
        {
            if (thirdCam != null) Object.Destroy(thirdCam.gameObject);
            thirdCam = null;
        }

        private static Camera zoomCam;
        private static float zoomFrom = -1f, zoomFov = 25f;

        [MenuButton("Zoom", Category = "Desktop", Order = 5, OnDisable = nameof(ZoomOff), Tooltip = "Hold C to zoom, scroll while zooming to change how far")]
        public static void Zoom()
        {
            bool held = Util.HeldKey(Key.C);
            if (held && zoomCam == null)
            {
                zoomCam = GunLib.DesktopCamera;
                if (zoomCam != null) zoomFrom = zoomCam.fieldOfView;
            }
            if (zoomCam == null) return;

            if (held && ScrollStep != 0) zoomFov = Mathf.Clamp(zoomFov - ScrollStep * 4f, 8f, 60f);
            float goal = held ? zoomFov : zoomFrom;
            zoomCam.fieldOfView = Mathf.Lerp(zoomCam.fieldOfView, goal, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            if (!held && Mathf.Abs(zoomCam.fieldOfView - zoomFrom) < 0.1f) ZoomOff();
        }

        public static void ZoomOff()
        {
            if (zoomCam != null && zoomFrom > 0f) zoomCam.fieldOfView = zoomFrom;
            zoomCam = null;
        }

        [MenuButton("Click Teleport", Category = "Desktop", Order = 6, Tooltip = "Middle click teleports you to where your mouse points")]
        public static void ClickTeleport()
        {
            if (!Util.Ready || M == null || MenuGUI.MouseOver || !Util.Pressed("desk_tp", M.middleButton.isPressed)) return;
            if (GunLib.MouseRay(out Ray ray) && Physics.Raycast(ray, out RaycastHit hit, 512f, Util.RaycastMask, QueryTriggerInteraction.Ignore))
                Util.Teleport(hit.point + hit.normal * 0.8f);
        }

        [MenuButton("Snap Turn Keys", Category = "Desktop", Order = 7, Tooltip = "Left and Right arrows turn you 45 degrees")]
        public static void SnapTurnKeys()
        {
            if (Util.Player == null) return;
            if (Util.Hotkey(Key.LeftArrow)) Util.Player.Turn(-45f);
            if (Util.Hotkey(Key.RightArrow)) Util.Player.Turn(45f);
        }

        [MenuButton("Quick Disconnect Key", Category = "Desktop", Order = 8, Tooltip = "F9 leaves the room, press again to rejoin")]
        public static void QuickDisconnectKey()
        {
            if (!Util.Hotkey(Key.F9)) return;
            if (Important.InRoom) Important.Disconnect();
            else Important.RejoinLastRoom();
        }

        [MenuButton("Panic Key", Category = "Desktop", Order = 9, Tooltip = "End turns every mod off instantly")]
        public static void PanicKey()
        {
            if (!Util.Hotkey(Key.End)) return;
            Buttons.DisableAll();
            Buttons.SetEnabled("Panic Key", true);
            Notify.Push("Panic - all mods off");
        }

        private static string ShotFolder => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "RivenScreenshots");

        [MenuButton("Take Screenshot", ButtonType.Button, Category = "Desktop", Order = 10, Notify = false, Tooltip = "Saves a screenshot to the RivenScreenshots folder")]
        public static void TakeScreenshot()
        {
            Directory.CreateDirectory(ShotFolder);
            string file = Path.Combine(ShotFolder, $"riven_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
            ScreenCapture.CaptureScreenshot(file);
            Notify.Push("Screenshot saved");
        }

        [MenuButton("Screenshot Key", Category = "Desktop", Order = 11, Tooltip = "F12 takes a screenshot")]
        public static void ScreenshotKey()
        {
            if (Util.Hotkey(Key.F12)) TakeScreenshot();
        }

        [MenuButton("Free Cursor", Category = "Desktop", Order = 12, Tooltip = "Keeps your mouse unlocked and visible")]
        public static void FreeCursor()
        {
            if (Menu.IsOn("Mouse Look")) return;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static readonly (string Mod, string Keys)[] Binds =
        {
            ("Desktop Hands", "Q/E hands"),
            ("Mouse Look", "Alt cursor"),
            ("WASD Walk", "WASD walk"),
            ("Freecam", "RMB look"),
            ("Zoom", "C zoom"),
            ("Click Teleport", "MMB tp"),
            ("Snap Turn Keys", "Arrows turn"),
            ("Quick Disconnect Key", "F9 leave"),
            ("Panic Key", "End panic"),
            ("Screenshot Key", "F12 shot"),
            ("Ghost Monke", "G ghost"),
            ("Invisible Monke", "H invis"),
        };

        private static string BindLine()
        {
            List<string> on = new List<string>();
            foreach ((string mod, string keys) in Binds)
                if (Menu.IsOn(mod)) on.Add(keys);
            if (GunLib.Desktop) on.Add("RMB aim  LMB fire");
            on.Add("Esc all off");
            return string.Join("   ", on);
        }

        public static void BindsOn() => Hud.Show("binds", BindLine);
        public static void BindsOff() => Hud.Hide("binds");

        public static void EscapeAll()
        {
            List<ButtonInfo> on = new List<ButtonInfo>();
            foreach (ButtonInfo b in Buttons.All)
                if (b.IsActive && string.Equals(b.Category, "Desktop", StringComparison.OrdinalIgnoreCase)) on.Add(b);
            foreach (ButtonInfo b in on) Buttons.SetEnabled(b, false);
            Lock(false);
            if (on.Count > 0) Notify.Push(on.Count == 1 ? on[0].Name + " off" : on.Count + " desktop mods off");
        }

        [MenuButton("Keybind HUD", Category = "Desktop", Order = 13, OnEnable = nameof(BindsOn), OnDisable = nameof(BindsOff), Tooltip = "Shows the keys for the desktop mods you have on")]
        public static void KeybindHud() { }
    }
}
