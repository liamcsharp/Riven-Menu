using UnityEngine;

namespace RivenMenu.Mods
{
    public static class Rig
    {
        private const string Cat = "Rig";
        private const string Owner = "rigpose";

        private static Vector3 offsetCache = new Vector3(0f, -0.15f, 0f);

        private static bool Ready => Util.Ready && Util.LocalRig != null && Util.Head != null && Util.LeftHand != null && Util.RightHand != null;

        private static Vector3 RealBody
        {
            get
            {
                if (!Util.RigTaken && Util.LocalRig != null && Util.BodyT != null)
                    offsetCache = Util.LocalRig.transform.position - Util.BodyT.position;
                return Util.BodyT.position + offsetCache;
            }
        }

        private static Quaternion Facing
        {
            get
            {
                Vector3 f = Util.Head.forward;
                f.y = 0f;
                return f.sqrMagnitude < 0.001f ? Quaternion.identity : Quaternion.LookRotation(f.normalized);
            }
        }

        private static float T => Time.time;

        private static void Pose(Vector3 body, Quaternion rot, Vector3 left, Vector3 right, Quaternion head)
        {
            Util.TakeRig(Owner);
            Util.PlaceRig(body, rot);
            Util.PoseRig(body, head, body + rot * left, body + rot * right);
        }

        private static void Arms(Vector3 left, Vector3 right)
        {
            if (!Ready) return;
            Quaternion f = Facing;
            Pose(RealBody, f, left, right, Util.Head.rotation);
        }

        private static void Move(Vector3 body, Quaternion rot)
        {
            if (!Ready) return;
            Vector3 real = RealBody;
            Quaternion delta = rot * Quaternion.Inverse(Facing);
            Vector3 l = delta * (Util.LeftHand.position - real);
            Vector3 r = delta * (Util.RightHand.position - real);
            Util.TakeRig(Owner);
            Util.PlaceRig(body, rot);
            Util.PoseRig(body, delta * Util.Head.rotation, body + l, body + r);
        }

        private static bool AnyPose()
        {
            foreach (ButtonInfo b in Buttons.All)
                if (b.IsActive && b.Category == Cat && b.DisableMethod == (System.Action)PoseOff) return true;
            return false;
        }

        public static void PoseOff()
        {
            if (!AnyPose()) Util.ReleaseRig(Owner);
        }

        private static void Head(Vector3 euler)
        {
            if (Util.LocalRig != null && Util.LocalRig.head != null) Util.LocalRig.head.trackingRotationOffset = euler;
        }

        public static void HeadOff()
        {
            foreach (ButtonInfo b in Buttons.All)
                if (b.IsActive && b.Category == Cat && b.DisableMethod == (System.Action)HeadOff) return;
            Fun.HeadReset();
        }

        [MenuButton("T-Pose", Category = Cat, Order = 0, OnDisable = nameof(PoseOff), Tooltip = "Arms straight out to the sides")]
        public static void TPose() => Arms(new Vector3(-0.85f, 0.1f, 0f), new Vector3(0.85f, 0.1f, 0f));

        [MenuButton("Zombie Arms", Category = Cat, Order = 1, OnDisable = nameof(PoseOff), Tooltip = "Arms stick straight out in front")]
        public static void ZombieArms() => Arms(new Vector3(-0.2f, 0.12f + Mathf.Sin(T * 3f) * 0.03f, 0.75f), new Vector3(0.2f, 0.12f + Mathf.Cos(T * 3f) * 0.03f, 0.75f));

        [MenuButton("Hands Up", Category = Cat, Order = 2, OnDisable = nameof(PoseOff), Tooltip = "Both hands in the air")]
        public static void HandsUp() => Arms(new Vector3(-0.25f, 0.8f, 0.05f), new Vector3(0.25f, 0.8f, 0.05f));

        [MenuButton("Wave", Category = Cat, Order = 3, OnDisable = nameof(PoseOff), Tooltip = "Your right hand waves hi")]
        public static void Wave() => Arms(new Vector3(-0.25f, -0.35f, 0.1f), new Vector3(0.42f + Mathf.Sin(T * 9f) * 0.15f, 0.55f, 0.1f));

        [MenuButton("Clap", Category = Cat, Order = 4, OnDisable = nameof(PoseOff), Tooltip = "Claps your hands over and over")]
        public static void Clap()
        {
            float x = 0.05f + Mathf.Abs(Mathf.Sin(T * 8f)) * 0.17f;
            Arms(new Vector3(-x, 0.05f, 0.45f), new Vector3(x, 0.05f, 0.45f));
        }

        [MenuButton("Dab", Category = Cat, Order = 5, OnDisable = nameof(PoseOff), Tooltip = "Hits a dab")]
        public static void Dab()
        {
            if (!Ready) return;
            Quaternion f = Facing;
            Pose(RealBody, f, new Vector3(-0.75f, 0.45f, 0.25f), new Vector3(-0.15f, 0.22f, 0.22f), f * Quaternion.Euler(30f, -35f, 10f));
        }

        [MenuButton("Flap Arms", Category = Cat, Order = 6, OnDisable = nameof(PoseOff), Tooltip = "Flaps your arms like a bird")]
        public static void FlapArms()
        {
            float y = 0.1f + Mathf.Sin(T * 10f) * 0.45f;
            Arms(new Vector3(-0.8f, y, 0f), new Vector3(0.8f, y, 0f));
        }

        [MenuButton("Swim", Category = Cat, Order = 7, OnDisable = nameof(PoseOff), Tooltip = "Does the front crawl in the air")]
        public static void Swim()
        {
            float a = T * 5f;
            Arms(new Vector3(-0.25f, 0.1f + Mathf.Sin(a) * 0.35f, 0.4f + Mathf.Cos(a) * 0.35f), new Vector3(0.25f, 0.1f - Mathf.Sin(a) * 0.35f, 0.4f - Mathf.Cos(a) * 0.35f));
        }

        [MenuButton("Windmill Arms", Category = Cat, Order = 8, OnDisable = nameof(PoseOff), Tooltip = "Spins both arms in big circles")]
        public static void Windmill()
        {
            float a = T * 7f;
            Arms(new Vector3(-0.3f, 0.1f + Mathf.Sin(a + Mathf.PI) * 0.75f, Mathf.Cos(a + Mathf.PI) * 0.75f), new Vector3(0.3f, 0.1f + Mathf.Sin(a) * 0.75f, Mathf.Cos(a) * 0.75f));
        }

        [MenuButton("Jumping Jacks", Category = Cat, Order = 9, OnDisable = nameof(PoseOff), Tooltip = "Does jumping jacks")]
        public static void JumpingJacks()
        {
            if (!Ready) return;
            float p = (Mathf.Sin(T * 6f) + 1f) / 2f;
            Quaternion f = Facing;
            Vector3 l = Vector3.Lerp(new Vector3(-0.3f, -0.45f, 0f), new Vector3(-0.5f, 0.75f, 0f), p);
            Vector3 r = Vector3.Lerp(new Vector3(0.3f, -0.45f, 0f), new Vector3(0.5f, 0.75f, 0f), p);
            Pose(RealBody + Vector3.up * (p * 0.18f), f, l, r, Util.Head.rotation);
        }

        [MenuButton("Boxing", Category = Cat, Order = 10, OnDisable = nameof(PoseOff), Tooltip = "Throws punches left and right")]
        public static void Boxing()
        {
            float a = Mathf.Max(0f, Mathf.Sin(T * 9f));
            float b = Mathf.Max(0f, -Mathf.Sin(T * 9f));
            Arms(new Vector3(-0.18f, 0.18f, 0.25f + a * 0.55f), new Vector3(0.18f, 0.18f, 0.25f + b * 0.55f));
        }

        [MenuButton("Drum Roll", Category = Cat, Order = 11, OnDisable = nameof(PoseOff), Tooltip = "Drums super fast")]
        public static void DrumRoll()
        {
            float s = Mathf.Sin(T * 22f) * 0.12f;
            Arms(new Vector3(-0.22f, -0.05f + s, 0.42f), new Vector3(0.22f, -0.05f - s, 0.42f));
        }

        [MenuButton("Shrug", Category = Cat, Order = 12, OnDisable = nameof(PoseOff), Tooltip = "Idk")]
        public static void Shrug()
        {
            float b = Mathf.Abs(Mathf.Sin(T * 3f)) * 0.08f;
            Arms(new Vector3(-0.45f, -0.08f + b, 0.25f), new Vector3(0.45f, -0.08f + b, 0.25f));
        }

        [MenuButton("Pray", Category = Cat, Order = 13, OnDisable = nameof(PoseOff), Tooltip = "Hands together in front of you")]
        public static void Pray() => Arms(new Vector3(-0.03f, 0.15f, 0.3f), new Vector3(0.03f, 0.15f, 0.3f));

        [MenuButton("Big Stretch", Category = Cat, Order = 14, OnDisable = nameof(PoseOff), Tooltip = "Stretches up and out")]
        public static void Stretch() => Arms(new Vector3(-0.6f, 0.65f, 0f), new Vector3(0.6f, 0.65f, 0f));

        [MenuButton("Orbiting Hands", Category = Cat, Order = 15, OnDisable = nameof(PoseOff), Tooltip = "Your hands circle around your body")]
        public static void OrbitHands()
        {
            float a = T * 4f;
            Arms(new Vector3(Mathf.Cos(a + Mathf.PI), 0f, Mathf.Sin(a + Mathf.PI)) * 0.65f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.65f);
        }

        [MenuButton("Conductor", Category = Cat, Order = 16, OnDisable = nameof(PoseOff), Tooltip = "Conducts an invisible orchestra")]
        public static void Conductor()
        {
            float a = T * 4f;
            Arms(new Vector3(-0.3f, 0.1f + Mathf.Sin(a * 2f) * 0.1f, 0.45f), new Vector3(0.3f + Mathf.Sin(a) * 0.25f, 0.25f + Mathf.Sin(a * 2f) * 0.18f, 0.5f));
        }

        [MenuButton("Salute", Category = Cat, Order = 17, OnDisable = nameof(PoseOff), Tooltip = "Salutes")]
        public static void Salute() => Arms(new Vector3(-0.22f, -0.4f, 0.05f), new Vector3(0.14f, 0.42f, 0.14f));

        [MenuButton("Thinking", Category = Cat, Order = 18, OnDisable = nameof(PoseOff), Tooltip = "Hand on your chin, hmm")]
        public static void Thinking() => Arms(new Vector3(-0.1f, -0.05f, 0.25f), new Vector3(0.05f, 0.26f, 0.18f));

        [MenuButton("Point Forward", Category = Cat, Order = 19, OnDisable = nameof(PoseOff), Tooltip = "Points straight ahead")]
        public static void PointForward() => Arms(new Vector3(-0.22f, -0.4f, 0.05f), new Vector3(0.15f, 0.2f, 0.85f));

        [MenuButton("Hug", Category = Cat, Order = 20, OnDisable = nameof(PoseOff), Tooltip = "Arms out ready for a hug")]
        public static void Hug()
        {
            float x = 0.35f + Mathf.Sin(T * 2f) * 0.1f;
            Arms(new Vector3(-x, 0.12f, 0.5f), new Vector3(x, 0.12f, 0.5f));
        }

        [MenuButton("Floating Body", Category = Cat, Order = 30, OnDisable = nameof(PoseOff), Tooltip = "Your body floats above you")]
        public static void FloatingBody()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.up * (0.8f + Mathf.Sin(T * 2f) * 0.1f), Facing);
        }

        [MenuButton("Lay Down", Category = Cat, Order = 31, OnDisable = nameof(PoseOff), Tooltip = "Your body lies flat on its back")]
        public static void LayDown()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.down * 0.45f, Facing * Quaternion.Euler(-90f, 0f, 0f));
        }

        [MenuButton("Backflip Loop", Category = Cat, Order = 32, OnDisable = nameof(PoseOff), Tooltip = "Your body does backflips nonstop")]
        public static void BackflipLoop()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.up * 0.4f, Facing * Quaternion.Euler(-Mathf.Repeat(T * 360f, 360f), 0f, 0f));
        }

        [MenuButton("Frontflip Loop", Category = Cat, Order = 33, OnDisable = nameof(PoseOff), Tooltip = "Your body does frontflips nonstop")]
        public static void FrontflipLoop()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.up * 0.4f, Facing * Quaternion.Euler(Mathf.Repeat(T * 360f, 360f), 0f, 0f));
        }

        [MenuButton("Cartwheel", Category = Cat, Order = 34, OnDisable = nameof(PoseOff), Tooltip = "Your body cartwheels in place")]
        public static void Cartwheel()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.up * 0.3f, Facing * Quaternion.Euler(0f, 0f, Mathf.Repeat(T * 300f, 360f)));
        }

        [MenuButton("Matrix Lean", Category = Cat, Order = 35, OnDisable = nameof(PoseOff), Tooltip = "Leans way back like you are dodging bullets")]
        public static void MatrixLean()
        {
            if (!Ready) return;
            Move(RealBody, Facing * Quaternion.Euler(-40f + Mathf.Sin(T * 2f) * 8f, 0f, 0f));
        }

        [MenuButton("Slow Spin", Category = Cat, Order = 36, OnDisable = nameof(PoseOff), Tooltip = "Your body slowly turns in circles")]
        public static void SlowSpin()
        {
            if (!Ready) return;
            Move(RealBody, Quaternion.Euler(0f, Mathf.Repeat(T * 90f, 360f), 0f));
        }

        [MenuButton("Jitter Body", Category = Cat, Order = 37, OnDisable = nameof(PoseOff), Tooltip = "Your body shakes like it is glitching")]
        public static void Jitter()
        {
            if (!Ready) return;
            Move(RealBody + Random.insideUnitSphere * 0.06f, Facing * Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(-6f, 6f), Random.Range(-6f, 6f)));
        }

        [MenuButton("Bouncy Body", Category = Cat, Order = 38, OnDisable = nameof(PoseOff), Tooltip = "Your body bounces up and down")]
        public static void Bouncy()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.up * (Mathf.Abs(Mathf.Sin(T * 5f)) * 0.4f), Facing);
        }

        [MenuButton("Sway Dance", Category = Cat, Order = 39, OnDisable = nameof(PoseOff), Tooltip = "Sways side to side with your hands up")]
        public static void SwayDance()
        {
            if (!Ready) return;
            Quaternion f = Facing;
            float s = Mathf.Sin(T * 4f);
            Pose(RealBody + f * new Vector3(s * 0.15f, Mathf.Abs(s) * 0.05f, 0f), f * Quaternion.Euler(0f, 0f, -s * 12f), new Vector3(-0.35f + s * 0.1f, 0.65f, 0.05f), new Vector3(0.35f + s * 0.1f, 0.65f, 0.05f), Util.Head.rotation * Quaternion.Euler(0f, 0f, -s * 15f));
        }

        [MenuButton("Moonwalk", Category = Cat, Order = 40, OnDisable = nameof(PoseOff), Tooltip = "Your body slides backwards over and over")]
        public static void Moonwalk()
        {
            if (!Ready) return;
            Quaternion f = Facing;
            float k = Mathf.Repeat(T * 0.8f, 1f);
            Move(RealBody + f * new Vector3(0f, 0f, 0.6f - k * 1.2f), f);
        }

        [MenuButton("Half Sunk", Category = Cat, Order = 41, OnDisable = nameof(PoseOff), Tooltip = "Your body is half way into the floor")]
        public static void HalfSunk()
        {
            if (!Ready) return;
            Move(RealBody + Vector3.down * 0.7f, Facing);
        }

        [MenuButton("Body Behind You", Category = Cat, Order = 42, OnDisable = nameof(PoseOff), Tooltip = "Your body stands right behind where you really are")]
        public static void Behind()
        {
            if (!Ready) return;
            Move(RealBody - Facing * Vector3.forward * 1.2f, Facing);
        }

        [MenuButton("Body In Front", Category = Cat, Order = 43, OnDisable = nameof(PoseOff), Tooltip = "Your body walks in front of you")]
        public static void InFront()
        {
            if (!Ready) return;
            Move(RealBody + Facing * Vector3.forward * 1.2f, Facing);
        }

        [MenuButton("Body Orbits You", Category = Cat, Order = 44, OnDisable = nameof(PoseOff), Tooltip = "Your body circles around the real you")]
        public static void OrbitSelf()
        {
            if (!Ready) return;
            float a = T * 2.5f;
            Vector3 c = RealBody;
            Vector3 p = c + new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a)) * 1f;
            Vector3 look = c - p;
            look.y = 0f;
            Move(p, Quaternion.LookRotation(look.normalized));
        }

        [MenuButton("Giant Steps", Category = Cat, Order = 45, OnDisable = nameof(PoseOff), Tooltip = "Your body hops forward in big leaps and snaps back")]
        public static void GiantSteps()
        {
            if (!Ready) return;
            Quaternion f = Facing;
            float k = Mathf.Repeat(T, 1f);
            Move(RealBody + f * new Vector3(0f, Mathf.Sin(k * Mathf.PI) * 0.6f, k * 1.4f), f);
        }

        [MenuButton("Sideways Body", Category = Cat, Order = 46, OnDisable = nameof(PoseOff), Tooltip = "Your body is tipped over on its side")]
        public static void Sideways()
        {
            if (!Ready) return;
            Move(RealBody, Facing * Quaternion.Euler(0f, 0f, 90f));
        }

        [MenuButton("Nod Yes", Category = Cat, Order = 60, OnDisable = nameof(HeadOff), Tooltip = "Your head nods yes")]
        public static void NodYes() => Head(new Vector3(Mathf.Sin(T * 7f) * 22f, 0f, 0f));

        [MenuButton("Shake No", Category = Cat, Order = 61, OnDisable = nameof(HeadOff), Tooltip = "Your head shakes no")]
        public static void ShakeNo() => Head(new Vector3(0f, Mathf.Sin(T * 8f) * 35f, 0f));

        [MenuButton("Look Up", Category = Cat, Order = 62, OnDisable = nameof(HeadOff), Tooltip = "Your head stares at the sky")]
        public static void LookUp() => Head(new Vector3(-60f, 0f, 0f));

        [MenuButton("Look Down", Category = Cat, Order = 63, OnDisable = nameof(HeadOff), Tooltip = "Your head stares at the floor")]
        public static void LookDown() => Head(new Vector3(60f, 0f, 0f));

        [MenuButton("Owl Head", Category = Cat, Order = 64, OnDisable = nameof(HeadOff), Tooltip = "Your head swivels way around like an owl")]
        public static void OwlHead() => Head(new Vector3(0f, Mathf.Sin(T * 1.5f) * 150f, 0f));

        [MenuButton("Bobblehead", Category = Cat, Order = 65, OnDisable = nameof(HeadOff), Tooltip = "Your head wobbles everywhere")]
        public static void Bobblehead() => Head(new Vector3(Mathf.Sin(T * 13f) * 15f, Mathf.Sin(T * 9f) * 20f, Mathf.Sin(T * 11f) * 18f));

        [MenuButton("Head Tilt Loop", Category = Cat, Order = 66, OnDisable = nameof(HeadOff), Tooltip = "Your head tilts side to side")]
        public static void HeadTilt() => Head(new Vector3(0f, 0f, Mathf.Sin(T * 3f) * 30f));

        [MenuButton("Look At Closest", Category = Cat, Order = 67, OnDisable = nameof(HeadOff), Tooltip = "Your head always turns to look at the closest player")]
        public static void LookAtClosest()
        {
            if (!Ready) return;
            VRRig best = null;
            float bestD = float.MaxValue;
            Vector3 me = Util.Head.position;
            foreach (VRRig r in Util.OtherRigs)
            {
                if (r == null) continue;
                float d = (r.transform.position - me).sqrMagnitude;
                if (d < bestD) { bestD = d; best = r; }
            }
            if (best == null) { Head(Vector3.zero); return; }
            Vector3 dir = Quaternion.Inverse(Util.Head.rotation) * (GunLib.HeadOf(best) - me).normalized;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            Head(new Vector3(Mathf.Clamp(pitch, -70f, 70f), Mathf.Clamp(yaw, -160f, 160f), 0f));
        }

        [MenuButton("Crazy Eyes", Category = Cat, Order = 68, OnDisable = nameof(HeadOff), Tooltip = "Your head snaps to random directions")]
        public static void CrazyEyes()
        {
            if (Time.time < nextCrazy) return;
            nextCrazy = Time.time + Random.Range(0.15f, 0.5f);
            Head(new Vector3(Random.Range(-40f, 40f), Random.Range(-90f, 90f), Random.Range(-20f, 20f)));
        }
        private static float nextCrazy;

        [MenuButton("Reset Rig", ButtonType.Button, Category = Cat, Order = 90, Tooltip = "Turns off every rig mod and gives your body back")]
        public static void ResetRig()
        {
            foreach (ButtonInfo b in Buttons.All)
                if (b.Category == Cat && b.IsActive) Buttons.SetEnabled(b, false);
            Util.ReleaseRig(Owner);
            Fun.HeadReset();
        }
    }
}
