using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class Guns
    {
        public static void GunOff() => GunLib.Hide();

        private static Vector3 Behind(VRRig r, float distance)
        {
            Vector3 f = r.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
            return r.transform.position - f.normalized * distance + Vector3.up * 0.2f;
        }

        [MenuButton("Teleport To Player Gun", Category = "Guns", Order = 10, OnDisable = nameof(GunOff), Tooltip = "Aim at a player and fire to teleport behind them")]
        public static void TeleportToPlayerGun()
        {
            if (GunLib.Players(out GunHit g) && g.Rig != null && g.FireDown)
                Util.Teleport(Behind(g.Rig, 1.2f));
        }

        private static VRRig following;

        [MenuButton("Follow Player Gun", Category = "Guns", Order = 11, OnDisable = nameof(FollowOff), Tooltip = "Fire at a player to follow them, fire at nothing to stop")]
        public static void FollowPlayerGun()
        {
            if (GunLib.Players(out GunHit g) && g.FireDown)
            {
                following = g.Rig;
                Notify.Push(following != null ? "Following " + GunLib.NameOf(following) : "Stopped following");
            }

            if (!Util.Ready || following == null) return;
            if (!GunLib.Valid(following))
            {
                following = null;
                Notify.Push("Player left");
                return;
            }

            Vector3 to = Behind(following, 1.5f) - Util.BodyT.position;
            if (to.magnitude > 20f)
            {
                Util.Teleport(Behind(following, 1.5f));
                return;
            }
            Util.AntiGravity(1f);
            Util.Body.velocity = Vector3.ClampMagnitude(to * 4f, 25f);
        }

        public static void FollowOff()
        {
            following = null;
            GunLib.Hide();
        }

        private static VRRig infoTarget;

        private static string InfoLine()
        {
            if (!GunLib.Valid(infoTarget)) return "Aim at a player";
            float d = Util.BodyT != null ? Vector3.Distance(infoTarget.transform.position, Util.BodyT.position) : 0f;
            string tag = Util.IsTagged(infoTarget) ? "<color=#FF5A66>tagged</color>" : "<color=#5AFF8A>not tagged</color>";
            return $"{GunLib.NameOf(infoTarget)}  {d:0.0}m  {tag}";
        }

        public static void InfoOn() => Hud.Show("guninfo", InfoLine);

        public static void InfoOff()
        {
            Hud.Hide("guninfo");
            infoTarget = null;
            GunLib.Hide();
        }

        [MenuButton("Player Info Gun", Category = "Guns", Order = 12, OnEnable = nameof(InfoOn), OnDisable = nameof(InfoOff), Tooltip = "Aim at a player to see their name, distance and tag status")]
        public static void PlayerInfoGun()
        {
            if (!GunLib.Players(out GunHit g)) return;
            if (g.Rig != null) infoTarget = g.Rig;
            if (g.FireDown && g.Rig != null)
            {
                GUIUtility.systemCopyBuffer = GunLib.NameOf(g.Rig);
                Notify.Push("Copied name " + GunLib.NameOf(g.Rig));
            }
        }

        private static VRRig spectating;
        private static Camera spectateCam;

        [MenuButton("Spectate Gun", Category = "Guns", Order = 13, OnDisable = nameof(SpectateOff), Tooltip = "Fire at a player to watch them on your PC screen, fire at nothing to stop")]
        public static void SpectateGun()
        {
            if (GunLib.Players(out GunHit g) && g.FireDown)
            {
                spectating = g.Rig;
                Notify.Push(spectating != null ? "Spectating " + GunLib.NameOf(spectating) : "Stopped spectating");
            }

            if (!GunLib.Valid(spectating))
            {
                spectating = null;
                if (spectateCam != null) spectateCam.gameObject.SetActive(false);
                return;
            }

            if (spectateCam == null)
            {
                spectateCam = new GameObject("RivenSpectateCam").AddComponent<Camera>();
                Camera src = GunLib.DesktopCamera;
                if (src != null) spectateCam.CopyFrom(src);
                spectateCam.targetTexture = null;
                spectateCam.stereoTargetEye = StereoTargetEyeMask.None;
                spectateCam.depth = 90f;
            }
            spectateCam.gameObject.SetActive(true);

            Vector3 head = GunLib.HeadOf(spectating);
            Vector3 want = Behind(spectating, 2.2f) + Vector3.up * 0.6f;
            Transform t = spectateCam.transform;
            t.position = Vector3.Lerp(t.position, want, 1f - Mathf.Exp(-8f * Time.deltaTime));
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(head - t.position), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        public static void SpectateOff()
        {
            spectating = null;
            if (spectateCam != null) Object.Destroy(spectateCam.gameObject);
            spectateCam = null;
            GunLib.Hide();
        }
    }
}
