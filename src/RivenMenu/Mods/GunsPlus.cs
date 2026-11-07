using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RivenMenu.Mods
{
    public static class GunsPlus
    {
        private const string Cat = "Guns";

        private static readonly List<GameObject> paint = new List<GameObject>();
        private static float nextPaint;

        [MenuButton("Paint Gun", Category = Cat, Order = 30, OnDisable = nameof(PaintOff), Tooltip = "Hold fire to spray paint on anything, only you can see it")]
        public static void PaintGun()
        {
            if (!GunLib.World(out GunHit g) || !g.Fire || !g.Hit || Time.time < nextPaint) return;
            nextPaint = Time.time + 0.03f;
            GameObject dot = Util.Primitive(PrimitiveType.Quad, g.Point + g.Normal * 0.005f, Vector3.one * Random.Range(0.08f, 0.16f), Theme.Hue(Time.time * 0.3f));
            dot.GetComponent<Renderer>().material.mainTexture = Tex.SoftDot;
            dot.transform.rotation = Quaternion.LookRotation(-g.Normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            dot.layer = Util.VisualLayer;
            paint.Add(dot);
            if (paint.Count > 600)
            {
                if (paint[0] != null) Object.Destroy(paint[0]);
                paint.RemoveAt(0);
            }
        }

        public static void PaintOff() => GunLib.Hide();

        [MenuButton("Clear Paint", ButtonType.Button, Category = Cat, Order = 31, Tooltip = "Removes all your paint")]
        public static void ClearPaint() => Util.DestroyAll(paint);

        private static LineRenderer laser;

        [MenuButton("Laser Gun", Category = Cat, Order = 32, OnDisable = nameof(LaserOff), Tooltip = "Hold fire to shoot a thick glowing laser beam, only you can see it")]
        public static void LaserGun()
        {
            if (!GunLib.World(out GunHit g) || !g.Fire)
            {
                if (laser != null) laser.gameObject.SetActive(false);
                return;
            }
            if (laser == null) laser = Util.NewLine(0.06f, Color.white);
            laser.gameObject.SetActive(true);
            float w = 0.05f + Mathf.Sin(Time.time * 40f) * 0.015f;
            laser.startWidth = w;
            laser.endWidth = w * 0.6f;
            Util.SetLine(laser, g.Origin, g.Point, Theme.Lighten(Theme.Accent, 0.3f));
        }

        public static void LaserOff()
        {
            if (laser != null) Object.Destroy(laser.gameObject);
            laser = null;
            GunLib.Hide();
        }

        private static void Burst(Vector3 at, Color c, int count, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject p = Util.Primitive(PrimitiveType.Sphere, at, Vector3.one * size, c, true);
                p.layer = Util.VisualLayer;
                Rigidbody rb = p.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.velocity = Random.onUnitSphere * speed + Vector3.up * speed * 0.3f;
                Collider col = p.GetComponent<Collider>();
                if (Util.Player != null) Physics.IgnoreCollision(col, Util.Player.bodyCollider);
                Object.Destroy(p, Random.Range(1.2f, 2.2f));
            }
        }

        [MenuButton("Explosion Gun", Category = Cat, Order = 33, OnDisable = nameof(GunsOff), Tooltip = "Fire to make a burst of sparks where you aim, only you can see it")]
        public static void ExplosionGun()
        {
            if (GunLib.World(out GunHit g) && g.FireDown) Burst(g.Point + g.Normal * 0.1f, new Color(1f, 0.55f, 0.15f), 24, 6f, 0.07f);
        }

        [MenuButton("Firework Gun", Category = Cat, Order = 34, OnDisable = nameof(GunsOff), Tooltip = "Fire to pop rainbow fireworks where you aim, only you can see it")]
        public static void FireworkGun()
        {
            if (!GunLib.World(out GunHit g) || !g.FireDown) return;
            Vector3 at = g.Point + g.Normal * 0.2f + Vector3.up * 1.5f;
            for (int i = 0; i < 3; i++) Burst(at + Random.insideUnitSphere * 0.4f, Theme.Hue(Random.value), 14, 4f, 0.05f);
        }

        public static void GunsOff() => GunLib.Hide();

        private static readonly List<GameObject> markers = new List<GameObject>();

        [MenuButton("Marker Gun", Category = Cat, Order = 35, OnDisable = nameof(GunsOff), Tooltip = "Fire to drop a glowing marker pole, only you can see it")]
        public static void MarkerGun()
        {
            if (!GunLib.World(out GunHit g) || !g.FireDown || !g.Hit) return;
            GameObject pole = Util.Primitive(PrimitiveType.Cylinder, g.Point + Vector3.up * 1f, new Vector3(0.05f, 1f, 0.05f), Theme.Accent);
            GameObject top = Util.Primitive(PrimitiveType.Sphere, g.Point + Vector3.up * 2.05f, Vector3.one * 0.2f, Theme.Lighten(Theme.Accent, 0.4f));
            top.transform.SetParent(pole.transform, true);
            pole.layer = Util.VisualLayer;
            markers.Add(pole);
            if (markers.Count > 20)
            {
                if (markers[0] != null) Object.Destroy(markers[0]);
                markers.RemoveAt(0);
            }
        }

        [MenuButton("Clear Markers", ButtonType.Button, Category = Cat, Order = 36, Tooltip = "Removes all your markers")]
        public static void ClearMarkers() => Util.DestroyAll(markers);

        private static LineRenderer measure;
        private static Vector3? measureStart;

        [MenuButton("Measure Gun", Category = Cat, Order = 37, OnDisable = nameof(MeasureOff), Tooltip = "Fire once to start and again to measure the distance between two spots")]
        public static void MeasureGun()
        {
            if (!GunLib.World(out GunHit g)) return;
            if (g.FireDown && g.Hit)
            {
                if (measureStart == null) measureStart = g.Point;
                else
                {
                    Notify.Push(Vector3.Distance(measureStart.Value, g.Point).ToString("0.00") + " meters");
                    measureStart = null;
                }
            }
            if (measureStart == null)
            {
                if (measure != null) measure.gameObject.SetActive(false);
                return;
            }
            if (measure == null) measure = Util.NewLine(0.015f, Color.white);
            measure.gameObject.SetActive(true);
            Util.SetLine(measure, measureStart.Value, g.Point, Theme.Accent2);
        }

        public static void MeasureOff()
        {
            measureStart = null;
            if (measure != null) Object.Destroy(measure.gameObject);
            measure = null;
            GunLib.Hide();
        }
    }
}
