using System.Collections.Generic;
using UnityEngine;

namespace ViroLab.Pasteurizador
{
    // Vista explosionada: cada subsistema se desplaza hacia afuera de su centroide
    // en el plano horizontal local. Interpolacion con SmoothStep en Update.
    [DisallowMultipleComponent]
    public class PasteurizerExplodedView : MonoBehaviour
    {
        [Header("Refs")]
        public PasteurizerPartsRegistry registry;

        [Header("Settings")]
        [Tooltip("Distancia en metros que cada subsistema se aleja del centro global.")]
        public float explodeDistance = 1.5f;
        [Tooltip("Velocidad de la transicion (mayor = mas rapido).")]
        public float lerpSpeed = 4f;
        [Tooltip("Si true, ignora el eje Y al calcular direccion de explosion (se queda plano).")]
        public bool flatExplosion = true;

        private struct PartSnapshot
        {
            public Transform t;
            public Vector3 origin;
            public Vector3 target;
        }

        private readonly List<PartSnapshot> _snapshots = new();
        private bool _exploded = false;
        private bool _initialized = false;
        private float _t = 0f;

        public bool IsExploded => _exploded;

        private void Awake()
        {
            if (registry == null) registry = GetComponent<PasteurizerPartsRegistry>();
        }

        private void Start() => RebuildSnapshots();

        public void RebuildSnapshots()
        {
            _snapshots.Clear();
            if (registry == null) { _initialized = false; return; }

            // Centroide global y por subsistema (en coords mundiales)
            var subsystemCenters = new Dictionary<string, Vector3>();
            var subsystemCounts = new Dictionary<string, int>();

            foreach (var kvp in registry.BySubsystem)
            {
                Vector3 sum = Vector3.zero;
                int n = 0;
                foreach (var go in kvp.Value)
                {
                    sum += go.transform.position;
                    n++;
                }
                if (n > 0)
                {
                    subsystemCenters[kvp.Key] = sum / n;
                    subsystemCounts[kvp.Key] = n;
                }
            }

            if (subsystemCenters.Count == 0) { _initialized = false; return; }

            Vector3 overall = Vector3.zero;
            foreach (var c in subsystemCenters.Values) overall += c;
            overall /= subsystemCenters.Count;

            // Direccion por subsistema (radial desde overall)
            var subsystemDir = new Dictionary<string, Vector3>();
            foreach (var kvp in subsystemCenters)
            {
                var dir = kvp.Value - overall;
                if (flatExplosion) dir.y = 0;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
                subsystemDir[kvp.Key] = dir.normalized;
            }

            foreach (var kvp in registry.BySubsystem)
            {
                if (!subsystemDir.TryGetValue(kvp.Key, out var dir)) continue;
                foreach (var go in kvp.Value)
                {
                    var t = go.transform;
                    _snapshots.Add(new PartSnapshot
                    {
                        t = t,
                        origin = t.position,
                        target = t.position + dir * explodeDistance,
                    });
                }
            }
            _initialized = true;
        }

        public void Toggle() => SetExploded(!_exploded);

        public void SetExploded(bool on)
        {
            _exploded = on;
            if (!_initialized) RebuildSnapshots();
        }

        private void Update()
        {
            if (!_initialized) return;
            _t = Mathf.MoveTowards(_t, _exploded ? 1f : 0f, Time.deltaTime * lerpSpeed);
            float s = Mathf.SmoothStep(0f, 1f, _t);
            foreach (var snap in _snapshots)
            {
                if (snap.t == null) continue;
                snap.t.position = Vector3.LerpUnclamped(snap.origin, snap.target, s);
            }
        }
    }
}
