using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Object = UnityEngine.Object;

namespace RivenMenu
{
    public struct GunHit
    {
        public Vector3 Origin, Direction, Point, Normal;
        public bool Hit, Fire, FireDown;
        public VRRig Rig;
    }

    public static class GunLib
    {
        public static bool LeftHanded;
        public static bool Desktop => !XRSettings.isDeviceActive;
        public static Transform Hand => LeftHanded ? Util.LeftHand : Util.RightHand;

        private static Mouse M => Mouse.current;

        public static bool Aiming => Desktop
            ? M != null && M.rightButton.isPressed && !MenuGUI.MouseOver
            : LeftHanded ? Util.LeftGrip : Util.RightGrip;

        public static bool Firing => Desktop
            ? M != null && M.leftButton.isPressed && !MenuGUI.MouseOver
            : LeftHanded ? Util.LeftTrigger : Util.RightTrigger;

        private static Camera desktopCam;
        private static float nextCamScan;

        public static Camera DesktopCamera
        {
            get
            {
                if (desktopCam != null && desktopCam.isActiveAndEnabled && Time.unscaledTime < nextCamScan) return desktopCam;
                nextCamScan = Time.unscaledTime + 1f;
                desktopCam = null;
                foreach (Camera c in Camera.allCameras)
                    if (c.isActiveAndEnabled && c.targetTexture == null && (desktopCam == null || c.depth > desktopCam.depth))
                        desktopCam = c;
                if (desktopCam == null) desktopCam = Camera.main;
                return desktopCam;
            }
        }

        public static bool MouseRay(out Ray ray)
        {
            Camera cam = DesktopCamera;
            if (cam == null || M == null)
            {
                ray = default;
                return false;
            }
            ray = cam.ScreenPointToRay(M.position.ReadValue());
            return true;
        }

        private static bool AimRay(out Ray ray)
        {
            if (Desktop) return MouseRay(out ray);
            Transform h = Hand;
            if (h == null)
            {
                ray = default;
                return false;
            }
            Vector3 dir = -h.up;
            ray = new Ray(h.position + dir * 0.1f, dir);
            return true;
        }

        private static int pollFrame = -1;
        private static bool fireNow, fireLast, fireDown;

        private static void Poll()
        {
            if (pollFrame == Time.frameCount) return;
            pollFrame = Time.frameCount;
            fireNow = Firing;
            fireDown = fireNow && !fireLast;
            fireLast = fireNow;
        }

        private static bool Begin(out GunHit g, out Ray ray, float range, out RaycastHit info, out bool hit)
        {
            g = default;
            info = default;
            hit = false;
            Poll();
            if (!Util.Ready || !Aiming || !AimRay(out ray))
            {
                ray = default;
                return false;
            }

            hit = Physics.Raycast(ray, out info, range, Util.RaycastMask, QueryTriggerInteraction.Ignore);
            g.Origin = Hand.position;
            g.Direction = ray.direction;
            g.Hit = hit;
            g.Point = hit ? info.point : ray.GetPoint(Mathf.Min(range, 60f));
            g.Normal = hit ? info.normal : -ray.direction;
            g.Fire = fireNow;
            g.FireDown = fireDown;
            return true;
        }

        public static bool World(out GunHit g, float range = 512f)
        {
            if (!Begin(out g, out _, range, out _, out _))
            {
                Hide();
                return false;
            }
            Draw(g.Origin, g.Point, g.Fire, false);
            return true;
        }

        private static VRRig locked;

        public static bool Players(out GunHit g, float range = 512f)
        {
            if (!Begin(out g, out Ray ray, range, out RaycastHit info, out bool hit))
            {
                locked = null;
                Hide();
                return false;
            }

            if (!g.Fire || !Valid(locked)) locked = null;
            VRRig rig = locked != null ? locked : FindRig(ray, hit ? info.distance : range);
            if (g.Fire && rig != null) locked = rig;

            g.Rig = rig;
            g.Hit = rig != null;
            if (rig != null)
            {
                g.Point = HeadOf(rig);
                g.Normal = -ray.direction;
            }
            Draw(g.Origin, g.Point, g.Fire, rig != null);
            return true;
        }

        public static Vector3 HeadOf(VRRig r) =>
            r.head != null && r.head.rigTarget != null ? r.head.rigTarget.position : r.transform.position + Vector3.up * 0.3f;

        public static string NameOf(VRRig r)
        {
            if (r == null) return "?";
            if (r.Creator != null && !string.IsNullOrEmpty(r.Creator.NickName)) return r.Creator.NickName;
            return string.IsNullOrEmpty(r.playerNameVisible) ? "?" : r.playerNameVisible;
        }

        public static bool Valid(VRRig r) => r != null && r.gameObject.activeInHierarchy && Util.OtherRigs.Contains(r);

        private static VRRig FindRig(Ray ray, float maxDistance)
        {
            VRRig best = null;
            float nearest = maxDistance + 0.5f;
            foreach (VRRig rig in Util.OtherRigs)
            {
                float size = Mathf.Clamp(rig.scaleFactor, 0.5f, 2f);
                for (int i = 0; i < 2; i++)
                {
                    Vector3 p = i == 0 ? rig.transform.position : HeadOf(rig);
                    float along = Vector3.Dot(p - ray.origin, ray.direction);
                    if (along <= 0f || along > nearest) continue;
                    float radius = Mathf.Max(0.35f * size, along * 0.035f);
                    if ((p - ray.GetPoint(along)).sqrMagnitude > radius * radius) continue;
                    nearest = along;
                    best = rig;
                }
            }
            return best;
        }

        private static LineRenderer line;
        private static GameObject dot;

        private static void Draw(Vector3 from, Vector3 to, bool fire, bool target)
        {
            Color c = Util.GunColor;
            if (target) c = Color.Lerp(c, new Color(1f, 0.3f, 0.35f), 0.6f);
            if (fire) c = Theme.Lighten(c, 0.45f);

            if (line == null)
            {
                line = Util.NewLine(0.01f, c);
                line.name = "RivenGunLine";
            }
            if (dot == null)
            {
                dot = Util.Primitive(PrimitiveType.Sphere, to, Vector3.one * 0.1f, c);
                dot.name = "RivenGunDot";
            }

            line.gameObject.SetActive(true);
            dot.SetActive(true);
            Util.SetLine(line, from, to, c);
            line.startWidth = target ? 0.014f : 0.01f;
            line.endWidth = target ? 0.02f : 0.006f;

            float pulse = target ? 1f + 0.2f * Mathf.Sin(Time.time * 12f) : 1f;
            dot.transform.position = to;
            dot.transform.localScale = Vector3.one * ((fire ? 0.14f : 0.1f) * (target ? 1.6f : 1f) * pulse);
            dot.GetComponent<Renderer>().material.color = c;
            dot.layer = Util.VisualLayer;
        }

        public static void Hide()
        {
            if (line != null) line.gameObject.SetActive(false);
            if (dot != null) dot.SetActive(false);
        }

        public static void Cleanup()
        {
            locked = null;
            if (line != null) Object.Destroy(line.gameObject);
            if (dot != null) Object.Destroy(dot);
            line = null;
            dot = null;
        }
    }
}
