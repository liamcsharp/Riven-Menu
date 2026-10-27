using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu
{
    public class PropManager : MonoBehaviour
    {
        public static PropManager Instance;

        public static readonly string[] Kinds =
        {
            "Ball", "Beach Ball", "Crate", "Boulder", "Banana", "Rubber Chicken",
            "Cone", "Balloon", "Dumbbell", "Dice", "Hammer", "Donut"
        };

        private class Prop
        {
            public GameObject Root;
            public Rigidbody Body;
            public Collider[] Colliders;
        }

        private class HandState
        {
            public Prop Held;
            public Vector3 Offset;
            public Quaternion RotOffset;
            public Vector3 LastPos;
            public readonly Vector3[] Vels = new Vector3[5];
            public int VelCount;
            public int VelIndex;
            public bool LastGrip;

            public void PushVel(Vector3 v)
            {
                Vels[VelIndex] = v;
                VelIndex = (VelIndex + 1) % Vels.Length;
                if (VelCount < Vels.Length) VelCount++;
            }

            public Vector3 AverageVel()
            {
                if (VelCount == 0) return Vector3.zero;
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < VelCount; i++) sum += Vels[i];
                return sum / VelCount;
            }
        }

        private readonly List<Prop> props = new List<Prop>();
        private readonly HandState left = new HandState();
        private readonly HandState right = new HandState();

        public static PropManager Ensure()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("RivenProps");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<PropManager>();
            }
            return Instance;
        }

        public int Count => props.Count;

        private void Update()
        {
            props.RemoveAll(p => p == null || p.Root == null);
            if (!Util.Ready) return;
            HandUpdate(left, Util.LeftHand, Util.LeftGrip, true);
            HandUpdate(right, Util.RightHand, Util.RightGrip, false);
            int layer = Util.VisualLayer;
            foreach (Prop p in props) SetLayer(p.Root, layer);
        }

        private void HandUpdate(HandState hand, Transform tf, bool grip, bool isLeft)
        {
            if (tf == null) return;
            Vector3 handVel = (tf.position - hand.LastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            hand.LastPos = tf.position;
            hand.PushVel(handVel);

            if (grip && !hand.LastGrip && hand.Held == null)
            {
                Prop near = Nearest(tf.position, 0.28f, hand);
                if (near != null)
                {
                    hand.Held = near;
                    near.Body.isKinematic = true;
                    hand.Offset = Quaternion.Inverse(tf.rotation) * (near.Root.transform.position - tf.position);
                    hand.RotOffset = Quaternion.Inverse(tf.rotation) * near.Root.transform.rotation;
                    if (Util.Tagger != null) Util.Tagger.StartVibration(isLeft, 0.3f, 0.05f);
                }
            }

            if (hand.Held != null)
            {
                if (hand.Held.Root == null) { hand.Held = null; }
                else if (!grip)
                {
                    Prop p = hand.Held;
                    p.Body.isKinematic = false;
                    p.Body.velocity = hand.AverageVel() * 1.15f;
                    p.Body.angularVelocity = Random.insideUnitSphere * 4f;
                    hand.Held = null;
                }
                else
                {
                    Transform rt = hand.Held.Root.transform;
                    rt.position = tf.position + tf.rotation * hand.Offset;
                    rt.rotation = tf.rotation * hand.RotOffset;
                }
            }
            hand.LastGrip = grip;
        }

        private Prop Nearest(Vector3 point, float range, HandState skipOwner)
        {
            Prop best = null;
            float bestD = range * range;
            foreach (Prop p in props)
            {
                if (p == null || p.Root == null) continue;
                if (left.Held == p || right.Held == p) continue;
                float d = (p.Root.transform.position - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        public void Spawn(string kind, Vector3 pos)
        {
            Prop p = PropFactory.Build(kind, pos);
            if (p == null) return;
            props.Add(p);
            if (props.Count > 60)
            {
                Prop old = props[0];
                props.RemoveAt(0);
                if (old.Root != null) Object.Destroy(old.Root);
            }
        }

        public void Clear()
        {
            left.Held = right.Held = null;
            foreach (Prop p in props)
                if (p != null && p.Root != null) Object.Destroy(p.Root);
            props.Clear();
        }

        public void Nudge(Vector3 explosionAt, float force)
        {
            foreach (Prop p in props)
            {
                if (p == null || p.Root == null) continue;
                p.Body.isKinematic = false;
                p.Body.AddExplosionForce(force, explosionAt, 6f, 1.5f, ForceMode.Impulse);
            }
        }

        private static void SetLayer(GameObject go, int layer)
        {
            if (go.layer != layer) go.layer = layer;
            foreach (Transform t in go.transform)
                if (t.gameObject.layer != layer) t.gameObject.layer = layer;
        }

        private static class PropFactory
        {
            public static Prop Build(string kind, Vector3 pos)
            {
                GameObject root = new GameObject("RivenProp_" + kind);
                root.transform.position = pos;
                List<Collider> cols = new List<Collider>();
                float mass = 0.4f;

                switch (kind)
                {
                    case "Ball": Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.28f, Acc(), cols); mass = 0.3f; break;
                    case "Beach Ball": Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.5f, new Color(1f, 0.9f, 0.3f), cols); mass = 0.15f; break;
                    case "Crate": Part(root, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.35f, new Color(0.6f, 0.42f, 0.2f), cols); mass = 0.5f; break;
                    case "Boulder": Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.6f, new Color(0.45f, 0.45f, 0.48f), cols); mass = 2f; break;
                    case "Banana":
                        Part(root, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.1f, 0.22f, 0.1f), new Color(1f, 0.85f, 0.1f), cols, Quaternion.Euler(0f, 0f, 60f));
                        break;
                    case "Rubber Chicken":
                        Part(root, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.14f, 0.26f, 0.14f), new Color(1f, 0.95f, 0.5f), cols, Quaternion.Euler(20f, 0f, 0f));
                        Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0.08f), Vector3.one * 0.14f, new Color(1f, 0.95f, 0.5f), cols);
                        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.2f), new Vector3(0.05f, 0.04f, 0.1f), new Color(1f, 0.5f, 0.1f), null);
                        mass = 0.12f;
                        break;
                    case "Cone": Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.3f, 0.22f, 0.3f), new Color(1f, 0.45f, 0.1f), cols); break;
                    case "Balloon":
                        Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(0.3f, 0.38f, 0.3f), new Color(1f, 0.25f, 0.4f), cols);
                        Part(root, PrimitiveType.Cube, new Vector3(0f, -0.2f, 0f), new Vector3(0.01f, 0.3f, 0.01f), Color.white, null);
                        mass = 0.05f;
                        break;
                    case "Dumbbell":
                        Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.06f, 0.18f, 0.06f), new Color(0.3f, 0.3f, 0.34f), cols, Quaternion.Euler(0f, 0f, 90f));
                        Part(root, PrimitiveType.Sphere, new Vector3(0.22f, 0f, 0f), Vector3.one * 0.18f, new Color(0.2f, 0.2f, 0.24f), cols);
                        Part(root, PrimitiveType.Sphere, new Vector3(-0.22f, 0f, 0f), Vector3.one * 0.18f, new Color(0.2f, 0.2f, 0.24f), cols);
                        mass = 1.4f;
                        break;
                    case "Dice": Part(root, PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.3f, Color.white, cols); break;
                    case "Hammer":
                        Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.04f, 0.26f, 0.04f), new Color(0.6f, 0.42f, 0.2f), cols);
                        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.28f, 0f), new Vector3(0.22f, 0.1f, 0.1f), new Color(0.4f, 0.4f, 0.44f), cols);
                        mass = 1f;
                        break;
                    case "Donut":
                        Part(root, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.34f, 0.08f, 0.34f), new Color(0.85f, 0.5f, 0.75f), cols);
                        break;
                    default: Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.28f, Acc(), cols); break;
                }

                Rigidbody rb = root.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                foreach (Collider col in cols) IgnorePlayer(col);

                return new Prop { Root = root, Body = rb, Colliders = cols.ToArray() };
            }

            private static Color Acc() => Theme.Accent;

            private static void Part(GameObject root, PrimitiveType type, Vector3 local, Vector3 scale, Color color, List<Collider> cols, Quaternion rot = default)
            {
                GameObject go = GameObject.CreatePrimitive(type);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = local;
                go.transform.localScale = scale;
                go.transform.localRotation = rot == default ? Quaternion.identity : rot;
                go.GetComponent<Renderer>().material = Util.NewUnlit(color);
                Collider col = go.GetComponent<Collider>();
                if (cols != null) cols.Add(col);
                else Object.Destroy(col);
            }

            private static void IgnorePlayer(Collider col)
            {
                if (Util.Player != null)
                {
                    if (Util.Player.bodyCollider != null) Physics.IgnoreCollision(col, Util.Player.bodyCollider);
                    if (Util.Player.headCollider != null) Physics.IgnoreCollision(col, Util.Player.headCollider);
                }
            }
        }
    }
}