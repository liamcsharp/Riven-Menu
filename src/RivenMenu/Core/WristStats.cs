using UnityEngine;

namespace RivenMenu
{
    public class WristStats : MonoBehaviour
    {
        public static bool Shown;
        public static bool NowPlaying;

        private TextMesh stats;
        private TextMesh song;
        private string statsText = "", songText = "";
        private float fps;
        private int frames;
        private float timer;
        private float nextText;

        private void Awake() => DontDestroyOnLoad(gameObject);

        private static TextMesh MakeLabel(string name, float size)
        {
            GameObject go = new GameObject(name);
            DontDestroyOnLoad(go);
            TextMesh t = go.AddComponent<TextMesh>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.GetComponent<MeshRenderer>().material = t.font.material;
            t.fontSize = 48;
            t.characterSize = size;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.richText = true;
            return t;
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

            bool ready = Util.Ready && Util.RightHand != null;
            bool showStats = Shown && ready;
            bool showSong = NowPlaying && ready && SystemMedia.Instance != null;

            if (stats != null && stats.gameObject.activeSelf != showStats) stats.gameObject.SetActive(showStats);
            if (song != null && song.gameObject.activeSelf != showSong) song.gameObject.SetActive(showSong);
            if (!showStats && !showSong) return;

            Transform hand = Util.RightHand;
            Camera cam = Camera.main;
            Vector3 basePos = hand.position + Vector3.up * 0.07f;
            Quaternion face = cam != null ? Quaternion.LookRotation(basePos - cam.transform.position) : hand.rotation;
            bool refresh = Time.unscaledTime >= nextText;
            if (refresh) nextText = Time.unscaledTime + 0.25f;
            string acc = Theme.AccentHex;

            if (showStats)
            {
                if (stats == null) stats = MakeLabel("RivenStatsLabel", 0.0015f);
                stats.transform.SetPositionAndRotation(basePos, face);
                if (refresh)
                {
                    string next = "<color=#" + acc + ">" + Mathf.RoundToInt(fps) + "</color> fps   <color=#" + acc + ">" + Mods.Media.Ping + "</color> ms";
                    if (next != statsText) { statsText = next; stats.text = next; }
                }
            }

            if (showSong)
            {
                if (song == null) song = MakeLabel("RivenSongLabel", 0.0012f);
                song.transform.SetPositionAndRotation(basePos + Vector3.up * (showStats ? 0.035f : 0f), face);
                if (refresh)
                {
                    SystemMedia m = SystemMedia.Instance;
                    string next;
                    if (!m.Found) next = "<color=#" + acc + ">♪</color> no player";
                    else if (!m.Playing) next = "<color=#" + acc + ">♪</color> paused";
                    else next = "<color=#" + acc + ">♪</color> " + Trim(m.Track, 22) + "\n<size=30>" + Trim(m.Artist, 26) + "</size>";
                    if (next != songText) { songText = next; song.text = next; }
                }
            }
        }

        private static string Trim(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
