#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ViroLab.Pasteurizador.EditorTools
{
    // Builder de un click:
    //   1) Importa el OBJ con escala 0.001
    //   2) Genera materiales URP y los guarda como assets
    //   3) Crea ScriptableObject del subsystem database
    //   4) Crea prefab Pasteurizador_HTST con todos los componentes
    //   5) Opcionalmente reemplaza el GameObject "Pasteurizer" en la escena activa
    public static class PasteurizerBuilder
    {
        private const string ObjPath = "Assets/Models/Pasteurizador_HTST/pasteurizador_htst.obj";
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
                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Verificando OBJ", 0.05f);
                if (!File.Exists(ObjPath))
                {
                    EditorUtility.DisplayDialog("Pasteurizador HTST",
                        $"No encuentro el OBJ en {ObjPath}\nVerifica que el archivo este copiado.", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Asegurando import settings del OBJ", 0.1f);
                EnsureObjImportSettings();

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Cargando OBJ importado", 0.2f);
                var objAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ObjPath);
                if (objAsset == null)
                {
                    EditorUtility.DisplayDialog("Pasteurizador HTST",
                        "Unity todavia no termino de importar el OBJ. Esperate a que termine y volve a correr.", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Generando 15 materiales URP", 0.3f);
                var palette = BuildAndSaveMaterials();

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Creando ScriptableObject database", 0.4f);
                var db = CreateOrUpdateDatabase();

                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Instanciando OBJ y configurando", 0.55f);
                Directory.CreateDirectory(PrefabDir);

                // Instancia el OBJ en escena temporal
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(objAsset);
                instance.name = "Pasteurizador_HTST";

                // Asigna materiales por nombre
                var matMap = new Dictionary<PasteurizerMaterialAssigner.PaletteRule, Material>();
                foreach (var e in palette) matMap[e.rule] = e.material;
                foreach (var r in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var rule = PasteurizerMaterialAssigner.ClassifyName(r.gameObject.name);
                    if (matMap.TryGetValue(rule, out var m)) r.sharedMaterial = m;
                }

                // Componentes
                var registry = instance.AddComponent<PasteurizerPartsRegistry>();
                registry.SetDatabase(db);
                registry.addCollidersOnAwake = true;
                registry.convexColliders = true;

                var assigner = instance.AddComponent<PasteurizerMaterialAssigner>();
                assigner.palette = palette;

                var hover = instance.AddComponent<PasteurizerHoverHandler>();
                hover.registry = registry;
                hover.alsoUseMouse = true;

                var exploded = instance.AddComponent<PasteurizerExplodedView>();
                exploded.registry = registry;

                var controller = instance.AddComponent<UnityPasteurizerController>();

                // Guardar como prefab
                EditorUtility.DisplayProgressBar("Pasteurizador HTST", "Guardando prefab", 0.85f);
                var saved = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                Object.DestroyImmediate(instance);
                Selection.activeObject = saved;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> Prefab generado: {PrefabPath}");
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
            foreach (var root in scene.GetRootGameObjects())
            {
                target = FindInChildren(root.transform, "Pasteurizer");
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
            // Mantenemos scale 1 porque el OBJ ya viene con escala 0.001 desde import settings
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

        private static void EnsureObjImportSettings()
        {
            var imp = AssetImporter.GetAtPath(ObjPath) as ModelImporter;
            if (imp == null) return;
            bool changed = false;
            if (Mathf.Abs(imp.globalScale - 0.001f) > 0.00001f) { imp.globalScale = 0.001f; changed = true; }
            if (imp.useFileScale) { imp.useFileScale = false; changed = true; }
            if (!imp.isReadable) { imp.isReadable = true; changed = true; }
            if (!imp.generateSecondaryUV) { imp.generateSecondaryUV = true; changed = true; }
            if (imp.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            { imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; changed = true; }
            if (imp.materialLocation != ModelImporterMaterialLocation.External)
            { imp.materialLocation = ModelImporterMaterialLocation.External; changed = true; }
            if (changed)
            {
                imp.SaveAndReimport();
                AssetDatabase.Refresh();
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
