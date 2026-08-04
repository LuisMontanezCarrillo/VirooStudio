#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Coloca una caja invisible detras de cada Canvas World Space para que el rayo del
    /// explorador de piezas se detenga ahi en lugar de atravesar el panel y golpear el
    /// pasteurizador que hay detras.
    ///
    /// Es la solucion geometrica al problema de "pulso SIGUIENTE en el carrusel y se
    /// abre la ficha de una pieza": la UI de Unity no tiene colliders, asi que ningun
    /// raycast fisico la ve. Resolverlo por eventos de UI no es fiable porque depende
    /// del modo de interaccion (VR con NearFarInteractor vs escritorio con el interactor
    /// de raton propio de VIROO).
    public static class PasteurizerUIRayBlockers
    {
        private const string Menu = "Viroo/Adecuacion VIROO/";
        private const string BlockerName = "_RayBlocker";

        /// Separacion hacia atras respecto al plano del canvas. Va DETRAS a proposito:
        /// delante podria bloquear el raycast de la propia UI y dejar los botones
        /// sin responder.
        private const float BackOffset = 0.05f;
        private const float BlockerThickness = 0.02f;

        [MenuItem(Menu + "6. Bloquear el rayo en los paneles de UI", priority = 8)]
        public static void CreateBlockers()
        {
            var scene = SceneManager.GetActiveScene();
            int creados = 0, actualizados = 0, omitidos = 0;
            var report = new List<string>();

            foreach (var canvas in Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.gameObject.scene != scene) continue;

                // Solo canvases raiz: los anidados heredan el render mode del padre.
                var parent = canvas.transform.parent;
                if (parent != null && parent.GetComponentInParent<Canvas>() != null) continue;

                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    omitidos++;
                    continue;
                }

                var rt = canvas.GetComponent<RectTransform>();
                if (rt == null) { omitidos++; continue; }

                var existente = canvas.transform.Find(BlockerName);
                GameObject blocker;
                if (existente != null)
                {
                    blocker = existente.gameObject;
                    actualizados++;
                }
                else
                {
                    blocker = new GameObject(BlockerName);
                    Undo.RegisterCreatedObjectUndo(blocker, "Crear bloqueador de rayo");
                    Undo.SetTransformParent(blocker.transform, canvas.transform,
                        "Crear bloqueador de rayo");
                    creados++;
                }

                // Se usa lossyScale (escala real en el mundo) y no localScale: el canvas
                // puede colgar de objetos escalados y entonces no coinciden.
                float escala = Mathf.Max(0.0001f, Mathf.Abs(rt.lossyScale.z));

                Undo.RecordObject(blocker.transform, "Configurar bloqueador");
                blocker.transform.localRotation = Quaternion.identity;
                blocker.transform.localScale = Vector3.one;
                // El contenido del canvas mira hacia -Z, asi que "detras" es +Z local.
                blocker.transform.localPosition = new Vector3(0f, 0f, BackOffset / escala);

                var box = blocker.GetComponent<BoxCollider>();
                if (box == null) box = Undo.AddComponent<BoxCollider>(blocker);
                Undo.RecordObject(box, "Configurar bloqueador");
                box.isTrigger = false;
                // El collider es hijo del canvas, asi que trabaja en unidades de canvas
                // (pixeles): el tamaño se toma tal cual del rect.
                box.size = new Vector3(rt.rect.width, rt.rect.height, BlockerThickness / escala);
                box.center = Vector3.zero;

                if (blocker.GetComponent<PasteurizerRayBlocker>() == null)
                    Undo.AddComponent<PasteurizerRayBlocker>(blocker);

                report.Add($"  - {canvas.name}: {rt.rect.width:0}x{rt.rect.height:0} px " +
                           $"({rt.rect.width * rt.localScale.x:0.00} x " +
                           $"{rt.rect.height * rt.localScale.y:0.00} m)");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool guardada = EditorSceneManager.SaveScene(scene);

            var detail = report.Count > 0 ? "\n" + string.Join("\n", report) : string.Empty;
            Debug.Log($"<color=cyan>[Bloqueadores de rayo]</color> {creados} creados, " +
                      $"{actualizados} actualizados, {omitidos} canvas omitidos " +
                      $"(no World Space). Escena guardada: {guardada}.{detail}");

            EditorUtility.DisplayDialog("Bloqueadores de rayo",
                $"{creados} bloqueadores creados y {actualizados} actualizados.\n\n" +
                "Los paneles de UI ya detienen el rayo del explorador: pulsar los botones " +
                "del carrusel o del cuestionario dejara de abrir fichas de piezas del " +
                "pasteurizador.\n\n" +
                "Las cajas van detras del plano de cada panel, asi que no interfieren con " +
                "los botones." +
                (guardada ? "\n\nLa escena se guardo automaticamente." : ""),
                "OK");
        }

        [MenuItem(Menu + "6b. Quitar los bloqueadores de rayo", priority = 9)]
        public static void RemoveBlockers()
        {
            var scene = SceneManager.GetActiveScene();
            int borrados = 0;

            foreach (var b in Object.FindObjectsByType<PasteurizerRayBlocker>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.gameObject.scene != scene) continue;
                Undo.DestroyObjectImmediate(b.gameObject);
                borrados++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=cyan>[Bloqueadores de rayo]</color> {borrados} eliminados.");
        }
    }
}
#endif
