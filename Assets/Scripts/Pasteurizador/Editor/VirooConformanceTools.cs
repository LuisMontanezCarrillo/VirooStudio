#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Virtualware.DependencyInjection;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Herramientas de adecuacion a VIROO 3.0 que NO reestructuran el desarrollo:
    /// solo agregan o verifican lo que la plataforma exige sobre lo que ya existe.
    ///
    /// DISEÑO DEL PROYECTO:
    ///  - Una unica escena con fade entre los tres momentos (no hay LoadSceneAction).
    ///  - Cada estudiante vive la experiencia de forma INDIVIDUAL: el clic de uno no
    ///    debe disparar nada en los otros tres visores.
    ///
    /// Consecuencia tecnica de lo segundo: los objetos interactuables NO llevan
    /// NetworkObject ni VirooXRSimpleInteractable. Un XRSimpleInteractable puro de XRI
    /// ya es detectado por los interactores del rig de VIROO (NearFarInteractor en VR y
    /// XRMouseInteractor en escritorio, ambos en interaction layer Default) y su
    /// UnityEvent se ejecuta solo en el cliente local. Añadir los componentes de red
    /// haria justo lo contrario: propagaria el clic a los demas.
    public static class VirooConformanceTools
    {
        private const string Menu = "Viroo/Adecuacion VIROO/";

        /// Interaction layer "Default" (bit 0). El bit 31 esta reservado al teleport.
        private const int DefaultInteractionLayer = 1;

        // ------------------------------------------------------------------
        // 1. Convertir objetos existentes en interactuables individuales
        // ------------------------------------------------------------------

        [MenuItem(Menu + "1. Hacer interactuables (individual, sin red)", priority = 1)]
        public static void MakeSelectionInteractable()
        {
            var targets = Selection.gameObjects;
            if (targets == null || targets.Length == 0)
            {
                EditorUtility.DisplayDialog("Adecuacion VIROO",
                    "Selecciona en la jerarquia uno o mas GameObjects de la escena.", "OK");
                return;
            }

            int converted = 0, alreadyOk = 0, noCollider = 0;
            var report = new List<string>();
            int defaultLayer = Mathf.Max(0, LayerMask.NameToLayer("Default"));

            foreach (var go in targets)
            {
                if (!go.scene.IsValid())
                {
                    report.Add($"  - {go.name}: es un asset de proyecto, no un objeto de escena. Omitido.");
                    continue;
                }

                Undo.RegisterFullObjectHierarchyUndo(go, "Hacer interactuable VIROO");

                // Los interactuables deben estar en capa fisica Default (regla de VIROO 3.0).
                if (go.layer != defaultLayer)
                {
                    go.layer = defaultLayer;
                    report.Add($"  - {go.name}: layer corregido a Default.");
                }

                // Hace falta un collider no-trigger: XRBaseInteractable descarta los triggers.
                var colliders = go.GetComponentsInChildren<Collider>(true);
                if (colliders.Length == 0)
                {
                    noCollider++;
                    report.Add($"  - {go.name}: SIN COLLIDER. El rayo no lo detectara.");
                }
                else if (colliders.All(c => c.isTrigger))
                {
                    noCollider++;
                    report.Add($"  - {go.name}: todos sus colliders son trigger; hace falta uno no-trigger.");
                }

                var interactable = go.GetComponent<XRSimpleInteractable>();
                if (interactable == null)
                {
                    interactable = Undo.AddComponent<XRSimpleInteractable>(go);
                    converted++;
                }
                else
                {
                    alreadyOk++;
                }

                if (interactable.interactionLayers != DefaultInteractionLayer)
                {
                    Undo.RecordObject(interactable, "Interaction layer Default");
                    interactable.interactionLayers = DefaultInteractionLayer;
                    EditorUtility.SetDirty(interactable);
                    report.Add($"  - {go.name}: interaction layer puesto en Default.");
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var detail = report.Count > 0 ? "\n" + string.Join("\n", report) : string.Empty;
            Debug.Log($"<color=cyan>[Adecuacion VIROO]</color> {converted} interactuables nuevos, " +
                      $"{alreadyOk} ya lo eran, {noCollider} sin collider valido.{detail}");

            EditorUtility.DisplayDialog("Adecuacion VIROO",
                $"{converted} objeto(s) convertidos en interactuables individuales.\n" +
                $"{alreadyOk} ya lo eran." +
                (noCollider > 0 ? $"\n\nATENCION: {noCollider} sin collider valido (revisa la consola)." : "") +
                "\n\nSiguiente paso: en el XRSimpleInteractable, en 'Select Entered', arrastra el " +
                "metodo que ya tenias (por ejemplo ValidadorSecuencia.ValidarClic).\n\n" +
                "No se agregan componentes de red: el clic queda local para cada estudiante.",
                "OK");
        }

        // ------------------------------------------------------------------
        // 2. Canvases World Space listos para el laser VR
        // ------------------------------------------------------------------

        [MenuItem(Menu + "2. Preparar Canvases World Space para VR", priority = 2)]
        public static void PrepareWorldCanvases()
        {
            var canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int fixedCanvases = 0, notWorldSpace = 0, alreadyOk = 0;
            var report = new List<string>();

            foreach (var canvas in canvases)
            {
                if (!canvas.gameObject.scene.IsValid()) continue;

                // Los canvas anidados heredan el render mode del padre: solo interesa el raiz.
                var parent = canvas.transform.parent;
                if (parent != null && parent.GetComponentInParent<Canvas>() != null) continue;

                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    notWorldSpace++;
                    report.Add($"  - {canvas.name}: en {canvas.renderMode}. En VR debe ser World Space.");
                    continue;
                }

                bool changed = false;
                Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Preparar canvas VR");

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
                    changed = true;
                }
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject);
                    changed = true;
                }

                if (changed) { fixedCanvases++; report.Add($"  - {canvas.name}: raycasters agregados."); }
                else alreadyOk++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var detail = report.Count > 0 ? "\n" + string.Join("\n", report) : string.Empty;
            Debug.Log($"<color=cyan>[Adecuacion VIROO]</color> {fixedCanvases} canvases preparados, " +
                      $"{alreadyOk} ya estaban bien, {notWorldSpace} no estan en World Space.{detail}");

            EditorUtility.DisplayDialog("Adecuacion VIROO",
                $"{fixedCanvases} canvas preparados, {alreadyOk} ya estaban correctos." +
                (notWorldSpace > 0
                    ? $"\n\n{notWorldSpace} canvas NO estan en World Space y no funcionaran en el visor " +
                      "(revisa la consola)."
                    : ""),
                "OK");
        }

        // ------------------------------------------------------------------
        // 3. Diagnostico de conformidad
        // ------------------------------------------------------------------

        [MenuItem(Menu + "3. Diagnosticar conformidad VIROO (consola)", priority = 3)]
        public static void Diagnose()
        {
            var scene = SceneManager.GetActiveScene();
            var lines = new List<string>();
            int errors = 0;

            void Check(bool ok, string okMsg, string failMsg)
            {
                if (ok) lines.Add($"  OK    {okMsg}");
                else { lines.Add($"  FALLA {failMsg}"); errors++; }
            }

            // Regla 1: un unico root de contenido llamado "Root"
            var roots = scene.GetRootGameObjects();
            var contentRoots = roots.Where(r => !IsVirooInfrastructure(r.name)).ToArray();
            Check(contentRoots.Length == 1 && contentRoots[0].name == "Root",
                "Un unico GameObject raiz de contenido llamado 'Root'.",
                $"Hay {contentRoots.Length} objetos de contenido en la raiz " +
                $"({string.Join(", ", contentRoots.Take(5).Select(r => r.name))}). Debe haber uno solo.");

            // Regla 2: componentes de inyeccion de dependencias en el Root
            var root = roots.FirstOrDefault(r => r.name == "Root");
            bool hasDi = root != null
                         && root.GetComponent<DependencyInjectionContext>() != null
                         && root.GetComponent<DependencyInjectionContextAutoWire>() != null;
            Check(hasDi,
                "Root con DependencyInjectionContext + DependencyInjectionContextAutoWire.",
                "Al Root le faltan los componentes de inyeccion de dependencias. " +
                "Corre 'Viroo/Pasteurizador HTST/8. Adaptar a Viroo'.");

            // Regla 3: sin camaras activas (componente habilitado Y GameObject activo)
            var activeCams = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.gameObject.scene == scene && c.targetTexture == null
                            && c.enabled && c.gameObject.activeInHierarchy)
                .ToArray();
            Check(activeCams.Length == 0,
                "Sin camaras activas (VIROO crea el rig en runtime).",
                $"Hay {activeCams.Length} camara(s) activa(s): " +
                $"{string.Join(", ", activeCams.Take(5).Select(c => c.name))}.");

            // Regla 4: sin EventSystem
            var eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => e.gameObject.scene == scene).ToArray();
            Check(eventSystems.Length == 0,
                "Sin EventSystem (lo aporta la plataforma).",
                $"Hay {eventSystems.Length} EventSystem en la escena.");

            // Regla 5: un unico PlayerStart
            int playerStarts = CountByTypeName("PlayerStart", "InternalPlayerStart", scene);
            Check(playerStarts == 1,
                "Exactamente un PlayerStart.",
                $"Hay {playerStarts} PlayerStart (debe haber exactamente uno).");

            // Regla 6: interactuables usables (collider no-trigger + interaction layer Default)
            var interactables = Object.FindObjectsByType<XRSimpleInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(i => i.gameObject.scene == scene).ToArray();

            var sinCollider = interactables
                .Where(i => i.GetComponentsInChildren<Collider>(true).All(c => c.isTrigger))
                .ToArray();
            Check(sinCollider.Length == 0,
                $"Los {interactables.Length} interactuables tienen collider no-trigger.",
                $"{sinCollider.Length} interactuables sin collider valido: " +
                $"{string.Join(", ", sinCollider.Take(5).Select(i => i.name))}.");

            var malaCapa = interactables
                .Where(i => (i.interactionLayers & DefaultInteractionLayer) == 0).ToArray();
            Check(malaCapa.Length == 0,
                "Todos los interactuables estan en interaction layer Default.",
                $"{malaCapa.Length} interactuables fuera de la capa Default (el rayo no los vera): " +
                $"{string.Join(", ", malaCapa.Take(5).Select(i => i.name))}.");

            // Regla 7: interactuables sin nada cableado (fallo silencioso tipico)
            var sinEventos = interactables
                .Where(i => i.selectEntered.GetPersistentEventCount() == 0
                            && i.activated.GetPersistentEventCount() == 0)
                .ToArray();
            if (sinEventos.Length > 0)
                lines.Add($"  AVISO {sinEventos.Length} interactuables sin nada en 'Select Entered'/'Activated': " +
                          $"{string.Join(", ", sinEventos.Take(5).Select(i => i.name))}. " +
                          "Se podran apuntar pero no haran nada.");

            var header = errors == 0
                ? "<color=lime>[Adecuacion VIROO] Escena conforme.</color>"
                : $"<color=orange>[Adecuacion VIROO] {errors} problema(s) que bloquean el build.</color>";

            Debug.Log($"{header}\nEscena: {scene.name}\n" + string.Join("\n", lines) +
                      $"\n  INFO  {interactables.Length} interactuables individuales (sin red, correcto para " +
                      "que cada estudiante viva su propia experiencia)." +
                      "\n  INFO  Falta definir el Application Identifier en " +
                      "Window > Viroo > Dashboard > Application Builder.");
        }

        // ------------------------------------------------------------------

        private static bool IsVirooInfrastructure(string name)
        {
            return name.StartsWith("Viroo") || name.Contains("GazeSnapVolume") ||
                   name.Contains("Gaze Snap Volume");
        }

        /// Cuenta componentes por nombre de tipo para no acoplarse al namespace del paquete,
        /// que cambia entre versiones de VIROO.
        private static int CountByTypeName(string nameA, string nameB, Scene scene)
        {
            int count = 0;
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mb in behaviours)
            {
                if (mb == null || mb.gameObject.scene != scene) continue;
                var n = mb.GetType().Name;
                if (n == nameA || n == nameB) count++;
            }
            return count;
        }
    }
}
#endif
