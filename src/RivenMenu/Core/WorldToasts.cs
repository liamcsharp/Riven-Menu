using UnityEngine;
using UnityEngine.XR;

namespace RivenMenu
{
    public class WorldToasts : MonoBehaviour
    {
        private GameObject root;
        private TextMesh text;
        private Material cardMat;
        private string shown = "";
        private Notify.Toast current;
        private Vector3 smoothPos;
        private bool placed;

        private void Update()
        {
            bool vr = XRSettings.isDeviceActive && Util.Head != null;
            Notify.Toast latest = Notify.Toasts.Count > 0 ? Notify.Toasts[Notify.Toasts.Count - 1] : null;
            float now = Time.unscaledTime;
            if (latest != null && now - latest.Time > Notify.Life) latest = null;

            bool show = vr && latest != null;
            if (root == null)
            {
                if (!show) return;
                Build();
            }
            if (root.activeSelf != show) root.SetActive(show);
            if (!show)
            {
                placed = false;
                return;
            }

            if (latest != current)
            {
                current = latest;
                string next = "<color=#" + Theme.AccentHex + "><size=36>RIVEN</size></color>\n" + latest.Text;
                if (next != shown) { shown = next; text.text = next; }
            }

            Transform head = Util.Head;
            Vector3 fwd = head.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 target = head.position + fwd * 0.9f + Vector3.down * 0.32f;
            smoothPos = placed ? Vector3.Lerp(smoothPos, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)) : target;
            placed = true;
            root.transform.SetPositionAndRotation(smoothPos, Quaternion.LookRotation(smoothPos - head.position));

            float age = now - latest.Time;
            float a = Mathf.Clamp01(age / 0.2f) * Mathf.Clamp01((Notify.Life - age) / 0.4f);
            float pop = 0.85f + 0.15f * Mathf.Clamp01(age / 0.2f);
            root.transform.localScale = new Vector3(pop, pop, pop);
            text.color = new Color(1f, 1f, 1f, a);
            Color c = Theme.Current.Panel;
            c.a = 0.85f * a;
            cardMat.color = c;
        }

        private void Build()
        {
            root = new GameObject("RivenWorldToast");
            DontDestroyOnLoad(root);

            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(root.transform, false);
            q.transform.localPosition = new Vector3(0f, 0f, 0.002f);
            q.transform.localScale = new Vector3(0.42f, 0.1f, 1f);
            cardMat = new Material(Shader.Find("GUI/Text Shader")) { mainTexture = Tex.RoundedRect(420, 100, 30f, 0f), renderQueue = 4000 };
            q.GetComponent<Renderer>().material = cardMat;

            GameObject t = new GameObject("text");
            t.transform.SetParent(root.transform, false);
            text = t.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.fontSize = 48;
            text.characterSize = 0.0042f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.richText = true;
            text.fontStyle = FontStyle.Bold;
            Material m = new Material(font.material) { renderQueue = 4001 };
            m.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            t.GetComponent<MeshRenderer>().material = m;
            cardMat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        }
    }
}
