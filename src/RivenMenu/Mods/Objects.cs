using UnityEngine;

namespace RivenMenu.Mods
{
    public static class Objects
    {
        public static string[] ObjectKinds => PropManager.Kinds;

        private static string Selected => PropManager.Kinds[Menu.Index("Spawn Object")];

        private static Vector3 InFront =>
            Util.Head != null ? Util.Head.position + Util.Head.forward * 0.7f - Vector3.up * 0.1f : Vector3.zero;

        [MenuButton("Spawn Object", ButtonType.Option, Category = "Objects", Order = 0, OptionsFrom = nameof(ObjectKinds), Notify = false, Tooltip = "Pick which object to spawn, only you can see them")]
        public static void SpawnObjectPick() { }

        [MenuButton("Spawn", ButtonType.Button, Category = "Objects", Order = 1, Tooltip = "Drops the picked object in front of you, grab it with grip")]
        public static void Spawn()
        {
            if (!Util.Ready) { Notify.Push("Not in game yet"); return; }
            PropManager.Ensure().Spawn(Selected, InFront);
            Notify.Push("Spawned " + Selected);
        }

        private static float nextGunSpawn;

        [MenuButton("Spawn Gun", Category = "Objects", Order = 2, OnDisable = nameof(GunOff), Tooltip = "Grip to aim, trigger to drop the picked object where you point")]
        public static void SpawnGun()
        {
            if (!GunLib.World(out GunHit g)) return;
            if (g.Fire && Time.time >= nextGunSpawn)
            {
                nextGunSpawn = Time.time + 0.25f;
                PropManager.Ensure().Spawn(Selected, g.Point + g.Normal * 0.3f);
            }
        }

        public static void GunOff() => GunLib.Hide();

        [MenuButton("Spawn 5", ButtonType.Button, Category = "Objects", Order = 3, Tooltip = "Drops five of the picked object around you")]
        public static void SpawnFive()
        {
            if (!Util.Ready) { Notify.Push("Not in game yet"); return; }
            PropManager m = PropManager.Ensure();
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Vector3 p = Util.BodyT.position + new Vector3(Mathf.Cos(a), 1.2f, Mathf.Sin(a)) * 1.2f;
                m.Spawn(Selected, p);
            }
        }

        [MenuButton("Blast Objects", ButtonType.Button, Category = "Objects", Order = 4, Tooltip = "Launches every object away from you")]
        public static void BlastObjects()
        {
            if (PropManager.Instance == null || Util.BodyT == null) return;
            PropManager.Instance.Nudge(Util.BodyT.position, 14f);
        }

        [MenuButton("Clear Objects", ButtonType.Button, Category = "Objects", Order = 5, Tooltip = "Removes every object you spawned")]
        public static void ClearObjects()
        {
            if (PropManager.Instance != null) PropManager.Instance.Clear();
        }

        public static void CountOn() => Hud.Show("objcount", () => (PropManager.Instance != null ? PropManager.Instance.Count : 0) + " objects");
        public static void CountOff() => Hud.Hide("objcount");

        [MenuButton("Object Counter", Category = "Objects", Order = 6, OnEnable = nameof(CountOn), OnDisable = nameof(CountOff), Tooltip = "Shows how many objects you have spawned")]
        public static void ObjectCounter() { }
    }
}
