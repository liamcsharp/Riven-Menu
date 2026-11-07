using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class Fun
    {
        private static void HeadOffset(Vector3 euler)
        {
            if (Util.LocalRig != null && Util.LocalRig.head != null)
                Util.LocalRig.head.trackingRotationOffset = euler;
        }

        public static void HeadReset() => HeadOffset(Vector3.zero);

        [MenuButton("Spin Head", Category = "Fun", Order = 0, OnDisable = nameof(HeadReset), Tooltip = "Your head spins around")]
        public static void SpinHead() => HeadOffset(new Vector3(0f, Mathf.Repeat(Time.time * 360f, 360f), 0f));

        [MenuButton("Backwards Head", Category = "Fun", Order = 1, OnDisable = nameof(HeadReset), Tooltip = "Your head faces backwards")]
        public static void BackwardsHead() => HeadOffset(new Vector3(0f, 180f, 0f));

        [MenuButton("Upside Down Head", Category = "Fun", Order = 2, OnDisable = nameof(HeadReset), Tooltip = "Your head is upside down")]
        public static void UpsideDownHead() => HeadOffset(new Vector3(0f, 0f, 180f));

        [MenuButton("Broken Neck", Category = "Fun", Order = 3, OnDisable = nameof(HeadReset), Tooltip = "Your head tilts sideways")]
        public static void BrokenNeck() => HeadOffset(new Vector3(0f, 0f, 90f));

        [MenuButton("Head Bob", Category = "Fun", Order = 4, OnDisable = nameof(HeadReset), Tooltip = "Your head nods to a beat")]
        public static void HeadBob() => HeadOffset(new Vector3(Mathf.Sin(Time.time * 8f) * 25f, 0f, 0f));

        private static readonly List<GameObject> spawned = new List<GameObject>();

        private static GameObject SpawnPhysics(PrimitiveType type, Vector3 pos, float size, Color color, Vector3 velocity, float life = 30f)
        {
            GameObject go = Util.Primitive(type, pos, Vector3.one * size, color, true);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.velocity = velocity;
            Collider col = go.GetComponent<Collider>();
            if (Util.Player != null)
            {
                Physics.IgnoreCollision(col, Util.Player.bodyCollider);
                Physics.IgnoreCollision(col, Util.Player.headCollider);
            }
            spawned.Add(go);
            if (life > 0f) Object.Destroy(go, life);
            if (spawned.Count > 300)
            {
                if (spawned[0] != null) Object.Destroy(spawned[0]);
                spawned.RemoveAt(0);
            }
            return go;
        }

        private static Vector3 InFront => Util.Head != null ? Util.Head.position + Util.Head.forward * 0.8f : Vector3.zero;

        [MenuButton("Spawn Cube", ButtonType.Button, Category = "Fun", Order = 10, Notify = false, Tooltip = "Drops a cube in front of you")]
        public static void SpawnCube() => SpawnPhysics(PrimitiveType.Cube, InFront, 0.3f, Theme.Accent, Vector3.zero, 0f);

        [MenuButton("Spawn Ball", ButtonType.Button, Category = "Fun", Order = 11, Notify = false, Tooltip = "Drops a ball in front of you")]
        public static void SpawnBall() => SpawnPhysics(PrimitiveType.Sphere, InFront, 0.3f, Theme.Accent2, Vector3.zero, 0f);

        private static float nextRain;

        [MenuButton("Cube Rain", Category = "Fun", Order = 12, Tooltip = "Cubes fall from the sky around you")]
        public static void CubeRain()
        {
            if (Util.BodyT == null || Time.time < nextRain) return;
            nextRain = Time.time + 0.08f;
            Vector3 p = Util.BodyT.position + new Vector3(Random.Range(-6f, 6f), 8f, Random.Range(-6f, 6f));
            SpawnPhysics(PrimitiveType.Cube, p, Random.Range(0.1f, 0.3f), Color.HSVToRGB(Random.value, 0.7f, 1f), Vector3.zero, 8f);
        }

        [MenuButton("Ball Gun", Category = "Guns", Order = 2, OnDisable = nameof(GunOff), Tooltip = "Aim, then hold trigger or left click to shoot balls")]
        public static void BallGun()
        {
            if (!GunLib.World(out GunHit g) || !g.Fire || Time.time < nextShot) return;
            nextShot = Time.time + 0.12f;
            SpawnPhysics(PrimitiveType.Sphere, g.Origin, 0.12f, Util.GunColor, (g.Point - g.Origin).normalized * 25f, 10f);
        }

        public static void GunOff() => GunLib.Hide();

        private static float nextShot;

        [MenuButton("Throw Balls", Category = "Fun", Order = 14, Tooltip = "Right trigger throws balls from your hand")]
        public static void ThrowBalls()
        {
            if (!Util.Ready || !Util.RightTrigger || Time.time < nextShot) return;
            nextShot = Time.time + 0.15f;
            SpawnPhysics(PrimitiveType.Sphere, Util.RightHand.position, 0.1f, Theme.Accent, -Util.RightHand.up * 14f + Util.Body.velocity, 10f);
        }

        private static Rigidbody held;

        private static Vector3 gravityDir = Vector3.forward;

        [MenuButton("Gravity Gun", Category = "Guns", Order = 3, OnDisable = nameof(GravityOff), Tooltip = "Grab spawned objects with the gun and fling them")]
        public static void GravityGun()
        {
            bool aiming = GunLib.World(out GunHit g);
            if (aiming) gravityDir = g.Direction;
            if (!aiming || !g.Fire)
            {
                if (held != null) held.velocity = gravityDir * 18f;
                held = null;
                return;
            }
            if (held == null && g.Hit)
            {
                Collider[] near = Physics.OverlapSphere(g.Point, 0.6f);
                foreach (Collider c in near)
                {
                    Rigidbody rb = c.attachedRigidbody;
                    if (rb != null && spawned.Contains(rb.gameObject)) { held = rb; break; }
                }
            }
            if (held != null)
            {
                Vector3 target = g.Origin + g.Direction * 2f;
                held.velocity = (target - held.position) * 12f;
            }
        }

        public static void GravityOff()
        {
            held = null;
            GunLib.Hide();
        }

        [MenuButton("Clear Spawned", ButtonType.Button, Category = "Fun", Order = 16, Tooltip = "Removes everything you spawned")]
        public static void ClearSpawned()
        {
            foreach (GameObject go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
        }

        private static readonly List<LineRenderer> drawings = new List<LineRenderer>();
        private static LineRenderer currentStroke;

        [MenuButton("Draw", Category = "Fun", Order = 20, Tooltip = "Hold right trigger to draw in the air")]
        public static void Draw()
        {
            if (!Util.Ready) return;
            if (!Util.RightTrigger)
            {
                currentStroke = null;
                return;
            }
            Vector3 tip = Util.RightHand.position - Util.RightHand.up * 0.1f;
            if (currentStroke == null)
            {
                currentStroke = Util.NewLine(0.015f, Theme.Accent);
                currentStroke.positionCount = 0;
                drawings.Add(currentStroke);
            }
            int count = currentStroke.positionCount;
            if (count > 0 && Vector3.Distance(currentStroke.GetPosition(count - 1), tip) < 0.01f) return;
            currentStroke.positionCount = count + 1;
            currentStroke.SetPosition(count, tip);
            currentStroke.gameObject.layer = Util.VisualLayer;
        }

        [MenuButton("Clear Drawings", ButtonType.Button, Category = "Fun", Order = 21, Tooltip = "Erases everything you drew")]
        public static void ClearDrawings() => Util.DestroyAll(drawings);

        private static LineRenderer laser;

        [MenuButton("Laser Pointer", Category = "Fun", Order = 22, OnDisable = nameof(LaserOff), Tooltip = "A laser from your right hand")]
        public static void LaserPointer()
        {
            if (!Util.Ready) return;
            if (laser == null) laser = Util.NewLine(0.008f, new Color(1f, 0.2f, 0.25f));
            Vector3 o = Util.RightHand.position, d = -Util.RightHand.up;
            Vector3 end = Physics.Raycast(o, d, out RaycastHit hit, 200f, Util.RaycastMask) ? hit.point : o + d * 200f;
            Util.SetLine(laser, o, end, new Color(1f, 0.2f, 0.25f));
        }

        public static void LaserOff()
        {
            if (laser != null) Object.Destroy(laser.gameObject);
        }

        private static GameObject pet;
        private static Vector3 petVel;

        [MenuButton("Pet Orb", Category = "Fun", Order = 23, OnDisable = nameof(PetOff), Tooltip = "A little orb that follows you around")]
        public static void PetOrb()
        {
            if (!Util.Ready) return;
            if (pet == null) pet = Util.Primitive(PrimitiveType.Sphere, Util.Head.position, Vector3.one * 0.15f, Theme.Accent);
            Vector3 target = Util.Head.position + Util.Head.right * 0.5f + Vector3.up * (0.2f + Mathf.Sin(Time.time * 2f) * 0.08f);
            pet.transform.position = Vector3.SmoothDamp(pet.transform.position, target, ref petVel, 0.25f);
            pet.GetComponent<Renderer>().material.color = Theme.Accent;
            pet.layer = Util.VisualLayer;
        }

        public static void PetOff()
        {
            if (pet != null) Object.Destroy(pet);
        }

        private static readonly List<GameObject> orbs = new List<GameObject>();

        [MenuButton("Orbiting Orbs", Category = "Fun", Order = 24, OnDisable = nameof(OrbsOff), Tooltip = "Orbs circle around your head")]
        public static void OrbitingOrbs()
        {
            if (!Util.Ready) return;
            while (orbs.Count < 4) orbs.Add(Util.Primitive(PrimitiveType.Sphere, Util.Head.position, Vector3.one * 0.08f, Theme.Accent));
            for (int i = 0; i < orbs.Count; i++)
            {
                float a = Time.time * 2.5f + i * Mathf.PI / 2f;
                orbs[i].transform.position = Util.Head.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 0.5f) * 0.2f, Mathf.Sin(a)) * 0.45f;
                orbs[i].GetComponent<Renderer>().material.color = Color.Lerp(Theme.Accent, Theme.Accent2, i / 3f);
                orbs[i].layer = Util.VisualLayer;
            }
        }

        public static void OrbsOff()
        {
            foreach (GameObject o in orbs)
                if (o != null) Object.Destroy(o);
            orbs.Clear();
        }

        private static LineRenderer halo;

        [MenuButton("Halo", Category = "Fun", Order = 25, OnDisable = nameof(HaloOff), Tooltip = "A glowing ring above your head")]
        public static void Halo()
        {
            if (!Util.Ready) return;
            if (halo == null)
            {
                halo = Util.NewLine(0.02f, Color.white);
                halo.positionCount = 24;
                halo.loop = true;
            }
            Vector3 c = Util.Head.position + Vector3.up * 0.32f;
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                halo.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.16f);
            }
            Color col = Theme.Lighten(Theme.Accent, 0.4f);
            halo.material.color = col;
            halo.startColor = halo.endColor = col;
            halo.gameObject.layer = Util.VisualLayer;
        }

        public static void HaloOff()
        {
            if (halo != null) Object.Destroy(halo.gameObject);
        }

        private static Light leftLight, rightLight;

        private static Light NewLight(Transform hand)
        {
            GameObject go = new GameObject("RivenLight");
            go.transform.SetParent(hand, false);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = 2.5f;
            return l;
        }

        [MenuButton("Hand Lights", Category = "Fun", Order = 26, OnDisable = nameof(LightsOff), Tooltip = "Your hands glow and light up the world")]
        public static void HandLights()
        {
            if (!Util.Ready) return;
            if (leftLight == null) leftLight = NewLight(Util.LeftHand);
            if (rightLight == null) rightLight = NewLight(Util.RightHand);
            leftLight.color = Theme.Accent;
            rightLight.color = Theme.Accent2;
        }

        public static void LightsOff()
        {
            if (leftLight != null) Object.Destroy(leftLight.gameObject);
            if (rightLight != null) Object.Destroy(rightLight.gameObject);
        }

        private static void Burst(Vector3 at, int count, float force)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 v = Random.onUnitSphere * force + Vector3.up * force * 0.5f;
                SpawnPhysics(PrimitiveType.Cube, at, Random.Range(0.03f, 0.07f), Color.HSVToRGB(Random.value, 0.8f, 1f), v, 4f);
            }
        }

        [MenuButton("Confetti", ButtonType.Button, Category = "Fun", Order = 27, Notify = false, Tooltip = "A burst of confetti from your hand")]
        public static void Confetti()
        {
            if (Util.RightHand != null) Burst(Util.RightHand.position, 40, 4f);
        }

        private static float nextFirework;

        [MenuButton("Fireworks", Category = "Fun", Order = 28, Tooltip = "Fireworks pop in the sky around you")]
        public static void Fireworks()
        {
            if (Util.BodyT == null || Time.time < nextFirework) return;
            nextFirework = Time.time + 1.2f;
            Vector3 p = Util.BodyT.position + new Vector3(Random.Range(-10f, 10f), Random.Range(8f, 14f), Random.Range(-10f, 10f));
            Burst(p, 30, 6f);
        }

        private static AudioSource piano;
        private static readonly Dictionary<int, AudioClip> notes = new Dictionary<int, AudioClip>();

        private static AudioClip Note(int semitone)
        {
            if (notes.TryGetValue(semitone, out AudioClip clip)) return clip;
            const int rate = 44100;
            float freq = 261.63f * Mathf.Pow(2f, semitone / 12f);
            int n = rate / 2;
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 6f) * Mathf.Min(1f, t * 200f);
                data[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(4f * Mathf.PI * freq * t)) * env * 0.4f;
            }
            clip = AudioClip.Create("riven_note" + semitone, n, 1, rate, false);
            clip.SetData(data, 0);
            notes[semitone] = clip;
            return clip;
        }

        private static readonly int[] Scale = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };

        private static void PlayHand(Transform hand, string id, bool pressed)
        {
            if (!Util.Pressed(id, pressed)) return;
            float rel = Mathf.Clamp01((hand.position.y - (Util.Head.position.y - 1f)) / 1.4f);
            int idx = Mathf.Clamp(Mathf.FloorToInt(rel * Scale.Length), 0, Scale.Length - 1);
            piano.PlayOneShot(Note(Scale[idx]));
        }

        [MenuButton("Piano Hands", Category = "Fun", Order = 29, Tooltip = "Triggers play notes, higher hands play higher notes. Only you hear it")]
        public static void PianoHands()
        {
            if (!Util.Ready) return;
            if (piano == null)
            {
                GameObject go = new GameObject("RivenPiano");
                Object.DontDestroyOnLoad(go);
                piano = go.AddComponent<AudioSource>();
                piano.spatialBlend = 0f;
                piano.volume = 0.6f;
            }
            PlayHand(Util.RightHand, "pianoR", Util.RightTrigger);
            PlayHand(Util.LeftHand, "pianoL", Util.LeftTrigger);
        }

        private static GameObject bubble;

        [MenuButton("Bubble", Category = "Fun", Order = 30, OnDisable = nameof(BubbleOff), Tooltip = "A see-through bubble around you")]
        public static void Bubble()
        {
            if (!Util.Ready) return;
            if (bubble == null) bubble = Util.Primitive(PrimitiveType.Sphere, Util.BodyT.position, Vector3.one * 1.6f, Color.white);
            bubble.transform.position = Util.BodyT.position + Vector3.up * 0.2f;
            float s = 1.6f + Mathf.Sin(Time.time * 2f) * 0.05f;
            bubble.transform.localScale = Vector3.one * s;
            bubble.GetComponent<Renderer>().material.color = Theme.WithA(Theme.Accent, 0.12f);
            bubble.layer = Util.VisualLayer;
        }

        public static void BubbleOff()
        {
            if (bubble != null) Object.Destroy(bubble);
        }
    }
}
