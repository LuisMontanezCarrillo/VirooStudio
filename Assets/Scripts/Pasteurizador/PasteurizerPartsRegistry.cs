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

        // partName -> GameObject (el GO con MeshRenderer/MeshFilter, salvo en los
        // tanques FBX, donde es la raiz marcada con PasteurizerFBXTankInfo)
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
            var indexados = new HashSet<GameObject>();
            foreach (var r in renderers)
            {
                var go = r.gameObject;

                // Los dos tanques vienen del mismo FBX, cuyo unico nodo con malla se
                // llama "tripo_node_a7bcadc3" en AMBOS. Indexarlo por ese nombre es
                // inservible: no distingue un tanque del otro, ResolveSubsystemKey lo
                // manda a "99_Otros" y el ContainsKey de abajo descarta el segundo.
                // Se indexa en su lugar la raiz del FBX, que PasteurizerFBXReplacements
                // marca con PasteurizerFBXTankInfo y nombra T_RAW_FBX_* / T_PROD_FBX_*,
                // nombres que ResolveSubsystemKey si reconoce. Es tambien la raiz la que
                // recibe el collider en AddColliders, y solo estando en ByName la acepta
                // el IsValidPart del hover.
                // includeInactive: el barrido de renderers incluye objetos apagados y
                // GetComponentInParent, por defecto, ignora los padres inactivos.
                var tanque = r.GetComponentInParent<PasteurizerFBXTankInfo>(true);
                if (tanque != null) go = tanque.gameObject;
                if (!indexados.Add(go)) continue;

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
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = convexColliders;
                    continue;
                }

                // Los tanques FBX no tienen malla propia: la parte indexada es la raiz
                // y el mesh cuelga de un hijo, asi que el camino de arriba los deja sin
                // collider y el rayo los atraviesa. Tampoco sirve un MeshCollider
                // convex sobre el hijo, porque el FBX esta importado con isReadable = 0
                // y en runtime no se puede cocinar. Se usa un BoxCollider ajustado.
                if (go.GetComponent<PasteurizerFBXTankInfo>() != null)
                    AgregarBoxDesdeMallaHija(go);
            }
        }

        /// Coloca en `raiz` un BoxCollider que envuelve la malla de su hijo.
        /// Mesh.bounds es metadato del asset y esta disponible aunque la malla no sea
        /// legible, que es justo el caso de los tanques.
        private static void AgregarBoxDesdeMallaHija(GameObject raiz)
        {
            // Se busca el primer MeshFilter CON malla: GetComponentInChildren mira
            // primero la propia raiz, y si algun dia esta llevara un MeshFilter vacio
            // nos quedariamos con el y sin collider.
            MeshFilter mf = null;
            foreach (var c in raiz.GetComponentsInChildren<MeshFilter>(true))
            {
                if (c.sharedMesh != null) { mf = c; break; }
            }
            if (mf == null) return;

            // Se transforman las 8 esquinas y se reencapsulan, en lugar de escalar el
            // size: asi el resultado es correcto sea cual sea la rotacion relativa
            // entre el hijo y la raiz, que ademas varia (el prefab trae -90 en X y la
            // escena lo sobrescribe a 180 en Z).
            var mb = mf.sharedMesh.bounds;
            var t = raiz.transform;
            Bounds local = default;
            for (int i = 0; i < 8; i++)
            {
                var signo = new Vector3((i & 1) == 0 ? -1f : 1f,
                                        (i & 2) == 0 ? -1f : 1f,
                                        (i & 4) == 0 ? -1f : 1f);
                var p = t.InverseTransformPoint(
                    mf.transform.TransformPoint(mb.center + Vector3.Scale(mb.extents, signo)));
                if (i == 0) local = new Bounds(p, Vector3.zero);
                else local.Encapsulate(p);
            }

            var bc = raiz.AddComponent<BoxCollider>();
            bc.center = local.center;
            bc.size = local.size;

            var mundo = Vector3.Scale(local.size, t.lossyScale);
            Debug.Log($"<color=cyan>[Pasteurizador]</color> Collider de {raiz.name}: " +
                      $"{Mathf.Abs(mundo.x):F2} x {Mathf.Abs(mundo.y):F2} x {Mathf.Abs(mundo.z):F2} m");
        }

        public string ResolveSubsystemKey(string partName)
            => database != null ? database.ResolveSubsystemKey(partName) : null;
    }
}
