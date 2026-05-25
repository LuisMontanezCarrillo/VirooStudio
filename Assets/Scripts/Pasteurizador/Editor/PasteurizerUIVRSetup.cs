#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Reconfigura el Pasteurizador_UI_Canvas para que funcione en VR:
    ///   - Canvas en WorldSpace (sino no se ve en HMD)
    ///   - Agrega PasteurizerWorldCanvas (sigue suavemente a la mirada)
    ///   - Oculta el SidePanel por defecto (es demasiado grande para VR;
    ///     se puede activar a mano al lado del jugador)
    ///   - Mantiene la DescriptionCard activa flotando frente a la cara
    ///
    /// Ejecutar DESPUES de "4. Instanciar UI Canvas en escena + conectar".
    public static class PasteurizerUIVRSetup
    {
        private const string CanvasName = "Pasteurizador_UI_Canvas";

        [MenuItem("Viroo/Pasteurizador HTST/9. Configurar UI para VR (World Space + Follow)", priority = 110)]
        public static void SetupForVR()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST", "No hay escena cargada.", "OK");
                return;
            }

            var canvasGO = GameObject.Find(CanvasName);
            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No encontré '{CanvasName}' en la escena.\nCorré primero el paso 4.", "OK");
                return;
            }

            var canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"'{CanvasName}' no tiene componente Canvas.", "OK");
                return;
            }

            Undo.RecordObject(canvas, "VR Canvas Setup");
            canvas.renderMode = RenderMode.WorldSpace;

            // Quitar CanvasScaler si lo trae (en WorldSpace no aplica y puede romper layout)
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
            }

            // Asegurar GraphicRaycaster (para clicks futuros)
            if (canvasGO.GetComponent<GraphicRaycaster>() == null)
                canvasGO.AddComponent<GraphicRaycaster>();

            // Agregar PasteurizerWorldCanvas si no está
            var worldCanvas = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (worldCanvas == null)
            {
                worldCanvas = Undo.AddComponent<PasteurizerWorldCanvas>(canvasGO);
            }
            worldCanvas.followMode = PasteurizerWorldCanvas.FollowMode.FaceCameraSmoothed;
            worldCanvas.distance = 1.2f;
            worldCanvas.verticalOffset = -0.20f;
            worldCanvas.horizontalOffset = 0f;
            worldCanvas.worldScale = 0.001f;
            worldCanvas.positionLerp = 6f;
            worldCanvas.rotationLerp = 6f;
            worldCanvas.showOnlyWhenPinned = true;  // solo aparece al hacer click

            // Buscar hover handler para conectarlo
            var hover = Object.FindFirstObjectByType<PasteurizerHoverHandler>();
            worldCanvas.hover = hover;

            // Reposicionar el RectTransform del Canvas en (0,0,0) — el script lo moverá
            var rt = canvasGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                Undo.RecordObject(rt, "VR Canvas RT");
                rt.localPosition = Vector3.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one * worldCanvas.worldScale;
                // Tamaño en píxeles "virtuales" del canvas (1000 x 600 ~ 1m x 0.6m)
                rt.sizeDelta = new Vector2(1000f, 600f);
            }

            // Configurar DescriptionCard centrada en el canvas
            var card = canvasGO.GetComponentInChildren<PasteurizerDescriptionCard>(true);
            if (card != null)
            {
                var cardRT = card.GetComponent<RectTransform>();
                if (cardRT != null)
                {
                    Undo.RecordObject(cardRT, "Card RT");
                    cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                    cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                    cardRT.pivot = new Vector2(0.5f, 0.5f);
                    cardRT.anchoredPosition = Vector2.zero;
                    // Card más grande en VR para que se lea
                    cardRT.sizeDelta = new Vector2(600f, 340f);
                }
                // Conectar hover si quedó vacío
                if (card.hover == null) card.hover = hover;
            }

            // Ocultar SidePanel (es demasiado en VR; el usuario puede mostrarlo a mano)
            var panel = canvasGO.GetComponentInChildren<PasteurizerSidePanel>(true);
            if (panel != null)
            {
                Undo.RecordObject(panel.gameObject, "Hide SidePanel in VR");
                panel.gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = canvasGO;

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                "Canvas configurado para VR:\n\n" +
                $"• RenderMode = WorldSpace\n" +
                $"• Sigue la mirada (lerp suave, 1.2 m al frente)\n" +
                $"• Solo aparece al hacer click en una parte\n" +
                $"• Card centrada 600×340 (legible en VR)\n" +
                $"• SidePanel oculto (muy grande para VR)\n\n" +
                "Entrá a Play, hacé click en una pieza y la tarjeta aparece flotando frente tuyo.",
                "OK");

            Debug.Log("<color=cyan>[Pasteurizador HTST]</color> UI VR Setup completo. " +
                      "Canvas en WorldSpace, sigue cámara con lerp suave.");
        }

        [MenuItem("Viroo/Pasteurizador HTST/Restaurar UI a modo escritorio (ScreenSpace)", priority = 111)]
        public static void RestoreDesktop()
        {
            var canvasGO = GameObject.Find(CanvasName);
            if (canvasGO == null) return;
            var canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null) return;

            Undo.RecordObject(canvas, "Restore Desktop Canvas");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;

            var worldCanvas = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (worldCanvas != null) Undo.DestroyObjectImmediate(worldCanvas);

            var rt = canvasGO.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;

            Debug.Log("<color=cyan>[Pasteurizador HTST]</color> Canvas restaurado a ScreenSpaceOverlay.");
        }
    }
}
#endif
