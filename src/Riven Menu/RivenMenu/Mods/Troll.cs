using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivenMenu.Mods
{
    public static class Troll
    {
        private static Vector3 RigOffset => Util.LocalRig != null && Util.BodyT != null
            ? Util.LocalRig.transform.position - Util.BodyT.position
            : Vector3.zero;

        private static Quaternion Yaw(Vector3 forward)
        {
            forward.y = 0f;
            return forward.sqrMagnitude < 0.001f ? Quaternion.identity : Quaternion.LookRotation(forward.normalized);
        }

        private static void Mimic(VRRig target, Vector3 offset)
        {
            if (target == null || Util.LocalRig == null) return;
            Util.PlaceRig(target.transform.position + offset, target.transform.rotation);
            Quaternion head = target.head?.rigTarget != null ? target.head.rigTarget.rotation : target.transform.rotation;
            Util.PoseRig(GunLib.HeadOf(target) + offset, head, target.leftHandTransform.position + offset, target.rightHandTransform.position + offset);
        }

        private static bool ghost;

        [MenuButton("Ghost Monke", Category = "Troll", Order = 0, OnDisable = nameof(GhostOff), Tooltip = "B or G freezes your body in place while you keep moving")]
        public static void GhostMonke()
        {
            if (Util.LocalRig == null) return;
            if (Util.Pressed("troll_ghost", Util.B || Util.HeldKey(Key.G)))
            {
                ghost = !ghost;
                if (ghost) Util.TakeRig("ghost");
                else Util.ReleaseRig("ghost");
                Notify.Push(ghost ? "Ghost on" : "Ghost off");
            }
        }

        public static void GhostOff()
        {
            ghost = false;
            Util.ReleaseRig("ghost");
        }

        private static bool invisible;

        [MenuButton("Invisible Monke", Category = "Troll", Order = 1, OnDisable = nameof(InvisibleOff), Tooltip = "B or H hides your body under the map")]
        public static void InvisibleMonke()
        {
            if (!Util.Ready || Util.LocalRig == null) return;
            if (Util.Pressed("troll_invis", Util.B || Util.HeldKey(Key.H)))
            {
                invisible = !invisible;
                if (!invisible) Util.ReleaseRig("invis");
                Notify.Push(invisible ? "Invisible on" : "Invisible off");
            }
            if (!invisible) return;
            Util.TakeRig("invis");
            Util.PlaceRig(Util.BodyT.position + Vector3.down * 120f, Util.LocalRig.transform.rotation);
        }

        public static void InvisibleOff()
        {
            invisible = false;
            Util.ReleaseRig("invis");
        }

        [MenuButton("Rig Gun", Category = "Troll", Order = 10, OnDisable = nameof(RigGunOff), Tooltip = "Aim and hold fire to put your body there")]
        public static void RigGun()
        {
            if (GunLib.World(out GunHit g) && g.Fire)
            {
                Util.TakeRig("riggun");
                Util.PlaceRig(g.Point + g.Normal * 0.4f + Vector3.up * 0.3f, Yaw(Util.Head.position - g.Point));
                return;
            }
            Util.ReleaseRig("riggun");
        }

        public static void RigGunOff()
        {
            Util.ReleaseRig("riggun");
            GunLib.Hide();
        }

        private static VRRig orbitTarget;

        [MenuButton("Orbit Gun", Category = "Troll", Order = 11, OnDisable = nameof(OrbitOff), Tooltip = "Fire at a player and your body circles them, fire at nothing to stop")]
        public static void OrbitGun()
        {
            if (GunLib.Players(out GunHit g) && g.FireDown)
            {
                orbitTarget = g.Rig;
                Notify.Push(orbitTarget != null ? "Orbiting " + GunLib.NameOf(orbitTarget) : "Stopped orbiting");
            }
            if (!GunLib.Valid(orbitTarget))
            {
                orbitTarget = null;
                Util.ReleaseRig("orbit");
                return;
            }

            float a = Time.time * 3f;
            Vector3 center = orbitTarget.transform.position;
            Vector3 p = center + new Vector3(Mathf.Cos(a), 0.25f + Mathf.Sin(a * 2f) * 0.2f, Mathf.Sin(a)) * 1.3f;
            Util.TakeRig("orbit");
            Util.PlaceRig(p, Yaw(center - p));
        }

        public static void OrbitOff()
        {
            orbitTarget = null;
            Util.ReleaseRig("orbit");
            GunLib.Hide();
        }

        private static VRRig copyTarget;

        [MenuButton("Copy Movement Gun", Category = "Troll", Order = 12, OnDisable = nameof(CopyOff), Tooltip = "Fire at a player and your body copies everything they do")]
        public static void CopyMovementGun()
        {
            if (GunLib.Players(out GunHit g) && g.FireDown)
            {
                copyTarget = g.Rig;
                Notify.Push(copyTarget != null ? "Copying " + GunLib.NameOf(copyTarget) : "Stopped copying");
            }
            if (!GunLib.Valid(copyTarget))
            {
                copyTarget = null;
                Util.ReleaseRig("copy");
                return;
            }

            Util.TakeRig("copy");
            Vector3 side = copyTarget.transform.right;
            side.y = 0f;
            Mimic(copyTarget, side.normalized * 0.9f);
        }

        public static void CopyOff()
        {
            copyTarget = null;
            Util.ReleaseRig("copy");
            GunLib.Hide();
        }

        private static VRRig annoyTarget;

        [MenuButton("Annoy Gun", Category = "Troll", Order = 13, OnDisable = nameof(AnnoyOff), Tooltip = "Fire at a player and your body sticks in their face")]
        public static void AnnoyGun()
        {
            if (GunLib.Players(out GunHit g) && g.FireDown)
            {
                annoyTarget = g.Rig;
                Notify.Push(annoyTarget != null ? "Annoying " + GunLib.NameOf(annoyTarget) : "Stopped");
            }
            if (!GunLib.Valid(annoyTarget))
            {
                annoyTarget = null;
                Util.ReleaseRig("annoy");
                return;
            }

            Vector3 head = GunLib.HeadOf(annoyTarget);
            Vector3 fwd = annoyTarget.head?.rigTarget != null ? annoyTarget.head.rigTarget.forward : annoyTarget.transform.forward;
            Vector3 p = head + fwd * 0.45f + new Vector3(Mathf.Sin(Time.time * 9f), Mathf.Cos(Time.time * 7f), 0f) * 0.06f - Vector3.up * 0.3f;
            Util.TakeRig("annoy");
            Util.PlaceRig(p, Yaw(head - p));
        }

        public static void AnnoyOff()
        {
            annoyTarget = null;
            Util.ReleaseRig("annoy");
            GunLib.Hide();
        }

        private static float nextBee;
        private static VRRig beeTarget;

        [MenuButton("Bees", Category = "Troll", Order = 20, OnDisable = nameof(BeesOff), Tooltip = "Your body buzzes from player to player")]
        public static void Bees()
        {
            VRRig[] rigs = Util.OtherRigs.ToArray();
            if (rigs.Length == 0)
            {
                Util.ReleaseRig("bees");
                return;
            }
            if (Time.time >= nextBee || !GunLib.Valid(beeTarget))
            {
                nextBee = Time.time + 0.45f;
                beeTarget = rigs[Random.Range(0, rigs.Length)];
            }
            Vector3 p = beeTarget.transform.position + Random.insideUnitSphere * 0.5f;
            Util.TakeRig("bees");
            Util.PlaceRig(p, Random.rotation);
        }

        public static void BeesOff()
        {
            beeTarget = null;
            Util.ReleaseRig("bees");
        }

        [MenuButton("Spaz Monke", Category = "Troll", Order = 21, OnDisable = nameof(SpazOff), Tooltip = "Your head and arms flail everywhere")]
        public static void SpazMonke()
        {
            if (!Util.Ready || Util.LocalRig == null) return;
            Util.TakeRig("spaz");
            Vector3 body = Util.BodyT.position + RigOffsetCache;
            Util.PlaceRig(body, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            Util.PoseRig(body, Random.rotation, body + Random.insideUnitSphere * 0.7f, body + Random.insideUnitSphere * 0.7f);
        }

        private static Vector3 rigOffsetCache = new Vector3(0f, -0.15f, 0f);
        private static Vector3 RigOffsetCache
        {
            get
            {
                if (!Util.RigTaken && Util.LocalRig != null && Util.BodyT != null) rigOffsetCache = RigOffset;
                return rigOffsetCache;
            }
        }

        public static void SpazOff() => Util.ReleaseRig("spaz");

        private static float heliAngle;

        [MenuButton("Helicopter Monke", Category = "Troll", Order = 22, OnDisable = nameof(HeliOff), Tooltip = "Your body spins like a helicopter with arms out")]
        public static void HelicopterMonke()
        {
            if (!Util.Ready || Util.LocalRig == null) return;
            Util.TakeRig("heli");
            heliAngle = Mathf.Repeat(heliAngle + Time.deltaTime * 900f, 360f);
            Quaternion rot = Quaternion.Euler(0f, heliAngle, 0f);
            Vector3 body = Util.BodyT.position + RigOffsetCache;
            Util.PlaceRig(body, rot);
            Vector3 side = rot * Vector3.right;
            Util.PoseRig(body + Vector3.up * 0.3f, rot, body + Vector3.up * 0.2f - side * 0.9f, body + Vector3.up * 0.2f + side * 0.9f);
        }

        public static void HeliOff() => Util.ReleaseRig("heli");

        private static float lagLiveUntil, lagFrozenUntil;

        [MenuButton("Fake Lag", Category = "Troll", Order = 23, OnDisable = nameof(LagOff), Tooltip = "Others see you teleporting around like you have bad wifi")]
        public static void FakeLag()
        {
            if (!Util.Ready || Util.LocalRig == null) return;
            float now = Time.time;
            if (now < lagLiveUntil)
            {
                Util.ReleaseRig("lag");
                return;
            }
            if (now < lagFrozenUntil)
            {
                Util.TakeRig("lag");
                return;
            }
            lagLiveUntil = now + 0.1f;
            lagFrozenUntil = lagLiveUntil + Random.Range(0.25f, 0.6f);
            Util.ReleaseRig("lag");
        }

        public static void LagOff() => Util.ReleaseRig("lag");

        [MenuButton("Puppet Monke", Category = "Troll", Order = 24, OnDisable = nameof(PuppetOff), Tooltip = "Hold right grip to carry your body around with your hand")]
        public static void PuppetMonke()
        {
            if (!Util.Ready || Util.LocalRig == null) return;
            if (!Util.RightGrip)
            {
                Util.ReleaseRig("puppet");
                return;
            }
            Util.TakeRig("puppet");
            Vector3 p = Util.RightHand.position - Util.RightHand.up * 0.5f;
            Util.PlaceRig(p, Yaw(Util.Head.position - p));
        }

        public static void PuppetOff() => Util.ReleaseRig("puppet");

        [MenuButton("Upside Down Body", Category = "Troll", Order = 25, OnDisable = nameof(UpsideOff), Tooltip = "Your whole body walks around upside down")]
        public static void UpsideDownBody()
        {
            if (!Util.Ready || Util.LocalRig == null) return;
            Util.TakeRig("upside");
            Vector3 body = Util.BodyT.position + RigOffsetCache;
            Quaternion rot = Yaw(Util.Head.forward) * Quaternion.Euler(0f, 0f, 180f);
            Util.PlaceRig(body + Vector3.up * 0.4f, rot);
            Util.PoseRig(body, Util.Head.rotation * Quaternion.Euler(0f, 0f, 180f), Util.LeftHand.position, Util.RightHand.position);
        }

        public static void UpsideOff() => Util.ReleaseRig("upside");

        [MenuButton("Release Body", ButtonType.Button, Category = "Troll", Order = 40, Tooltip = "Gives your body back if anything gets stuck")]
        public static void ReleaseBody()
        {
            foreach (string name in new[] { "Ghost Monke", "Invisible Monke", "Rig Gun", "Orbit Gun", "Copy Movement Gun", "Annoy Gun", "Bees", "Spaz Monke", "Helicopter Monke", "Fake Lag", "Puppet Monke", "Upside Down Body", "Tag Self" })
                Buttons.SetEnabled(name, false);
            foreach (string owner in new[] { "ghost", "invis", "riggun", "orbit", "copy", "annoy", "bees", "spaz", "heli", "lag", "puppet", "upside", "tagself" })
                Util.ReleaseRig(owner);
        }
    }
}
