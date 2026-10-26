using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class Movement
    {
        private static float Dt => Time.deltaTime;
        private static float Speed => Settings.FlySpeed;

        private static void FlyStep(Vector3 dir, float speed)
        {
            if (!Util.Ready) return;
            Util.Player.transform.position += dir * (speed * Dt);
            Util.Body.velocity = Vector3.zero;
        }

        private static GameObject leftPlat, rightPlat;

        private static void Platforms(bool left, bool right, bool visible)
        {
            if (!Util.Ready) return;
            HandlePlatform(ref leftPlat, left, Util.LeftHand, visible);
            HandlePlatform(ref rightPlat, right, Util.RightHand, visible);
        }

        private static void HandlePlatform(ref GameObject plat, bool held, Transform hand, bool visible)
        {
            if (held && plat == null)
            {
                plat = Util.Primitive(PrimitiveType.Cube, hand.position - Vector3.up * 0.06f, new Vector3(0.28f, 0.015f, 0.38f), Settings.PlatformTint, true);
                plat.transform.rotation = Quaternion.Euler(0f, hand.eulerAngles.y, 0f);
                plat.GetComponent<Renderer>().enabled = visible;
            }
            else if (!held && plat != null)
            {
                Object.Destroy(plat);
                plat = null;
            }
            if (plat != null && visible) plat.GetComponent<Renderer>().material.color = Settings.PlatformTint;
        }

        public static void ClearPlatforms()
        {
            if (leftPlat != null) Object.Destroy(leftPlat);
            if (rightPlat != null) Object.Destroy(rightPlat);
            leftPlat = rightPlat = null;
        }

        [MenuButton("Platforms", Category = "Movement", Order = 0, OnDisable = nameof(ClearPlatforms), Tooltip = "Hold grip to make a platform under your hand")]
        public static void PlatformsMod() => Platforms(Util.LeftGrip, Util.RightGrip, true);

        [MenuButton("Invisible Platforms", Category = "Movement", Order = 1, OnDisable = nameof(ClearPlatforms), Tooltip = "Platforms nobody can see")]
        public static void InvisiblePlatforms() => Platforms(Util.LeftGrip, Util.RightGrip, false);

        [MenuButton("Trigger Platforms", Category = "Movement", Order = 2, OnDisable = nameof(ClearPlatforms), Tooltip = "Hold trigger to make a platform")]
        public static void TriggerPlatforms() => Platforms(Util.LeftTrigger, Util.RightTrigger, true);

        [MenuButton("Fly", Category = "Movement", Order = 10, Tooltip = "Hold A to fly where you look")]
        public static void Fly()
        {
            if (Util.A) FlyStep(Util.Head.forward, Speed);
        }

        [MenuButton("Trigger Fly", Category = "Movement", Order = 11, Tooltip = "Hold right trigger to fly")]
        public static void TriggerFly()
        {
            if (Util.RightTrigger) FlyStep(Util.Head.forward, Speed);
        }

        [MenuButton("Velocity Fly", Category = "Movement", Order = 12, Tooltip = "Hold A to fly with smooth momentum")]
        public static void VelocityFly()
        {
            if (!Util.Ready || !Util.A) return;
            Util.Body.velocity = Vector3.Lerp(Util.Body.velocity, Util.Head.forward * Speed, Dt * 4f);
        }

        [MenuButton("Hand Fly", Category = "Movement", Order = 13, Tooltip = "Hold A to fly where your right hand points")]
        public static void HandFly()
        {
            if (Util.A) FlyStep(-Util.RightHand.up, Speed);
        }

        [MenuButton("Joystick Fly", Category = "Movement", Order = 14, Tooltip = "Left stick moves, right stick goes up and down")]
        public static void JoystickFly()
        {
            if (!Util.Ready) return;
            Vector2 l = Util.LeftStick, r = Util.RightStick;
            Vector3 dir = Util.Head.forward * l.y + Util.Head.right * l.x + Vector3.up * r.y;
            if (dir.sqrMagnitude < 0.01f)
            {
                Util.AntiGravity(1f);
                Util.Body.velocity = Vector3.zero;
                return;
            }
            FlyStep(dir, Speed);
        }

        [MenuButton("WASD Fly", Category = "Movement", Order = 15, Tooltip = "PC: WASD to move, Space up, Ctrl down, Shift fast")]
        public static void WasdFly()
        {
            if (!Util.Ready) return;
            Transform cam = Camera.main != null ? Camera.main.transform : Util.Head;
            Vector3 dir = Vector3.zero;
            if (Util.Key(Key.W)) dir += cam.forward;
            if (Util.Key(Key.S)) dir -= cam.forward;
            if (Util.Key(Key.D)) dir += cam.right;
            if (Util.Key(Key.A)) dir -= cam.right;
            if (Util.Key(Key.Space)) dir += Vector3.up;
            if (Util.Key(Key.LeftCtrl)) dir -= Vector3.up;
            float sp = Speed * (Util.Key(Key.LeftShift) ? 2f : 1f);
            if (dir.sqrMagnitude > 0.01f) FlyStep(dir.normalized, sp);
            else
            {
                Util.AntiGravity(1f);
                Util.Body.velocity = Vector3.zero;
            }
        }

        private static readonly List<MeshCollider> noclipped = new List<MeshCollider>();

        public static void NoclipOn()
        {
            foreach (MeshCollider c in Object.FindObjectsOfType<MeshCollider>())
            {
                if (!c.enabled) continue;
                c.enabled = false;
                noclipped.Add(c);
            }
        }

        public static void NoclipOff()
        {
            foreach (MeshCollider c in noclipped)
                if (c != null) c.enabled = true;
            noclipped.Clear();
        }

        [MenuButton("Noclip", Category = "Movement", Order = 16, OnEnable = nameof(NoclipOn), OnDisable = nameof(NoclipOff), Tooltip = "Float through walls, pair it with any fly mod")]
        public static void Noclip()
        {
            if (!Util.Ready) return;
            Util.AntiGravity(1f);
            Util.Body.velocity = Vector3.Lerp(Util.Body.velocity, Vector3.zero, Dt * 4f);
        }

        [MenuButton("Noclip Fly", Category = "Movement", Order = 17, OnEnable = nameof(NoclipOn), OnDisable = nameof(NoclipOff), Tooltip = "Fly through walls with A")]
        public static void NoclipFly()
        {
            if (!Util.Ready) return;
            if (Util.A) FlyStep(Util.Head.forward, Speed);
            else
            {
                Util.AntiGravity(1f);
                Util.Body.velocity = Vector3.zero;
            }
        }

        [MenuButton("Hover", Category = "Movement", Order = 20, Tooltip = "Hold left grip to float in place")]
        public static void Hover()
        {
            if (!Util.Ready || !Util.LeftGrip) return;
            Util.AntiGravity(1f);
            Util.Body.velocity = Vector3.Lerp(Util.Body.velocity, Vector3.zero, Dt * 6f);
        }

        [MenuButton("Low Gravity", Category = "Movement", Order = 22, Tooltip = "Gravity at 40%")]
        public static void LowGravity() => Util.AntiGravity(0.6f);

        [MenuButton("Moon Gravity", Category = "Movement", Order = 23, Tooltip = "Gravity like the moon")]
        public static void MoonGravity() => Util.AntiGravity(0.83f);

        [MenuButton("High Gravity", Category = "Movement", Order = 24, Tooltip = "Double gravity")]
        public static void HighGravity() => Util.AntiGravity(-1f);

        [MenuButton("Reverse Gravity", Category = "Movement", Order = 25, Tooltip = "Fall up")]
        public static void ReverseGravity() => Util.AntiGravity(2f);

        [MenuButton("Slow Fall", Category = "Movement", Order = 26, Tooltip = "Fall slowly like a feather")]
        public static void SlowFall()
        {
            if (!Util.Ready) return;
            Vector3 v = Util.Body.velocity;
            if (v.y < -2.5f) Util.Body.velocity = new Vector3(v.x, -2.5f, v.z);
        }

        private static float baseJump = -1f, baseMult = -1f, setJump, setMult;

        internal static void Boost(float mult)
        {
            if (!Util.Ready) return;
            if (baseJump < 0f || !Mathf.Approximately(Util.Player.maxJumpSpeed, setJump)) baseJump = Util.Player.maxJumpSpeed;
            if (baseMult < 0f || !Mathf.Approximately(Util.Player.jumpMultiplier, setMult)) baseMult = Util.Player.jumpMultiplier;
            setJump = baseJump * mult;
            setMult = baseMult * Mathf.Lerp(1f, mult, 0.5f);
            Util.Player.maxJumpSpeed = setJump;
            Util.Player.jumpMultiplier = setMult;
        }

        public static void BoostOff()
        {
            if (Util.Player != null && baseJump > 0f)
            {
                Util.Player.maxJumpSpeed = baseJump;
                Util.Player.jumpMultiplier = baseMult;
            }
            baseJump = baseMult = -1f;
        }

        [MenuButton("Super Speed", Category = "Movement", Order = 31, OnDisable = nameof(BoostOff), Tooltip = "Very fast running")]
        public static void SuperSpeed() => Boost(2.8f);

        [MenuButton("Air Jump", Category = "Movement", Order = 33, Tooltip = "Press A to jump again mid air")]
        public static void AirJump()
        {
            if (Util.Ready && Util.Pressed("airjump", Util.A))
                Util.Body.velocity = new Vector3(Util.Body.velocity.x, 7f, Util.Body.velocity.z);
        }

        [MenuButton("Leap", Category = "Movement", Order = 35, Tooltip = "Press B to leap forward and up")]
        public static void Leap()
        {
            if (!Util.Ready || !Util.Pressed("leap", Util.B)) return;
            Vector3 f = Util.Head.forward;
            f.y = 0f;
            Util.Body.velocity = f.normalized * 12f + Vector3.up * 7f;
        }

        [MenuButton("Blink", Category = "Movement", Order = 36, Tooltip = "Press A to teleport a few meters forward")]
        public static void Blink()
        {
            if (!Util.Ready || !Util.Pressed("blink", Util.A)) return;
            Vector3 dir = Util.Head.forward;
            float dist = 6f;
            if (Physics.Raycast(Util.Head.position, dir, out RaycastHit hit, dist, Util.RaycastMask))
                dist = Mathf.Max(0f, hit.distance - 0.6f);
            Util.Teleport(Util.Player.transform.position + dir * dist);
        }

        [MenuButton("Jetpack", Category = "Movement", Order = 37, Tooltip = "Hold both triggers to fly up")]
        public static void Jetpack()
        {
            if (Util.Ready && Util.LeftTrigger && Util.RightTrigger)
                Util.Body.AddForce(Vector3.up * 22f, ForceMode.Acceleration);
        }

        [MenuButton("Rocket", Category = "Movement", Order = 38, Tooltip = "Hold B to boost where you look")]
        public static void Rocket()
        {
            if (Util.Ready && Util.B)
                Util.Body.AddForce(Util.Head.forward * 28f, ForceMode.Acceleration);
        }

        [MenuButton("Hand Thrusters", Category = "Movement", Order = 39, Tooltip = "Hold grips to push where your hands point")]
        public static void HandThrusters()
        {
            if (!Util.Ready) return;
            if (Util.RightGrip) Util.Body.AddForce(-Util.RightHand.up * 14f, ForceMode.Acceleration);
            if (Util.LeftGrip) Util.Body.AddForce(-Util.LeftHand.up * 14f, ForceMode.Acceleration);
        }

        [MenuButton("Car Monke", Category = "Movement", Order = 40, Tooltip = "Right stick drives you forward and back")]
        public static void CarMonke()
        {
            if (!Util.Ready) return;
            float y = Util.RightStick.y;
            if (Mathf.Abs(y) < 0.1f) return;
            Vector3 f = Util.Head.forward;
            f.y = 0f;
            Vector3 v = f.normalized * (y * Speed * 0.8f);
            Util.Body.velocity = new Vector3(v.x, Util.Body.velocity.y, v.z);
        }

        [MenuButton("Up And Down", Category = "Movement", Order = 41, Tooltip = "Right stick moves you up and down")]
        public static void UpAndDown()
        {
            if (!Util.Ready) return;
            float y = Util.RightStick.y;
            if (Mathf.Abs(y) < 0.1f) return;
            Util.AntiGravity(1f);
            Util.Body.velocity = new Vector3(Util.Body.velocity.x, y * Speed * 0.6f, Util.Body.velocity.z);
        }

        private static Vector3 wallNormal;
        private static float wallTime;

        [MenuButton("Wall Walk", Category = "Movement", Order = 43, Tooltip = "Touch a wall and hold grip to stick to it")]
        public static void WallWalk()
        {
            if (!Util.Ready) return;
            foreach (Transform hand in new[] { Util.LeftHand, Util.RightHand })
            {
                foreach (Vector3 d in new[] { -hand.up, hand.right, -hand.right, hand.forward, -hand.forward, Vector3.down })
                {
                    if (Physics.Raycast(hand.position, d, out RaycastHit hit, 0.25f, Util.RaycastMask))
                    {
                        wallNormal = hit.normal;
                        wallTime = Time.time;
                    }
                }
            }
            if ((Util.LeftGrip || Util.RightGrip) && Time.time - wallTime < 1.2f)
            {
                Util.AntiGravity(1f);
                Util.Body.AddForce(-wallNormal * 9.81f, ForceMode.Acceleration);
            }
        }

        private static float savedSlide = -1f;

        public static void NoSlipOn()
        {
            if (Util.Player == null) return;
            savedSlide = Util.Player.slideControl;
        }

        public static void NoSlipOff()
        {
            if (Util.Player != null && savedSlide >= 0f) Util.Player.slideControl = savedSlide;
        }

        [MenuButton("No Slip", Category = "Movement", Order = 44, OnEnable = nameof(NoSlipOn), OnDisable = nameof(NoSlipOff), Tooltip = "Full control on slippery surfaces")]
        public static void NoSlip()
        {
            if (Util.Player != null) Util.Player.slideControl = 1f;
        }

        [MenuButton("Freeze Mid Air", Category = "Movement", Order = 45, Tooltip = "Hold B to freeze in place")]
        public static void FreezeMidAir()
        {
            if (!Util.Ready || !Util.B) return;
            Util.AntiGravity(1f);
            Util.Body.velocity = Vector3.zero;
        }

        [MenuButton("Teleport Gun", Category = "Guns", Order = 0, OnDisable = nameof(GunOff), Tooltip = "Aim with grip or right mouse, trigger or left click to teleport")]
        public static void TeleportGun()
        {
            if (GunLib.World(out GunHit g) && g.Hit && g.FireDown)
                Util.Teleport(g.Point + g.Normal * 0.8f);
        }

        public static void GunOff() => GunLib.Hide();

        private static Vector3? grapplePoint;
        private static LineRenderer grappleLine;

        [MenuButton("Grapple Gun", Category = "Guns", Order = 1, OnDisable = nameof(GrappleOff), Tooltip = "Aim, then hold trigger or left click to reel in")]
        public static void GrappleGun()
        {
            bool aiming = GunLib.World(out GunHit g);
            if (aiming && g.Fire && grapplePoint == null && g.Hit) grapplePoint = g.Point;
            if (!aiming || !g.Fire || !Util.Ready) grapplePoint = null;

            if (grapplePoint == null)
            {
                if (grappleLine != null) grappleLine.gameObject.SetActive(false);
                return;
            }

            Vector3 to = grapplePoint.Value - Util.BodyT.position;
            Util.Body.AddForce(to.normalized * Mathf.Min(40f, to.magnitude * 3f), ForceMode.Acceleration);
            Util.AntiGravity(0.4f);

            if (grappleLine == null) grappleLine = Util.NewLine(0.02f, Util.GunColor);
            grappleLine.gameObject.SetActive(true);
            Util.SetLine(grappleLine, GunLib.Hand.position, grapplePoint.Value, Util.GunColor);
        }

        public static void GrappleOff()
        {
            grapplePoint = null;
            GunLib.Hide();
            if (grappleLine != null) grappleLine.gameObject.SetActive(false);
        }

        private static Vector3? checkpoint;
        private static GameObject checkpointMarker;

        [MenuButton("Checkpoint", Category = "Movement", Order = 52, OnDisable = nameof(CheckpointOff), Tooltip = "Y saves your spot, X teleports back")]
        public static void Checkpoint()
        {
            if (!Util.Ready) return;
            if (Util.Pressed("cpset", Util.Y))
            {
                checkpoint = Util.Player.transform.position;
                if (checkpointMarker == null) checkpointMarker = Util.Primitive(PrimitiveType.Sphere, Util.BodyT.position, Vector3.one * 0.25f, Theme.Accent);
                checkpointMarker.transform.position = Util.BodyT.position;
                Notify.Push("Checkpoint saved");
            }
            if (checkpoint != null && Util.Pressed("cpgo", Util.X)) Util.Teleport(checkpoint.Value);
            if (checkpointMarker != null)
            {
                checkpointMarker.GetComponent<Renderer>().material.color = Theme.WithA(Theme.Accent, 0.6f + 0.3f * Mathf.Sin(Time.time * 4f));
                checkpointMarker.layer = Util.VisualLayer;
            }
        }

        public static void CheckpointOff()
        {
            checkpoint = null;
            if (checkpointMarker != null) Object.Destroy(checkpointMarker);
        }

        [MenuButton("Slow Motion", Category = "Movement", Order = 60, OnEnable = nameof(SlowOn), OnDisable = nameof(SlowOff), Tooltip = "Everything moves at half speed for you")]
        public static void SlowMotion() { }

        private static float savedFixed = 0.02f;

        public static void SlowOn()
        {
            savedFixed = Time.fixedDeltaTime;
            Time.timeScale = 0.5f;
            Time.fixedDeltaTime = savedFixed * 0.5f;
        }

        public static void SlowOff()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = savedFixed;
        }
    }
}
