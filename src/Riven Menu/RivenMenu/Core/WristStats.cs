using UnityEngine;

namespace RivenMenu
{
    public class WristStats : MonoBehaviour
    {
        public static bool Shown;

        private TextMesh text;
        private Transform tf;
        private float fps;
        private int frames;
        private float timer;

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Build()
        {
            GameObject go = new GameObject("RivenWristStats");
            tf = go.transform;
            text = go.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().material = text.font.material;
            text.fontSize = 46;
            text.characterSize = 0.0016f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.richText = true;
        }

        private void Update()
        {
            frames++;
            timer += Time.unscaledDeltaTime;
            if (timer >= 0.5f)
            {
                fps = frames / timer;
                frames = 0;
                timer = 0f;
            }

            if (!Shown || !Util.Ready || Util.RightHand == null)
            {
                if (text != null) text.gameObject.SetActive(false);
                return;
            }

            if (text == null) Build();
            text.gameObject.SetActive(true);

            Transform hand = Util.RightHand;
            Camera cam = Camera.main;
            tf.position = hand.position + hand.up * 0.04f;
            if (cam != null) tf.rotation = Quaternion.LookRotation(tf.position - cam.transform.position);
            tf.gameObject.layer = Util.VisualLayer;

            int ping = Mods.Media.Ping;
            string acc = Theme.AccentHex;
            text.text = $"<color=#{acc}>{Mathf.RoundToInt(fps)}</color> fps\n<color=#{acc}>{ping}</color> ms";
        }
    }
}
