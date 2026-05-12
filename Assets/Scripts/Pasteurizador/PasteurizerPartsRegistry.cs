using System.Collections.Generic;
using UnityEngine;

namespace ViroLab.Pasteurizador
{
    // Indexa las 654 partes del modelo por nombre y por subsistema.
    // Tambien agrega un MeshCollider en cada parte para soportar Raycast (hover/click).
    [DisallowMultipleComponent]
    public class PasteurizerPartsRegistry : MonoBehaviour
    {
        [SerializeField] private PasteurizerSubsystemDatabase database;
        [Tooltip("Agrega MeshCollider convex a cada parte para Raycast.")]
        public bool addCollidersOnAwake = true;
        [Tooltip("Si true los colliders son convex (mas barato pero menos preciso).")]
        public bool convexColliders = true;

        public PasteurizerSubsystemDatabase Database => database;

        // partName -> GameObject (el GO con MeshRenderer/MeshFilter)
        public readonly Dictionary<string, GameObject> ByName = new();
        // subsystemKey -> lista de GameObjects que pertenecen a ese subsistema
        public readonly Dictionary<string, List<GameObject>> BySubsystem = new();

        public int PartsCount => ByName.Count;
        public int SubsystemCount => BySubsystem.Count;

        private void Awake()
        {
            if (database == null)
            {
                database = PasteurizerSubsystemDatabase.LoadFromResources();
            }
            BuildIndex();
            if (addCollidersOnAwake) AddColliders();
        }

        public void SetDatabase(PasteurizerSubsystemDatabase db) => database = db;

        public void BuildIndex()
        {
            ByName.Clear();
            BySubsystem.Clear();
            if (database == null)
            {
                Debug.LogError("PasteurizerPartsRegistry: database no asignada.");
                return;
            }
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                var go = r.gameObject;
                var nm = go.name;
                if (!ByName.ContainsKey(nm)) ByName[nm] = go;
                var key = database.ResolveSubsystemKey(nm);
                if (!BySubsystem.TryGetValue(key, out var list))
                {
                    list = new List<GameObject>();
                    BySubsystem[key] = list;
                }
                list.Add(go);
            }
        }

        public void AddColliders()
        {
            foreach (var kvp in ByName)
            {
                var go = kvp.Value;
                if (go.TryGetComponent<Collider>(out _)) continue;
                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = convexColliders;
            }
        }

        public string ResolveSubsystemKey(string partName)
            => database != null ? database.ResolveSubsystemKey(partName) : null;
    }
}
