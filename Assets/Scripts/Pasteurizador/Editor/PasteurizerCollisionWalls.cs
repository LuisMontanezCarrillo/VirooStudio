#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Crea un GameObject "_CollisionWalls" hijo del prefab Pasteurizador_HTST
    /// con 4 BoxColliders perimetrales + 1 BoxCollider piso. Son invisibles
    /// (sin Renderer), no son trigger, asi el CharacterController del XR Rig
    /// no puede atravesar el pasteurizador.
    ///
    /// El tamaño del bounding box se calcula desde los Renderers del prefab,
    /// con un margen configurable. El piso queda 1 cm bajo y=0 para evitar
    /// z-fighting con el suelo de la escena.
    ///
    /// Esto es independiente de los MeshColliders de cada parte (que tienen
    /// los 654 objetos para hover/click) — esos colliders son convexos y
    /// chiquitos, asi que muchos motores de movimiento (teleport, snap turn)
    /// los ignoran. Los muros perimetrales son una caja simple y barata
    /// que SIEMPRE bloquea al player.
    public static class PasteurizerCollisionWalls
    {
        private const string PrefabPath =
            "Assets/Prefabs/Pasteurizador_HTST/Pasteurizador_HTST.prefab";
        private const string WallsName = "_CollisionWalls";

        // Margen horizontal alrededor del pasteurizador (m)
        private const float MarginXZ = 0.30f;
        // Altura de los muros (m). Generosa para que ni siquiera saltando lo cruces.
        private const float WallHeight = 3.5f;
        // Espesor de cada muro (m)
        private const float WallThickness = 0.20f;

        [MenuItem("Viroo/Pasteurizador HTST/7. Crear muros de colisión", priority = 107)]
        public static void CreateCollisionWalls()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No encontre el prefab en {PrefabPath}.\nCorre primero el paso 1.", "OK");
                return;
            }

            try
            {
                // 1) Borrar el contenedor previo si existe (idempotencia)
                var existing = prefabRoot.transform.Find(WallsName);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                // 2) Calcular bounding box global del prefab (en coords mundo
                //    pero como el prefab esta en origen, equivale a coords locales)
                var bbox = ComputeBoundsWorld(prefabRoot);
                if (bbox.size == Vector3.zero)
                {
                    EditorUtility.DisplayDialog("Pasteurizador HTST",
                        "El prefab no tiene Renderers — no puedo calcular bbox.\n" +
                        "Corre primero los pasos 1, 5 y 6.", "OK");
                    return;
                }

                // 3) Expandir XZ y dejar piso en y=0
                Vector3 min = bbox.min;
                Vector3 max = bbox.max;
                min.x -= MarginXZ; min.z -= MarginXZ;
                max.x += MarginXZ; max.z += MarginXZ;

                float yBase = 0f;                   // piso de la escena
                float yTop  = yBase + WallHeight;
                Vector3 center = new Vector3((min.x + max.x) * 0.5f, (yBase + yTop) * 0.5f, (min.z + max.z) * 0.5f);
                Vector3 size   = new Vector3(max.x - min.x, yTop - yBase, max.z - min.z);

                // 4) Contenedor
                var walls = new GameObject(WallsName);
                walls.transform.SetParent(prefabRoot.transform, false);
                walls.transform.localPosition = Vector3.zero;
                walls.transform.localRotation = Quaternion.identity;
                walls.transform.localScale = Vector3.one;

                int added = 0;

                // Muros perimetrales (4)
                added += AddWall(walls.transform, "Wall_North",
                    new Vector3(center.x, center.y, max.z),
                    new Vector3(size.x + WallThickness * 2f, size.y, WallThickness));
                added += AddWall(walls.transform, "Wall_South",
                    new Vector3(center.x, center.y, min.z),
                    new Vector3(size.x + WallThickness * 2f, size.y, WallThickness));
                added += AddWall(walls.transform, "Wall_East",
                    new Vector3(max.x, center.y, center.z),
                    new Vector3(WallThickness, size.y, size.z));
                added += AddWall(walls.transform, "Wall_West",
                    new Vector3(min.x, center.y, center.z),
                    new Vector3(WallThickness, size.y, size.z));

                // NOTA: NO creamos Floor_Interior — empuja al CharacterController
                // del XR Rig hacia abajo y hunde al avatar bajo el piso de la escena.
                // El suelo real lo aporta la escena (Plane "Piso" / Terrain).

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> {added} BoxColliders de pared creados en " +
                          $"'{WallsName}'. BBox: min={min}, max={max}, size={size}");
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"Listo. Creé {added} muros de colisión invisibles.\n\n" +
                    $"Tamaño: {size.x:F1} x {size.y:F1} x {size.z:F1} m\n\n" +
                    "Si en Play seguís atravesando, es porque tu XR Rig usa teleport puro " +
                    "o no tiene CharacterController. Mirá la consola para los siguientes pasos.",
                    "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem("Viroo/Pasteurizador HTST/Quitar muros de colisión", priority = 108)]
        public static void RemoveCollisionWalls()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null) return;
            try
            {
                var existing = prefabRoot.transform.Find(WallsName);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                    Debug.Log("<color=cyan>[Pasteurizador HTST]</color> Muros de colisión removidos.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static int AddWall(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var bc = go.AddComponent<BoxCollider>();
            bc.center = Vector3.zero;
            bc.size = size;
            bc.isTrigger = false;     // bloqueo solido
            return 1;
        }

        private static Bounds ComputeBoundsWorld(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds();
            // Ignorar el contenedor de paredes si quedo de una corrida previa
            var b = new Bounds();
            bool inited = false;
            foreach (var r in renderers)
            {
                if (r.transform.IsChildOf(go.transform) == false) continue;
                var t = r.transform;
                bool skip = false;
                while (t != null && t != go.transform)
                {
                    if (t.name == WallsName) { skip = true; break; }
                    t = t.parent;
                }
                if (skip) continue;
                if (!inited) { b = r.bounds; inited = true; }
                else b.Encapsulate(r.bounds);
            }
            return inited ? b : new Bounds();
        }
    }
}
#endif
