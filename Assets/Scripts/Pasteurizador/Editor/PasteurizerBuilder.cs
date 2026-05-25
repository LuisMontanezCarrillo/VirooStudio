#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Builder de un click (v12 - multi-OBJ split por subsistema):
    ///   1) Asegura import settings de los 31 OBJs (escala 0.001, sin lightmap UVs)
    ///   2) Genera materiales URP y los guarda como assets
    ///   3) Crea ScriptableObject del subsystem database desde subsystems.json
    ///   4) Instancia los 31 OBJs como hijos de un prefab unificado
    ///      Pasteurizador_HTST con todos los componentes (registry, hover,
    ///      material assigner, exploded, controller).
    ///   5) Opcionalmente reemplaza el GameObject "Pasteurizer" en la escena.
    ///
    /// El OBJ original era 155 MB con 860 sub-meshes y Unity 6 se colgaba
    /// importandolo. La solucion fue partirlo en 31 OBJs chicos por
    /// subsistema (ver split_obj_por_subsistema.py).
    public static class PasteurizerBuilder
    {
        private const string SplitDir = "Assets/Models/Pasteurizador_HTST/Split";
        private const string MaterialsDir = "Assets/Models/Pasteurizador_HTST/Materials";
        private const string PrefabPath = "Assets/Prefabs/Pasteurizador_HTST/Pasteurizador_HTST.prefab";
        private const string PrefabDir = "Assets/Prefabs/Pasteurizador_HTST";
        private const string DatabaseAssetPath = "Assets/Resources/Pasteurizador/SubsystemDatabase.asset";
        private const string JsonResourcePath = "Pasteurizador/subsystems";

        [MenuItem("Viroo/Pasteurizador HTST/1. Construir prefab desde OBJ", priority = 100)]
        public static void BuildPrefab()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Verificando OBJs split", 0.05f);
                if (!Directory.Exists(SplitDir))
                {
                    EditorUtility.DisplayDialog("Pasteurizador HTST",
                        $"No encuentro la carpeta {SplitDir}\nVerifica que los OBJs esten copiados.", "OK");
                    return;
                }
                var objPaths = Directory.GetFiles(SplitDir, "*.obj")
                                         .OrderBy(p => p).ToArray();
                if (objPaths.Length == 0)
                {
                    EditorUtility.DisplayDialog("Pasteurizador HTST",
                        $"No hay archivos .obj en {SplitDir}", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Pasteurizador HTST",
                    $"Asegurando import settings de {objPaths.Length} OBJs", 0.10f);
                foreach (var p in objPaths) EnsureObjImportSettings(p);

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Generando 15 materiales URP", 0.30f);
                var palette = BuildAndSaveMaterials();

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Creando ScriptableObject database", 0.40f);
                var db = CreateOrUpdateDatabase();

                EditorUtility.DisplayProgressBar("Pasteurizador HTST",
                    $"Instanciando {objPaths.Length} OBJs y configurando", 0.55f);
                Directory.CreateDirectory(PrefabDir);

                // Crea el GameObject raiz contenedor
                var root = new GameObject("Pasteurizador_HTST");

                // Mapa de materiales por regla
                var matMap = new Dictionary<PasteurizerMaterialAssigner.PaletteRule, Material>();
                foreach (var e in palette) matMap[e.rule] = e.material;

                // Instanciar cada OBJ como hijo del root, sin transform offset
                // (cada OBJ ya tiene sus vertices en coords FreeCAD).
                int i = 0;
                foreach (var objPath in objPaths)
                {
                    i++;
                    EditorUtility.DisplayProgressBar("Pasteurizador HTST",
                        $"Instanciando {Path.GetFileNameWithoutExtension(objPath)} ({i}/{objPaths.Length})",
                        0.55f + (0.25f * i / objPaths.Length));

                    var objAsset = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
                    if (objAsset == null) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(objAsset, root.transform);
                    inst.name = Path.GetFileNameWithoutExtension(objPath);

                    // Aplicar materiales URP por nombre a cada renderer
                    foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var rule = PasteurizerMaterialAssigner.ClassifyName(r.gameObject.name);
                        if (matMap.TryGetValue(rule, out var m)) r.sharedMaterial = m;
                    }
                }

                // Componentes en el root
                var registry = root.AddComponent<PasteurizerPartsRegistry>();
                registry.SetDatabase(db);
                registry.addCollidersOnAwake = true;
                registry.convexColliders = true;

                var assigner = root.AddComponent<PasteurizerMaterialAssigner>();
                assigner.palette = palette;

                var hover = root.AddComponent<PasteurizerHoverHandler>();
                hover.registry = registry;
                hover.alsoUseMouse = true;

                var exploded = root.AddComponent<PasteurizerExplodedView>();
                exploded.registry = registry;

                var controller = root.AddComponent<UnityPasteurizerController>();

                // Guardar como prefab
                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Guardando prefab", 0.95f);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
                Selection.activeObject = saved;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> Prefab generado con {objPaths.Length} OBJs: {PrefabPath}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Viroo/Pasteurizador HTST/2. Reemplazar Pasteurizer en escena activa", priority = 101)]
        public static void ReplaceInActiveScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No existe {PrefabPath}.\nPrimero corre 'Construir prefab desde OBJ'.", "OK");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            // Buscar GameObject llamado "Pasteurizer" en cualquier nivel
            GameObject target = null;
            foreach (var rootGo in scene.GetRootGameObjects())
            {
                target = FindInChildren(rootGo.transform, "Pasteurizer");
                if (target != null) break;
            }

            Transform parent = null;
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            Vector3 scale = Vector3.one;
            int siblingIndex = -1;

            if (target != null)
            {
                parent = target.transform.parent;
                pos = target.transform.position;
                rot = target.transform.rotation;
                scale = target.transform.localScale;
                siblingIndex = target.transform.GetSiblingIndex();
                Undo.RegisterFullObjectHierarchyUndo(target, "Replace Pasteurizer");
                Undo.DestroyObjectImmediate(target);
            }
            else
            {
                Debug.LogWarning("[Pasteurizador HTST] No encontre GameObject llamado 'Pasteurizer'. Instancio en raiz.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Replace Pasteurizer");
            if (parent != null) instance.transform.SetParent(parent, false);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            if (siblingIndex >= 0) instance.transform.SetSiblingIndex(siblingIndex);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = instance;
            Debug.Log("<color=cyan>[Pasteurizador HTST]</color> Reemplazado en escena.");
        }

        [MenuItem("Viroo/Pasteurizador HTST/Reaplicar materiales URP", priority = 110)]
        public static void ReapplyMaterials()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return;
            var pal = BuildAndSaveMaterials();
            var map = new Dictionary<PasteurizerMaterialAssigner.PaletteRule, Material>();
            foreach (var e in pal) map[e.rule] = e.material;
            var instance = (GameObject)PrefabUtility.LoadPrefabContents(PrefabPath);
            foreach (var r in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                var rule = PasteurizerMaterialAssigner.ClassifyName(r.gameObject.name);
                if (map.TryGetValue(rule, out var m)) r.sharedMaterial = m;
            }
            var assigner = instance.GetComponent<PasteurizerMaterialAssigner>();
            if (assigner != null) assigner.palette = pal;
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
            Debug.Log("[Pasteurizador HTST] Materiales reaplicados.");
        }

        // ----- Helpers -----

        private static void EnsureObjImportSettings(string objPath)
        {
            var imp = AssetImporter.GetAtPath(objPath) as ModelImporter;
            if (imp == null) return;
            bool changed = false;
            if (Mathf.Abs(imp.globalScale - 0.001f) > 0.00001f) { imp.globalScale = 0.001f; changed = true; }
            if (imp.useFileScale) { imp.useFileScale = false; changed = true; }
            if (!imp.isReadable) { imp.isReadable = true; changed = true; }
            // generateSecondaryUV DESACTIVADO (con muchos sub-meshes el
            // unwrapper de lightmap puede colgar Unity).
            if (imp.generateSecondaryUV) { imp.generateSecondaryUV = false; changed = true; }
            if (imp.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            { imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; changed = true; }
            if (imp.materialLocation != ModelImporterMaterialLocation.External)
            { imp.materialLocation = ModelImporterMaterialLocation.External; changed = true; }
            if (changed)
            {
                imp.SaveAndReimport();
            }
        }

        private static List<PasteurizerMaterialAssigner.PaletteEntry> BuildAndSaveMaterials()
        {
            Directory.CreateDirectory(MaterialsDir);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var list = PasteurizerMaterialAssigner.BuildDefaultPaletteURP(shader);
            for (int i = 0; i < list.Count; i++)
            {
                var path = $"{MaterialsDir}/{list[i].material.name}.mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(list[i].material, path);
                }
                else
                {
                    existing.shader = shader;
                    existing.CopyPropertiesFromMaterial(list[i].material);
                    list[i].material = existing;
                    EditorUtility.SetDirty(existing);
                }
            }
            AssetDatabase.SaveAssets();
            return list;
        }

        private static PasteurizerSubsystemDatabase CreateOrUpdateDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabaseAssetPath));
            var db = AssetDatabase.LoadAssetAtPath<PasteurizerSubsystemDatabase>(DatabaseAssetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<PasteurizerSubsystemDatabase>();
                AssetDatabase.CreateAsset(db, DatabaseAssetPath);
            }
            var json = Resources.Load<TextAsset>(JsonResourcePath);
            if (json == null)
            {
                Debug.LogError($"No encontre Resources/{JsonResourcePath}.json");
                return db;
            }
            var root = JsonUtility.FromJson<SubsystemsJsonRoot>(json.text);
            db.PopulateFrom(root);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            return db;
        }

        private static GameObject FindInChildren(Transform root, string name)
        {
            if (root.name == name) return root.gameObject;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindInChildren(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
#endif
