#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Coloca 2 instancias del Stainless_steel_tank.fbx como hijos del
    /// prefab Pasteurizador_HTST, para reemplazar visualmente los tanques
    /// parametricos T_RAW_01_* (silo leche cruda) y T_PROD_01_* (tanque
    /// producto pasteurizado) con un modelo realista.
    ///
    /// Ejecutar DESPUES de "1. Construir prefab desde OBJ" y
    /// "2. Reemplazar Pasteurizer en escena activa".
    public static class PasteurizerFBXReplacements
    {
        private const string TankFbxPath =
            "Assets/3D Laboratory Environment with Appratus/Models/Stainless_steel_tank (Tripo).fbx";
        private const string PrefabPath =
            "Assets/Prefabs/Pasteurizador_HTST/Pasteurizador_HTST.prefab";

        // Coordenadas en FreeCAD (mm). Conversion a Unity: (x, z, -y) * 0.001
        // T-RAW-01 silo leche cruda  - LEFT:  (-1000, 700, base=0)
        // T-PROD-01 tanque producto  - RIGHT: ( 4200, 700, base=0)
        private static readonly (string name, Vector3 fcCenterBottom, string tag, string title)[] Tanks =
        {
            ("T_RAW_FBX_SiloLecheCruda",
                new Vector3(-1000f, 700f, 0f), "T-RAW-01", "SILO LECHE CRUDA"),
            ("T_PROD_FBX_TanqueProducto",
                new Vector3( 4200f, 700f, 0f), "T-PROD-01", "TANQUE PRODUCTO"),
        };

        private const float TargetHeightMm = 1800f;
        private const float UnityScale     = 0.001f;  // mm -> m

        [MenuItem("Viroo/Pasteurizador HTST/5. Agregar tanques FBX (Silo + Producto)", priority = 105)]
        public static void AddFBXTanks()
        {
            var tankFbx = AssetDatabase.LoadAssetAtPath<GameObject>(TankFbxPath);
            if (tankFbx == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No encontre el FBX del tanque en\n{TankFbxPath}", "OK");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No encontre el prefab en {PrefabPath}. Primero corre el paso 1.",
                    "OK");
                return;
            }

            try
            {
                foreach (var t in Tanks)
                {
                    // Si ya existe, lo borro y vuelvo a crear (para idempotencia)
                    var existing = prefabRoot.transform.Find(t.name);
                    if (existing != null) Object.DestroyImmediate(existing.gameObject);

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(tankFbx, prefabRoot.transform);
                    inst.name = t.name;

                    // Auto-escalar para que la altura final sea ~1.8 m
                    inst.transform.localScale = Vector3.one;
                    var bbox = ComputeBoundsWorld(inst);
                    if (bbox.size == Vector3.zero) continue;
                    float maxAxis = Mathf.Max(bbox.size.x, bbox.size.y, bbox.size.z);
                    float scale = (TargetHeightMm * UnityScale) / maxAxis;
                    inst.transform.localScale = Vector3.one * scale;

                    // Re-calcular bbox luego de la escala
                    bbox = ComputeBoundsWorld(inst);

                    // FreeCAD (x, y, z=0 piso) -> Unity (x, z=0 piso, -y) en metros
                    var targetCenter = new Vector3(
                        t.fcCenterBottom.x * UnityScale,
                        0f,
                        -t.fcCenterBottom.y * UnityScale);

                    // Centrar XZ en target, alinear bottom Y en 0
                    Vector3 currentCenter = bbox.center;
                    float currentBottomY = bbox.min.y;
                    inst.transform.localPosition += new Vector3(
                        targetCenter.x - currentCenter.x,
                        0f - currentBottomY,
                        targetCenter.z - currentCenter.z);

                    // Marca con descripcion para hover/click
                    var info = inst.AddComponent<PasteurizerFBXTankInfo>();
                    info.isaTag = t.tag;
                    info.title  = t.title;

                    Debug.Log($"<color=cyan>[FBX Tank]</color> {t.name} colocado en " +
                              $"{inst.transform.localPosition}, scale {scale:F4}");
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log("<color=cyan>[Pasteurizador HTST]</color> Tanques FBX agregados al prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem("Viroo/Pasteurizador HTST/6. Ocultar tanques parametricos reemplazados", priority = 106)]
        public static void HideReplacedParametricTanks()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null) return;
            try
            {
                int hidden = 0;
                foreach (var t in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("T_RAW_01_") || t.name.StartsWith("T_PROD_01_"))
                    {
                        t.gameObject.SetActive(false);
                        hidden++;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> {hidden} meshes parametricos T-RAW-01/T-PROD-01 ocultados.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Bounds ComputeBoundsWorld(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds();
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
#endif
