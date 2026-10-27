using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class Advantages
    {
        private static float Dt => Time.deltaTime;

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static int groundFrame = -1;
        private static bool grounded;

        private static bool Grounded
        {
            get
            {
                if (groundFrame == Time.frameCount) return grounded;
                groundFrame = Time.frameCount;
                grounded = Util.Ready && (Physics.Raycast(Util.BodyT.position, Vector3.down, 0.9f, Util.RaycastMask)
                                          || Physics.Raycast(Util.LeftHand.position, Vector3.down, 0.12f, Util.RaycastMask)
                                          || Physics.Raycast(Util.RightHand.position, Vector3.down, 0.12f, Util.RaycastMask));
                return grounded;
            }
        }

        private static Vector3 StickDir(Vector2 stick)
        {
            Vector3 f = Flat(Util.Head.forward).normalized, r = Flat(Util.Head.right).normalized;
            return f * stick.y + r * stick.x;
        }

        private static void Buzz(float amp = 0.25f, float time = 0.06f)
        {
            if (Util.Tagger == null) return;
            Util.Tagger.StartVibration(true, amp, time);
            Util.Tagger.StartVibration(false, amp, time);
        }

        private class Preset
        {
            public string Name;
            public float Speed, Jump, Dash, Air, Arms, Sling;
        }

        private static readonly Preset[] Presets =
        {
            new Preset { Name = "Custom",  Speed = 0f,    Jump = 14f, Dash = 20f, Air = 12f, Arms = 1.25f, Sling = 1f },
            new Preset { Name = "Legit",   Speed = 1.15f, Jump = 10f, Dash = 13f, Air = 6f,  Arms = 1.1f,  Sling = 0.7f },
            new Preset { Name = "Comp",    Speed = 1.35f, Jump = 13f, Dash = 18f, Air = 10f, Arms = 1.2f,  Sling = 1f },
            new Preset { Name = "Blatant", Speed = 2f,    Jump = 20f, Dash = 30f, Air = 20f, Arms = 1.6f,  Sling = 1.6f },
            new Preset { Name = "Floaty",  Speed = 1.2f,  Jump = 9f,  Dash = 15f, Air = 16f, Arms = 1.25f, Sling = 1.2f },
        };

        public static string[] PresetNames => Presets.Select(p => p.Name).ToArray();

        private static Preset Current => Presets[Mathf.Clamp(Menu.Index("Movement Presets"), 0, Presets.Length - 1)];
        private static float SpeedMult => Current.Speed > 0f ? Current.Speed : Settings.SpeedBoost;

        [MenuButton("Movement Presets", ButtonType.Option, Category = "Advantages", Order = 0, OptionsFrom = nameof(PresetNames), Tooltip = "Tunes every Advantages mod at once, Custom uses Settings")]
        public static void MovementPresets() { }

        [MenuButton("Tag Self", Category = "Advantages", Order = 1, OnDisable = nameof(TagSelfOff), Tooltip = "Puts your rig on the closest tagger until you get tagged")]
        public static void TagSelf()
        {
            if (!Util.Ready || Util.LocalRig == null) return;

            if (Util.IsTagged(Util.LocalRig))
            {
                Notify.Push("You got tagged");
                Buttons.SetEnabled("Tag Self", false);
                return;
            }

            Vector3 me = Util.BodyT.position;
            VRRig tagger = Util.OtherRigs
                .Where(Util.IsTagged)
                .OrderBy(r => Vector3.Distance(r.transform.position, me))
                .FirstOrDefault();

            if (tagger == null)
            {
                Util.ReleaseRig("tagself");
                return;
            }

            Util.TakeRig("tagself");
            Util.LocalRig.transform.position = tagger.rightHandTransform.position;
        }

        public static void TagSelfOff() => Util.ReleaseRig("tagself");

        [MenuButton("Speed Boost", Category = "Advantages", Order = 10, OnDisable = nameof(SpeedBoostOff), Tooltip = "Run faster, strength follows the preset")]
        public static void SpeedBoost() => Movement.Boost(SpeedMult);

        public static void SpeedBoostOff() => Movement.BoostOff();

        private static float jumpCharge = -1f;

        [MenuButton("Super Jump", Category = "Advantages", Order = 11, Tooltip = "Hold B to charge, let go to launch")]
        public static void SuperJump()
        {
            if (!Util.Ready) return;
            if (Util.B)
            {
                if (jumpCharge < 0f) jumpCharge = 0f;
                float before = jumpCharge;
                jumpCharge = Mathf.Min(1f, jumpCharge + Dt * 1.6f);
                if (before < 1f && jumpCharge >= 1f) Buzz(0.15f, 0.04f);
                return;
            }
            if (jumpCharge < 0f) return;

            float power = Current.Jump * Mathf.Lerp(0.55f, 1.25f, jumpCharge);
            jumpCharge = -1f;
            Vector3 v = Util.Body.velocity;
            Util.Body.velocity = new Vector3(v.x, Mathf.Max(v.y, power), v.z);
            Buzz(0.4f, 0.08f);
        }

        private static bool HandOnWall(Transform hand, out Vector3 normal)
        {
            foreach (Vector3 d in new[] { hand.forward, -hand.forward, hand.right, -hand.right, -hand.up, hand.up })
            {
                if (Physics.Raycast(hand.position, d, out RaycastHit hit, 0.22f, Util.RaycastMask) && Mathf.Abs(hit.normal.y) < 0.6f)
                {
                    normal = hit.normal;
                    return true;
                }
            }
            normal = Vector3.zero;
            return false;
        }

        [MenuButton("Wall Climb", Category = "Advantages", Order = 12, Tooltip = "Hold grip on a wall to climb, hold trigger to slide down")]
        public static void WallClimb()
        {
            if (!Util.Ready) return;

            Vector3 normal = Vector3.zero;
            int hands = 0;
            if (Util.LeftGrip && HandOnWall(Util.LeftHand, out Vector3 ln)) { normal += ln; hands++; }
            if (Util.RightGrip && HandOnWall(Util.RightHand, out Vector3 rn)) { normal += rn; hands++; }
            if (hands == 0) return;

            normal.Normalize();
            float climb = Util.LeftTrigger || Util.RightTrigger ? -3f : 3f + hands;
            Vector3 along = Vector3.ProjectOnPlane(Flat(Util.Body.velocity), normal) * 0.85f;
            Vector3 target = along + Vector3.up * climb - normal * 0.6f;

            Util.AntiGravity(1f);
            Util.Body.velocity = Vector3.Lerp(Util.Body.velocity, target, Dt * 10f);
        }

        private static Vector3 savedScale = Vector3.one;

        public static void LongArmsOn()
        {
            if (Util.Player != null) savedScale = Util.Player.transform.localScale;
        }

        [MenuButton("Long Arms", Category = "Advantages", Order = 13, OnEnable = nameof(LongArmsOn), OnDisable = nameof(LongArmsOff), Tooltip = "Longer reach, length follows the preset")]
        public static void LongArms()
        {
            if (Util.Player != null) Util.Player.transform.localScale = savedScale * Current.Arms;
        }

        public static void LongArmsOff()
        {
            if (Util.Player != null) Util.Player.transform.localScale = savedScale;
        }

        [MenuButton("Zero Gravity", Category = "Advantages", Order = 14, Tooltip = "No gravity with light drift damping, hold left grip to brake")]
        public static void ZeroGravity()
        {
            if (!Util.Ready) return;
            Util.AntiGravity(1f);
            float brake = Util.LeftGrip ? 4f : 0.3f;
            Util.Body.velocity = Vector3.Lerp(Util.Body.velocity, Vector3.zero, Dt * brake);
        }

        [MenuButton("Air Control", Category = "Advantages", Order = 15, Tooltip = "Steer with the left stick in the air without losing speed")]
        public static void AirControl()
        {
            if (!Util.Ready || Grounded) return;
            Vector2 l = Util.LeftStick;
            if (l.sqrMagnitude < 0.02f) return;

            Vector3 v = Util.Body.velocity;
            Vector3 h = Flat(v);
            Vector3 desired = StickDir(l).normalized * Mathf.Max(h.magnitude, 6f);
            Vector3 next = Vector3.MoveTowards(h, desired, Current.Air * Mathf.Clamp01(l.magnitude) * Dt);
            Util.Body.velocity = new Vector3(next.x, v.y, next.z);
        }

        private static float nextDash;

        [MenuButton("Dash", Category = "Advantages", Order = 16, Tooltip = "Press Y to dash where you look or where the left stick points")]
        public static void Dash()
        {
            if (!Util.Ready || !Util.Pressed("adv_dash", Util.Y) || Time.time < nextDash) return;
            nextDash = Time.time + 0.45f;

            Vector2 l = Util.LeftStick;
            Vector3 dir = l.sqrMagnitude > 0.1f ? StickDir(l).normalized : Util.Head.forward;
            Util.Body.velocity = dir * Current.Dash + Vector3.up * (Grounded ? 2f : 0f);
            Buzz(0.35f, 0.07f);
        }

        private static bool airJumpUsed;

        [MenuButton("Double Jump", Category = "Advantages", Order = 17, Tooltip = "Press A once in the air to jump again, steer with the left stick")]
        public static void DoubleJump()
        {
            if (!Util.Ready) return;
            bool press = Util.Pressed("adv_djump", Util.A);
            if (Grounded)
            {
                airJumpUsed = false;
                return;
            }
            if (airJumpUsed || !press) return;

            airJumpUsed = true;
            Vector3 h = Flat(Util.Body.velocity);
            Vector2 l = Util.LeftStick;
            if (l.sqrMagnitude > 0.1f) h = StickDir(l).normalized * Mathf.Max(h.magnitude, 5f);
            Util.Body.velocity = new Vector3(h.x, Current.Jump * 0.6f, h.z);
            Buzz(0.3f, 0.06f);
        }

        private static Vector3? slingAnchor;
        private static LineRenderer slingLine;

        [MenuButton("Slingshot Movement", Category = "Advantages", Order = 18, OnDisable = nameof(SlingshotOff), Tooltip = "Hold right trigger, pull your hand back, let go to launch")]
        public static void SlingshotMovement()
        {
            if (!Util.Ready) return;
            Transform root = Util.Player.transform;
            Vector3 hand = Util.RightHand.position;

            if (Util.RightTrigger)
            {
                if (slingAnchor == null) slingAnchor = root.InverseTransformPoint(hand);
                Vector3 anchor = root.TransformPoint(slingAnchor.Value);
                float tension = Mathf.Clamp01(Vector3.Distance(anchor, hand) / 0.6f);

                if (!Grounded) Util.AntiGravity(0.5f);
                Color c = Color.Lerp(Util.GunColor, new Color(1f, 0.35f, 0.35f), tension);
                if (slingLine == null) slingLine = Util.NewLine(0.015f, c);
                slingLine.gameObject.SetActive(true);
                Util.SetLine(slingLine, anchor, hand, c);
                return;
            }

            if (slingAnchor != null)
            {
                Vector3 pull = root.TransformPoint(slingAnchor.Value) - hand;
                slingAnchor = null;
                if (pull.magnitude > 0.08f)
                {
                    float power = Mathf.Min(pull.magnitude * 40f, 35f) * Current.Sling;
                    Util.Body.velocity = pull.normalized * power;
                    Buzz(0.5f, 0.1f);
                }
            }
            if (slingLine != null) slingLine.gameObject.SetActive(false);
        }

        public static void SlingshotOff()
        {
            slingAnchor = null;
            if (slingLine != null) Object.Destroy(slingLine.gameObject);
            slingLine = null;
        }

        private static float wallRunTime;

        [MenuButton("Wall Run", Category = "Advantages", Order = 19, Tooltip = "Run along walls while moving fast, press A to jump off")]
        public static void WallRun()
        {
            if (!Util.Ready) return;
            bool jump = Util.Pressed("adv_wallrun", Util.A);
            Vector3 v = Util.Body.velocity;
            Vector3 h = Flat(v);

            if (Grounded)
            {
                wallRunTime = 0f;
                return;
            }
            if (h.magnitude < 3.5f || wallRunTime > 1.8f) return;

            Vector3 pos = Util.BodyT.position;
            Vector3 side = Flat(Util.Head.right).normalized;
            bool hit = Physics.Raycast(pos, side, out RaycastHit info, 0.9f, Util.RaycastMask)
                       || Physics.Raycast(pos, -side, out info, 0.9f, Util.RaycastMask);
            if (!hit || Mathf.Abs(info.normal.y) > 0.3f) return;

            wallRunTime += Dt;
            airJumpUsed = false;
            Vector3 along = Flat(Vector3.ProjectOnPlane(h, info.normal)).normalized * h.magnitude;

            if (jump)
            {
                Util.Body.velocity = along + info.normal * 7f + Vector3.up * Current.Jump * 0.55f;
                wallRunTime = 0f;
                Buzz(0.3f, 0.06f);
                return;
            }

            Util.AntiGravity(0.9f);
            Util.Body.velocity = along + Vector3.up * Mathf.Max(v.y, -1.2f) - info.normal * 0.8f;
        }

        private static float keptSpeed;

        [MenuButton("Momentum Control", Category = "Advantages", Order = 20, Tooltip = "Keeps your speed in the air, hold left grip to brake")]
        public static void MomentumControl()
        {
            if (!Util.Ready) return;
            Vector3 v = Util.Body.velocity;
            Vector3 h = Flat(v);

            if (Grounded)
            {
                keptSpeed = h.magnitude;
                return;
            }

            if (Util.LeftGrip)
                h = Vector3.MoveTowards(h, Vector3.zero, 18f * Dt);
            else if (h.sqrMagnitude > 0.25f && h.magnitude < keptSpeed && h.magnitude > keptSpeed * 0.7f)
                h = h.normalized * keptSpeed;

            keptSpeed = Mathf.Max(h.magnitude, keptSpeed - Dt * 1.5f);
            Util.Body.velocity = new Vector3(h.x, v.y, h.z);
        }
    }
}
