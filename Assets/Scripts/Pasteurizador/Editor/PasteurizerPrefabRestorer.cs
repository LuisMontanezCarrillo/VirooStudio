#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Crea placeholders visuales para los prefabs que estaban
    /// "Missing" en escena (sus FBX/GLB originales se movieron a backup
    /// porque colgaban Unity 6 al importarse).
    ///
    /// Estrategia:
    ///   1. Genera 4 prefabs nuevos en Assets/Prefabs/Pasteurizador_HTST/Placeholders/
    ///      con primitivos (Cube/Cylinder/Quad) coloreados en amarillo brillante
    ///      y un label de TextMeshPro "MISSING: <nombre>" para que el usuario
    ///      sepa que es provisional.
    ///   2. Edita el .meta de cada prefab para asignarle el GUID exacto que
    ///      la escena referencia. Asi las "Missing Prefab" desaparecen
    ///      y los GameObjects vuelven a aparecer en la jerarquia.
    ///
    /// Si despues conseguis re-exportar los FBX en Maya (sin errores
    /// "Error reading command from socket"), reemplazas el archivo
    /// manteniendo el .meta y todo sigue funcionando.
    public static class PasteurizerPrefabRestorer
    {
        private const string PlaceholderDir = "Assets/Prefabs/Pasteurizador_HTST/Placeholders";

        private struct Spec
        {
            public string filename;       // archivo destino (.prefab)
            public string guid;           // GUID que la escena referencia
            public string displayName;    // label para el TextMesh
            public PrimitiveType shape;
            public Vector3 scale;         // tamaño en metros
            public Color color;
        }

        private static readonly Spec[] Placeholders =
        {
            new Spec
            {
                filename = "Placeholder_WalkInFreezer.prefab",
                guid = "ab23328313fdfd24aa339788ac00e43b",
                displayName = "WALK-IN FREEZER (placeholder)",
                shape = PrimitiveType.Cube,
                scale = new Vector3(3.0f, 2.5f, 2.0f),
                color = new Color(0.85f, 0.85f, 0.95f, 1f),
            },
            new Spec
            {
                filename = "Placeholder_WalkInDoor.prefab",
                guid = "ccdf47cc786e7b74c9e72a1872f930a9",
                displayName = "WALK-IN DOOR (placeholder)",
                shape = PrimitiveType.Cube,
                scale = new Vector3(1.0f, 2.5f, 0.15f),
                color = new Color(0.70f, 0.78f, 0.85f, 1f),
            },
            new Spec
            {
                filename = "Placeholder_TapaRegistro.prefab",
                guid = "c3c8a62f05d353c48a84d574cc0e61b2",
                displayName = "TAPA REG. (placeholder)",
                shape = PrimitiveType.Cylinder,
                scale = new Vector3(0.30f, 0.025f, 0.30f),
                color = new Color(0.30f, 0.30f, 0.35f, 1f),
            },
            new Spec
            {
                filename = "Placeholder_LogoSimVR.prefab",
                guid = "f599cdf4f1433fa4a8c2135edd24a7b3",
                displayName = "LOGO SIM-VR (placeholder)",
                shape = PrimitiveType.Quad,
                scale = new Vector3(1.0f, 0.4f, 1f),
                color = new Color(0.15f, 0.50f, 0.85f, 1f),
            },
        };

        [MenuItem("Viroo/Pasteurizador HTST/12. Restaurar Missing Prefabs con placeholders", priority = 120)]
        public static void RestoreMissingPrefabs()
        {
            Directory.CreateDirectory(PlaceholderDir);

            int created = 0;
            int guidPatched = 0;

            foreach (var spec in Placeholders)
            {
                string prefabPath = PlaceholderDir + "/" + spec.filename;
                string metaPath = prefabPath + ".meta";

                // Si ya existe con el GUID correcto, skip
                if (File.Exists(metaPath))
                {
                    string existing = File.ReadAllText(metaPath);
                    if (existing.Contains("guid: " + spec.guid))
                    {
                        Debug.Log($"<color=cyan>[Placeholder]</color> {spec.filename} ya existe con GUID correcto.");
                        continue;
                    }
                }

                // 1) Crear el GameObject template
                var go = GameObject.CreatePrimitive(spec.shape);
                go.name = Path.GetFileNameWithoutExtension(spec.filename);
                go.transform.localScale = spec.scale;

                // Material URP/Lit amarillo brillante para que se distinga
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", spec.color);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", spec.color);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", spec.color * 0.3f);
                }
                go.GetComponent<Renderer>().sharedMaterial = mat;

                // Asegurar collider (los walk-in necesitan colisión)
                if (go.GetComponent<Collider>() == null)
                    go.AddComponent<BoxCollider>();

                // Label flotante con TextMeshPro
                var labelGO = new GameObject("MissingLabel", typeof(RectTransform), typeof(Canvas));
                labelGO.transform.SetParent(go.transform, false);
                var canvas = labelGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var labelRT = (RectTransform)labelGO.transform;
                labelRT.sizeDelta = new Vector2(2f, 0.5f);
                labelRT.localPosition = new Vector3(0f, spec.scale.y * 0.6f, 0f);
                labelRT.localScale = Vector3.one * 0.005f;  // ~5mm/pixel

                var textGO = new GameObject("Text", typeof(RectTransform));
                textGO.transform.SetParent(labelGO.transform, false);
                var textRT = (RectTransform)textGO.transform;
                textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = spec.displayName;
                tmp.fontSize = 40f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 0.9f, 0.2f, 1f);
                tmp.fontStyle = FontStyles.Bold;
                tmp.raycastTarget = false;

                // 2) Guardar como prefab
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                created++;

                // 3) Refresh para que Unity escriba el .meta
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

                // 4) Patch el .meta con el GUID correcto
                if (File.Exists(metaPath))
                {
                    string metaText = File.ReadAllText(metaPath);
                    string patched = Regex.Replace(metaText, @"guid:\s*[a-f0-9]+", "guid: " + spec.guid);
                    if (patched != metaText)
                    {
                        File.WriteAllText(metaPath, patched);
                        guidPatched++;
                        Debug.Log($"<color=cyan>[Placeholder]</color> {spec.filename} → GUID {spec.guid}");
                    }
                }
            }

            // Re-import con los GUIDs nuevos
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                $"Placeholders generados.\n\n" +
                $"• {created} prefabs creados\n" +
                $"• {guidPatched} GUIDs ajustados\n\n" +
                "Ubicación: " + PlaceholderDir + "\n\n" +
                "Re-abrí la escena (File → Open Scene) para que Unity\n" +
                "vuelva a resolver las referencias 'Missing Prefab'.\n" +
                "Los 4 errores rojos de Missing Prefab deberían desaparecer.\n\n" +
                "Si todavía ves Missing tras reabrir, corré:\n" +
                "Assets → Reimport All  (tarda 5-10 min)",
                "OK");

            Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> Placeholders: {created} creados, {guidPatched} GUIDs patcheados.");
        }

        [MenuItem("Viroo/Pasteurizador HTST/Quitar placeholders", priority = 121)]
        public static void RemovePlaceholders()
        {
            if (!Directory.Exists(PlaceholderDir))
            {
                Debug.Log($"<color=cyan>[Placeholder]</color> {PlaceholderDir} no existe.");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("Pasteurizador HTST",
                "Esto borra " + PlaceholderDir + " (y sus 4 prefabs).\n\n" +
                "Las referencias en escena volverán a estar 'Missing Prefab'.\n" +
                "¿Continuar?",
                "Sí, borrar", "Cancelar");
            if (!confirm) return;

            AssetDatabase.DeleteAsset(PlaceholderDir);
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[Placeholder]</color> Carpeta {PlaceholderDir} eliminada.");
        }
    }
}
#endif
