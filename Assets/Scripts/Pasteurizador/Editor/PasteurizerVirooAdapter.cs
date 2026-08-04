#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Virtualware.DependencyInjection;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Adapta la escena activa a las reglas de Viroo Studio Project Validation:
    ///   1) Todos los GameObjects deben colgar de un único "Root" en raíz de escena.
    ///   2) El Root debe llevar DependencyInjectionContext + DependencyInjectionContextAutoWire.
    ///   3) Ningún GameObject puede tener Layer "missing" (-1) — todo a "Default" mín.
    ///   4) No puede haber EventSystem en la escena (Viroo aporta el suyo).
    ///   5) No puede haber cámaras activas sin RenderTexture (Viroo trae su rig).
    ///   6) Sólo puede haber un PlayerStart en la escena.
    ///
    /// Esto NO toca los Mocks de Viroo (VirooContextMock, VirooInteractionsMock, etc.)
    /// porque esos son OK que estén en raíz — sólo mueve lo nuestro.
    public static class PasteurizerVirooAdapter
    {
        private const string PrefabInstanceName = "Pasteurizador_HTST";
        private const string PrefabInstanceLegacyName = "Pasteurizer";
        private const string RootName = "Root";

        // Nombres conocidos de Mocks de Viroo que SI deben quedar en raíz
        private static readonly string[] AllowedInRoot =
        {
            "Root",
            "VirooContextMock",
            "VirooInteractionsMock",
            "VirooInteractionsHandsMock",
            "VirooSceneLoaderSceneContextMock",
            "GazeSnapVolume",
            "Gaze Snap Volume",
        };

        [MenuItem("Viroo/Pasteurizador HTST/8. Adaptar a Viroo (mover dentro de Root + layers)", priority = 109)]
        public static void AdaptToViroo()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    "No hay escena activa abierta.", "OK");
                return;
            }

            int moved = 0;
            int relayered = 0;
            int killedEventSystems = 0;
            int killedCameras = 0;

            // 1) Buscar o crear "Root"
            GameObject root = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == RootName) { root = go; break; }
            }
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Crear Root");
            }

            // 1b) El validador de Viroo exige los dos componentes de inyección de dependencias
            //     en el Root (HasNotDependencyInjectionScriptsInRootGameObject).
            int addedDi = 0;
            if (root.GetComponent<DependencyInjectionContext>() == null)
            {
                Undo.AddComponent<DependencyInjectionContext>(root);
                addedDi++;
            }
            if (root.GetComponent<DependencyInjectionContextAutoWire>() == null)
            {
                Undo.AddComponent<DependencyInjectionContextAutoWire>(root);
                addedDi++;
            }

            // 2) Mover a Root todo lo que esté en raíz y no esté en AllowedInRoot
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go == root) continue;
                bool allowed = false;
                foreach (var n in AllowedInRoot)
                    if (go.name == n) { allowed = true; break; }
                if (allowed) continue;

                Undo.SetTransformParent(go.transform, root.transform,
                    $"Mover {go.name} a Root");
                moved++;
            }

            // 3) Asignar Layer Default a todo lo que tenga Layer inválido (-1) o sin layer real
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer < 0) defaultLayer = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.layer < 0 || t.gameObject.layer > 31)
                {
                    Undo.RecordObject(t.gameObject, "Set Layer Default");
                    t.gameObject.layer = defaultLayer;
                    relayered++;
                }
            }

            // 4) Eliminar EventSystems en la escena (Viroo aporta el suyo)
            var eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var es in eventSystems)
            {
                Undo.DestroyObjectImmediate(es.gameObject);
                killedEventSystems++;
            }

            // 5) Desactivar cámaras activas sin RenderTexture (las del FBX usualmente)
            //    Excepto las que están bajo los Mocks de Viroo
            var cams = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var cam in cams)
            {
                if (cam.targetTexture != null) continue;  // OK: cámaras con RenderTexture
                if (IsUnderAllowedRoot(cam.transform)) continue;
                Undo.RecordObject(cam, "Disable rogue camera");
                cam.enabled = false;
                Undo.RecordObject(cam.gameObject, "Disable rogue camera GO");
                cam.gameObject.SetActive(false);
                killedCameras++;
            }

            // 6) Avisar si hay más de un PlayerStart (el validador exige uno solo)
            int playerStarts = CountPlayerStarts();
            string playerStartWarning = playerStarts > 1
                ? $"\n\nATENCION: hay {playerStarts} PlayerStart en la escena. " +
                  "Viroo exige exactamente uno; borra los sobrantes a mano."
                : string.Empty;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> Viroo Adapter:\n" +
                      $"  - {moved} GameObjects movidos a '{RootName}'\n" +
                      $"  - {addedDi} componentes de inyeccion de dependencias agregados al Root\n" +
                      $"  - {relayered} GameObjects con Layer corregido a Default\n" +
                      $"  - {killedEventSystems} EventSystems eliminados\n" +
                      $"  - {killedCameras} camaras rogue desactivadas\n" +
                      $"  - {playerStarts} PlayerStart encontrados");

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                $"Adaptación a Viroo completa:\n\n" +
                $"• {moved} GO movidos dentro de 'Root'\n" +
                $"• {addedDi} componentes DI agregados al Root\n" +
                $"• {relayered} GO con layer corregido\n" +
                $"• {killedEventSystems} EventSystems eliminados\n" +
                $"• {killedCameras} cámaras rogue desactivadas" +
                playerStartWarning + "\n\n" +
                "Volvé a abrir Viroo Studio → Project Validation y dale 'Always refresh'.",
                "OK");
        }

        /// Cuenta los PlayerStart sin acoplarse al namespace del paquete de Viroo:
        /// el tipo concreto cambia entre versiones (PlayerStart / InternalPlayerStart).
        private static int CountPlayerStarts()
        {
            int count = 0;
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                var typeName = mb.GetType().Name;
                if (typeName == "PlayerStart" || typeName == "InternalPlayerStart") count++;
            }
            return count;
        }

        private static bool IsUnderAllowedRoot(Transform t)
        {
            while (t != null)
            {
                foreach (var n in AllowedInRoot)
                    if (t.name == n) return true;
                t = t.parent;
            }
            return false;
        }
    }
}
#endif
