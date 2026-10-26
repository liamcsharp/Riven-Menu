using System.Collections.Generic;
using System.Linq;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace RivenMenu
{
    public static class Util
    {
        public static GTPlayer Player => GTPlayer.Instance;
        public static GorillaTagger Tagger => GorillaTagger.Instance;
        public static Rigidbody Body => Tagger != null ? Tagger.rigidbody : null;
        public static Transform Head => Tagger != null ? Tagger.headCollider.transform : null;
        public static Transform RightHand => Tagger != null ? Tagger.rightHandTransform : null;
        public static Transform LeftHand => Tagger != null ? Tagger.leftHandTransform : null;
        public static Transform BodyT => Tagger != null ? Tagger.bodyCollider.transform : null;
        public static VRRig LocalRig => VRRig.LocalRig;
        public static bool Ready => Tagger != null && Player != null && Body != null;

        private static ControllerInputPoller Poller => ControllerInputPoller.instance;
        public static bool A => Poller != null && Poller.rightControllerPrimaryButton;
        public static bool B => Poller != null && Poller.rightControllerSecondaryButton;
        public static bool X => Poller != null && Poller.leftControllerPrimaryButton;
        public static bool Y => Poller != null && Poller.leftControllerSecondaryButton;
        public static bool RightGrip => Poller != null && Poller.rightGrab;
        public static bool LeftGrip => Poller != null && Poller.leftGrab;
        public static bool RightTrigger => Poller != null && Poller.rightControllerIndexFloat > 0.5f;
        public static bool LeftTrigger => Poller != null && Poller.leftControllerIndexFloat > 0.5f;
        public static Vector2 LeftStick => Poller != null ? Poller.leftControllerPrimary2DAxis : Vector2.zero;
        public static Vector2 RightStick => Poller != null ? Poller.rightControllerPrimary2DAxis : Vector2.zero;

        public static bool Key(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        public static bool KeyDown(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

        private static readonly Dictionary<string, bool> lastStates = new Dictionary<string, bool>();

        public static bool Pressed(string id, bool now)
        {
            lastStates.TryGetValue(id, out bool was);
            lastStates[id] = now;
            return now && !was;
        }

        private static VRRig[] rigCache = new VRRig[0];
        private static float nextRigRefresh;

        public static IEnumerable<VRRig> Rigs
        {
            get
            {
                if (Time.time >= nextRigRefresh)
                {
                    rigCache = Object.FindObjectsOfType<VRRig>();
                    nextRigRefresh = Time.time + 0.5f;
                }
                return rigCache.Where(r => r != null && r.gameObject.activeInHierarchy);
            }
        }

        public static IEnumerable<VRRig> OtherRigs =>
            Rigs.Where(r => !r.isLocal && !r.isOfflineVRRig && (Tagger == null || r != Tagger.offlineVRRig));

        public static bool IsTagged(VRRig rig) => rig != null && rig.setMatIndex != 0;

        public static Color PlayerColor(VRRig rig)
        {
            Color c = rig.playerColor;
            if (Menu.IsOn("Follow Menu Theme")) c = Theme.Accent;
            if (Menu.IsOn("Transparent Theme")) c.a = 0.45f;
            return c;
        }

        public static int VisualLayer => Menu.IsOn("Hidden on Camera") ? 19 : 0;

        private static Material unlit;
        public static Material Unlit
        {
            get
            {
                if (unlit == null) unlit = new Material(Shader.Find("GUI/Text Shader"));
                return unlit;
            }
        }

        public static Material NewUnlit(Color c)
        {
            Material m = new Material(Shader.Find("GUI/Text Shader")) { color = c };
            return m;
        }

        public static GameObject Primitive(PrimitiveType type, Vector3 pos, Vector3 scale, Color color, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = NewUnlit(color);
            return go;
        }

        public static LineRenderer NewLine(float width, Color color)
        {
            GameObject go = new GameObject("RivenLine");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = NewUnlit(color);
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startColor = color;
            lr.endColor = color;
            return lr;
        }

        public static void SetLine(LineRenderer lr, Vector3 a, Vector3 b, Color c)
        {
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.material.color = c;
            lr.startColor = c;
            lr.endColor = c;
            lr.gameObject.layer = VisualLayer;
        }

        public static void Teleport(Vector3 pos)
        {
            if (!Ready) return;
            Player.TeleportTo(pos, Player.transform.rotation, false, false);
            Body.velocity = Vector3.zero;
        }

        public static void AntiGravity(float amount)
        {
            if (Body == null) return;
            Body.AddForce(-Physics.gravity * amount * Body.mass, ForceMode.Force);
        }

        public static void SetVelocity(Vector3 v)
        {
            if (Body != null) Body.velocity = v;
        }

        public static int RaycastMask
        {
            get
            {
                int ignore = 0;
                foreach (string layer in new[] { "Gorilla Trigger", "Gorilla Boundary", "GorillaCosmetics", "GorillaParticle", "Zone", "Ignore Raycast", "TransparentFX", "Gorilla Head", "Gorilla Body Collider" })
                {
                    int l = LayerMask.NameToLayer(layer);
                    if (l >= 0) ignore |= 1 << l;
                }
                return ~ignore;
            }
        }

        public static Color GunColor
        {
            get
            {
                switch (Menu.Option("Gun Color"))
                {
                    case "White": return Color.white;
                    case "Red": return new Color(1f, 0.25f, 0.3f);
                    case "Green": return new Color(0.3f, 1f, 0.45f);
                    case "Cyan": return new Color(0.3f, 0.95f, 1f);
                    default: return Theme.Accent;
                }
            }
        }

        public static bool Typing => MenuGUI.IsTyping;
        public static bool Hotkey(Key key) => !Typing && KeyDown(key);
        public static bool HeldKey(Key key) => !Typing && Key(key);

        public static bool Grounded =>
            Ready && (Player.BodyOnGround || Player.IsGroundedHand || Physics.Raycast(BodyT.position, Vector3.down, 0.9f, RaycastMask));

        private static readonly HashSet<string> rigOwners = new HashSet<string>();

        public static bool RigTaken => rigOwners.Count > 0;

        public static bool TakeRig(string owner)
        {
            if (LocalRig == null) return false;
            rigOwners.Add(owner);
            if (LocalRig.enabled) LocalRig.enabled = false;
            return true;
        }

        public static void ReleaseRig(string owner)
        {
            rigOwners.Remove(owner);
            if (rigOwners.Count == 0 && LocalRig != null && !LocalRig.enabled) LocalRig.enabled = true;
        }

        public static void PlaceRig(Vector3 position, Quaternion rotation)
        {
            if (LocalRig == null) return;
            LocalRig.transform.SetPositionAndRotation(position, rotation);
        }

        public static void PoseRig(Vector3 head, Quaternion headRot, Vector3 left, Vector3 right)
        {
            if (LocalRig == null) return;
            if (LocalRig.head?.rigTarget != null) LocalRig.head.rigTarget.rotation = headRot;
            if (LocalRig.leftHand?.rigTarget != null) LocalRig.leftHand.rigTarget.position = left;
            if (LocalRig.rightHand?.rigTarget != null) LocalRig.rightHand.rigTarget.position = right;
        }

        public static void DestroyAll<T>(ICollection<T> objects) where T : Object
        {
            foreach (T o in objects)
                if (o != null)
                {
                    if (o is Component comp) Object.Destroy(comp.gameObject);
                    else Object.Destroy(o);
                }
            objects.Clear();
        }
    }
}
