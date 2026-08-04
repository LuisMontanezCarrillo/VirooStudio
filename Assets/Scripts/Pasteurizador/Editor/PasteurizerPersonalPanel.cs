#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using ViroLab.Pasteurizador;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Convierte el dashboard del simulador, que estaba fijo en el televisor de la
    /// planta, en un PANEL PERSONAL por estudiante.
    ///
    /// Por que esto basta para que sea personal: el dashboard no lleva componentes de
    /// red, asi que cada cliente tiene su propia instancia del objeto de escena y su
    /// propio motor de simulacion. Al hacer que el panel siga a la camara local, en
    /// cada visor se coloca frente a su dueño y muestra sus propios valores. Nadie ve
    /// el panel de otro.
    ///
    /// Se usa LazyFollow y no un seguimiento continuo: el panel tiene botones y
    /// sliders, y apuntar a un blanco que se mueve es incomodo. Con LazyFollow se
    /// queda quieto mientras se opera y solo se recoloca si el estudiante se gira o
    /// se aleja.
    public static class PasteurizerPersonalPanel
    {
        private const string Menu = "Viroo/Adecuacion VIROO/";
        private const string DashboardName = "_SimDashboard";
        private const string TvName = "Simulador";
        private const string RootName = "Root";

        // Lo que determina si el texto se lee NO es el tamaño del panel, sino su tamaño
        // ANGULAR, es decir la relacion escala/distancia. Acercar el panel y agrandarlo
        // a la vez es lo que hace crecer las letras; alejarlo y agrandarlo en la misma
        // proporcion se ve exactamente igual.
        //
        // Preset por defecto "Muy grande" (elegido en pruebas con visor): 1600x900 px
        // -> 2.72 x 1.53 m a 1.15 m, unos 99 grados de ancho. Es el tamaño angular mas
        // parecido al del tablero original de pared (3.07 x 1.73 m) y el unico con el
        // que las lecturas del SCADA resultan comodas de leer.
        private const float PanelDistance = 1.15f;
        private const float PanelVerticalOffset = -0.12f;
        private const float PanelWorldScale = 0.0017f;

        [MenuItem(Menu + "4. Convertir Dashboard en panel personal", priority = 4)]
        public static void MakePersonalPanel()
        {
            var dashboard = FindDashboard(out string where);
            if (dashboard == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    $"No se encontro ningun '{DashboardName}' en la escena.\n\n" +
                    "Corre primero 'Viroo/Pasteurizador HTST/13. Crear Dashboard Simulador en el TV'.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(dashboard, "Panel personal");

            // 1) Sacarlo del televisor, pero manteniendolo dentro de 'Root'
            //    (VIROO exige un unico raiz de contenido llamado 'Root').
            var root = FindRoot();
            if (root == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    "No existe un GameObject 'Root' en la escena. " +
                    "Corre antes 'Viroo/Pasteurizador HTST/8. Adaptar a Viroo'.", "OK");
                return;
            }

            bool reparented = false;
            if (dashboard.transform.parent != root.transform)
            {
                Undo.SetTransformParent(dashboard.transform, root.transform,
                    "Sacar dashboard del TV");
                reparented = true;
            }

            // 2) Canvas en World Space y alcanzable por el laser
            var canvas = dashboard.GetComponent<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    $"'{DashboardName}' no tiene componente Canvas.", "OK");
                return;
            }
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Undo.RecordObject(canvas, "Canvas a World Space");
                canvas.renderMode = RenderMode.WorldSpace;
            }
            if (dashboard.GetComponent<GraphicRaycaster>() == null)
                Undo.AddComponent<GraphicRaycaster>(dashboard);
            if (dashboard.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                Undo.AddComponent<TrackedDeviceGraphicRaycaster>(dashboard);

            // 3) Seguimiento perezoso frente al usuario local
            var follow = dashboard.GetComponent<PasteurizerWorldCanvas>();
            if (follow == null) follow = Undo.AddComponent<PasteurizerWorldCanvas>(dashboard);

            Undo.RecordObject(follow, "Configurar panel personal");
            follow.followMode = PasteurizerWorldCanvas.FollowMode.LazyFollow;
            follow.distance = PanelDistance;
            follow.verticalOffset = PanelVerticalOffset;
            follow.horizontalOffset = 0f;
            follow.worldScale = PanelWorldScale;
            Undo.RecordObject(dashboard.transform, "Configurar panel personal");
            dashboard.transform.localScale = Vector3.one * PanelWorldScale;
            // Margen de giro acorde al ancho del panel (ver AplicarTamano).
            float gradosAncho = 2f * Mathf.Atan((1600f * PanelWorldScale * 0.5f) / PanelDistance)
                                * Mathf.Rad2Deg;
            follow.angleDeadzone = Mathf.Clamp(gradosAncho * 0.5f + 6f, 22f, 80f);
            follow.distanceDeadzone = 0.6f;
            follow.recenterLerp = 3f;
            follow.showOnlyWhenPinned = false;   // el dashboard siempre visible
            EditorUtility.SetDirty(follow);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = dashboard;

            Debug.Log($"<color=cyan>[Panel personal]</color> '{DashboardName}' convertido en panel " +
                      $"personal (estaba en: {where}).\n" +
                      $"  - Reparentado a '{RootName}': {(reparented ? "si" : "ya estaba")}\n" +
                      $"  - Modo LazyFollow, {PanelDistance} m, escala {PanelWorldScale} " +
                      $"({1600 * PanelWorldScale:0.00} x {900 * PanelWorldScale:0.00} m)\n" +
                      "  - Cada estudiante vera su propio panel con sus propios valores.");

            EditorUtility.DisplayDialog("Panel personal",
                "Dashboard convertido en panel personal.\n\n" +
                "Cada estudiante lo vera frente a si, con los valores de su propia " +
                "simulacion. El panel se queda quieto mientras se opera y se recoloca " +
                "solo si el estudiante se gira o se aleja.\n\n" +
                "El televisor de la planta queda libre: podes dejarlo apagado o ponerle " +
                "una imagen fija de ambientacion.",
                "OK");
        }

        [MenuItem(Menu + "4b. Devolver Dashboard al televisor", priority = 5)]
        public static void RestoreToTv()
        {
            var dashboard = FindDashboard(out _);
            if (dashboard == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    $"No se encontro ningun '{DashboardName}' en la escena.", "OK");
                return;
            }

            var tv = GameObject.Find(TvName);
            if (tv == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    $"No se encontro el GameObject '{TvName}' en la escena.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(dashboard, "Devolver dashboard al TV");

            var follow = dashboard.GetComponent<PasteurizerWorldCanvas>();
            if (follow != null) Undo.DestroyObjectImmediate(follow);

            Undo.SetTransformParent(dashboard.transform, tv.transform, "Devolver dashboard al TV");
            dashboard.transform.localPosition = Vector3.zero;
            dashboard.transform.localRotation = Quaternion.identity;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"<color=cyan>[Panel personal]</color> '{DashboardName}' devuelto al televisor.");
        }

        // ------------------------------------------------------------------
        // Presets de tamaño. Se aplican en caliente para poder probarlos rapido.
        // ------------------------------------------------------------------

        [MenuItem(Menu + "Tamano del panel/1. Comodo", priority = 20)]
        public static void SizeComodo() => AplicarTamano(0.00115f, 1.25f, "Comodo");

        [MenuItem(Menu + "Tamano del panel/2. Grande", priority = 21)]
        public static void SizeGrande() => AplicarTamano(0.0014f, 1.20f, "Grande");

        [MenuItem(Menu + "Tamano del panel/3. Muy grande", priority = 22)]
        public static void SizeMuyGrande() => AplicarTamano(0.0017f, 1.15f, "Muy grande");

        private static void AplicarTamano(float worldScale, float distance, string nombre)
        {
            var dashboard = FindDashboard(out _);
            if (dashboard == null)
            {
                EditorUtility.DisplayDialog("Tamano del panel",
                    $"No se encontro ningun '{DashboardName}' en la escena.", "OK");
                return;
            }

            var follow = dashboard.GetComponent<PasteurizerWorldCanvas>();
            if (follow == null)
            {
                EditorUtility.DisplayDialog("Tamano del panel",
                    "El tablero todavia no es un panel personal. Corre antes el menu " +
                    "'4. Convertir Dashboard en panel personal'.", "OK");
                return;
            }

            Undo.RecordObject(follow, "Tamano del panel");
            follow.worldScale = worldScale;
            follow.distance = distance;
            EditorUtility.SetDirty(follow);

            // PasteurizerWorldCanvas solo aplica worldScale en su Awake, asi que hay
            // que tocar el transform para verlo al instante (tanto en editor como en Play).
            Undo.RecordObject(dashboard.transform, "Tamano del panel");
            dashboard.transform.localScale = Vector3.one * worldScale;

            if (Application.isPlaying) follow.Recenter();

            float anchoM = 1600f * worldScale;
            float altoM = 900f * worldScale;
            float grados = 2f * Mathf.Atan((anchoM * 0.5f) / distance) * Mathf.Rad2Deg;

            // El margen de tolerancia debe cubrir el propio ancho del panel: si no,
            // girar la cabeza para leer un extremo se interpreta como "el usuario se
            // giro" y el panel se recoloca mientras se esta leyendo. Se le da la mitad
            // del ancho mas un pequeño colchon.
            follow.angleDeadzone = Mathf.Clamp(grados * 0.5f + 6f, 22f, 80f);

            var escena = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            if (!Application.isPlaying) EditorSceneManager.SaveScene(escena);

            Debug.Log($"<color=cyan>[Tamano del panel]</color> {nombre}: " +
                      $"{anchoM:0.00} x {altoM:0.00} m a {distance:0.00} m " +
                      $"({grados:0} grados de ancho, margen de giro {follow.angleDeadzone:0} grados).");
        }

        /// El tablero vive en la escena desde el arranque, asi que sin compuerta
        /// aparece nada mas pulsar Play. Debe verse solo en el tercer momento, cuando
        /// termina el video explicativo.
        [MenuItem(Menu + "5. Mostrar el tablero solo tras el video (Escena 3)", priority = 7)]
        public static void GateDashboardBehindVideo()
        {
            var dashboard = FindDashboard(out _);
            if (dashboard == null)
            {
                EditorUtility.DisplayDialog("Compuerta del tablero",
                    $"No se encontro ningun '{DashboardName}' en la escena.", "OK");
                return;
            }

            var gestor = Object.FindFirstObjectByType<GestorEscena3>(FindObjectsInactive.Include);
            if (gestor == null)
            {
                EditorUtility.DisplayDialog("Compuerta del tablero",
                    "No se encontro ningun GestorEscena3 en la escena. Es el componente " +
                    "que sabe cuando termina el video explicativo.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(dashboard, "Compuerta del tablero");

            var gate = dashboard.GetComponent<PasteurizerSimGate>();
            if (gate == null) gate = Undo.AddComponent<PasteurizerSimGate>(dashboard);
            Undo.RecordObject(gate, "Configurar compuerta");
            gate.ocultarAlIniciar = true;
            EditorUtility.SetDirty(gate);

            // Evitar duplicados si el menu se ejecuta dos veces.
            int existentes = gestor.OnTutorialFinalizado.GetPersistentEventCount();
            for (int i = existentes - 1; i >= 0; i--)
            {
                if (gestor.OnTutorialFinalizado.GetPersistentTarget(i) is PasteurizerSimGate)
                    UnityEventTools.RemovePersistentListener(gestor.OnTutorialFinalizado, i);
            }

            Undo.RecordObject(gestor, "Cablear fin de video");
            UnityEventTools.AddVoidPersistentListener(
                gestor.OnTutorialFinalizado, gate.Mostrar);
            EditorUtility.SetDirty(gestor);

            var escena = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            // Guardar aqui mismo: si el cambio se queda sin guardar, al volver a abrir
            // la escena la compuerta no existe y el tablero reaparece desde el Play.
            bool guardada = EditorSceneManager.SaveScene(escena);
            Selection.activeGameObject = dashboard;

            Debug.Log("<color=cyan>[Compuerta del tablero]</color> El tablero arranca oculto y " +
                      $"se habilita desde GestorEscena3.OnTutorialFinalizado ('{gestor.name}'). " +
                      $"Escena guardada: {guardada}.");

            EditorUtility.DisplayDialog("Compuerta del tablero",
                "Listo.\n\n" +
                "El tablero del simulador ya no aparece al pulsar Play: queda oculto y " +
                "sin consumir recursos hasta que termina el video explicativo de la " +
                "Escena 3, momento en el que se enciende y se coloca frente al estudiante.\n\n" +
                (guardada
                    ? "La escena se guardo automaticamente."
                    : "ATENCION: no se pudo guardar la escena; guardala a mano con Ctrl+S.") +
                "\n\nPara probarlo sin esperar el video, usa el menu contextual del " +
                "componente PasteurizerSimGate y llama a Mostrar().",
                "OK");
        }

        /// El quad 'PantallaLed' del televisor usa un material blanco opaco y su
        /// VideoPlayer no lo pinta nunca (no tiene renderer de destino asignado), asi
        /// que se ve como un rectangulo blanco. Con el tablero convertido en panel
        /// personal, ese blanco queda a la vista en la pared.
        [MenuItem(Menu + "4c. Apagar/encender pantalla blanca del televisor", priority = 6)]
        public static void ToggleLedScreen()
        {
            GameObject led = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "PantallaLed") continue;
                    led = t.gameObject;
                    break;
                }
                if (led != null) break;
            }

            if (led == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    "No se encontro el objeto 'PantallaLed' en la escena.", "OK");
                return;
            }

            var renderer = led.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Panel personal",
                    "'PantallaLed' no tiene Renderer.", "OK");
                return;
            }

            Undo.RecordObject(renderer, "Apagar pantalla del televisor");
            renderer.enabled = !renderer.enabled;
            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            string estado = renderer.enabled ? "ENCENDIDA" : "APAGADA";
            Debug.Log($"<color=cyan>[Panel personal]</color> Pantalla del televisor: {estado}.");
            EditorUtility.DisplayDialog("Panel personal",
                $"Pantalla del televisor {estado}.\n\n" +
                "Se desactiva solo el Renderer, asi que el objeto y su VideoPlayer siguen " +
                "intactos y es reversible con este mismo menu.",
                "OK");
        }

        // ------------------------------------------------------------------

        /// Busca el dashboard incluyendo objetos inactivos, este donde este:
        /// puede colgar del TV o ya haber sido movido a Root.
        private static GameObject FindDashboard(out string where)
        {
            where = "no encontrado";
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != DashboardName) continue;
                    where = t.parent != null ? t.parent.name : "raiz de escena";
                    return t.gameObject;
                }
            }
            return null;
        }

        private static GameObject FindRoot()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == RootName) return go;
            return null;
        }
    }
}
#endif
