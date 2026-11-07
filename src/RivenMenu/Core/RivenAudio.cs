using System;
using UnityEngine;

namespace RivenMenu
{
    public class RivenAudio : MonoBehaviour
    {
        public static RivenAudio Instance;

        private AudioSource ui;
        private AudioClip welcome;

        private readonly AudioClip[] clickStyles = new AudioClip[4];
        private readonly AudioClip[] onStyles = new AudioClip[4];
        private readonly AudioClip[] offStyles = new AudioClip[4];

        public static int ClickStyle;

        private void Awake()
        {
            Instance = this;
            ui = gameObject.AddComponent<AudioSource>();
            ui.spatialBlend = 0f;
            ui.playOnAwake = false;
            ui.volume = 0.3f;

            BuildStyles();
            welcome = BuildWelcome();
        }

        private void BuildStyles()
        {
            clickStyles[0] = Tone("rv_c0", 1300f, 1300f, 0.035f, 0f);
            onStyles[0] = Tone("rv_n0", 850f, 1500f, 0.07f, 0f);
            offStyles[0] = Tone("rv_f0", 1300f, 650f, 0.07f, 0f);

            clickStyles[1] = Tone("rv_c1", 2000f, 2000f, 0.02f, 0f);
            onStyles[1] = Tone("rv_n1", 1500f, 2400f, 0.05f, 0f);
            offStyles[1] = Tone("rv_f1", 2000f, 1100f, 0.05f, 0f);

            clickStyles[2] = Tone("rv_c2", 420f, 420f, 0.05f, 0.4f);
            onStyles[2] = Tone("rv_n2", 300f, 520f, 0.09f, 0.4f);
            offStyles[2] = Tone("rv_f2", 520f, 240f, 0.09f, 0.4f);

            clickStyles[3] = Noise("rv_c3", 0.03f);
            onStyles[3] = Noise("rv_n3", 0.05f);
            offStyles[3] = Noise("rv_f3", 0.05f);
        }

        public void PlayClick() => Play(clickStyles);
        public void PlayOn() => Play(onStyles);
        public void PlayOff() => Play(offStyles);

        private void Play(AudioClip[] styles)
        {
            if (ui == null) return;
            int i = Mathf.Clamp(ClickStyle, 0, styles.Length - 1);
            if (styles[i] != null) ui.PlayOneShot(styles[i]);
        }

        public void PlayWelcome()
        {
            if (welcome != null && ui != null) ui.PlayOneShot(welcome, 2.8f);
        }

        public float WelcomeLength => welcome != null ? welcome.length : 1.6f;

        private static AudioClip BuildWelcome()
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(Assets.WelcomePcm16Base64);
                int count = bytes.Length / 2;
                if (count == 0) return null;
                float[] samples = new float[count];
                for (int i = 0; i < count; i++)
                {
                    short s = (short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
                    samples[i] = s / 32768f;
                }
                AudioClip clip = AudioClip.Create("Welcome To Riven Menu", count, 1, Assets.WelcomeSampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Riven] welcome build failed: " + e.Message);
                return null;
            }
        }

        private static AudioClip Tone(string name, float f0, float f1, float length, float square)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * length);
            float[] data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                phase += Mathf.Lerp(f0, f1, t) / rate;
                float wave = Mathf.Sin(phase * Mathf.PI * 2f);
                if (square > 0f) wave = Mathf.Lerp(wave, Mathf.Sign(wave), square);
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 8f)) * (1f - t);
                data[i] = wave * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Noise(string name, float length)
        {
            const int rate = 44100;
            int n = Mathf.CeilToInt(rate * length);
            float[] data = new float[n];
            System.Random rng = new System.Random(name.GetHashCode());
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, 0.4f);
                float env = (1f - t) * (1f - t);
                data[i] = last * env * 0.7f;
            }
            AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
