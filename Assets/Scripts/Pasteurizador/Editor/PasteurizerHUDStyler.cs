#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Aplica un estilo HUD futurista al Canvas del pasteurizador y lo ancla
    /// en la esquina superior izquierda del campo visual del usuario.
    /// Compatible con XR / Viroо (el canvas se posiciona en WorldSpace
    /// relativo al viewport de la cámara cada frame).
    ///
    /// Estética:
    ///   - Borde cyan brillante con emisión
    ///   - Fondo negro semi-transparente con leve tinte azul
    ///   - 4 corner brackets (típicos sci-fi HUD) en las esquinas
    ///   - Tag con color del subsistema y halo
    ///   - Tipografía limpia, título cyan, descripción gris claro
    public static class PasteurizerHUDStyler
    {
        private const string CanvasName = "Pasteurizador_UI_Canvas";

        // Paleta cibernética
        private static readonly Color HUDCyan      = new Color(0.00f, 0.88f, 1.00f, 1.00f);
        private static readonly Color HUDCyanDim   = new Color(0.00f, 0.60f, 0.85f, 1.00f);
        private static readonly Color HUDBgDark    = new Color(0.02f, 0.06f, 0.10f, 0.85f);
        private static readonly Color HUDLine      = new Color(0.00f, 0.88f, 1.00f, 0.55f);
        private static readonly Color HUDTextMain  = new Color(0.92f, 0.96f, 1.00f, 1.00f);
        private static readonly Color HUDTextDim   = new Color(0.65f, 0.80f, 0.90f, 1.00f);

        [MenuItem("Viroo/Pasteurizador HTST/10. Aplicar estilo HUD cibernético (arriba-izquierda)", priority = 112)]
        public static void ApplyHUDStyle()
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
                    $"No encontré '{CanvasName}' en escena.\nCorré antes los pasos 4 y 9.", "OK");
                return;
            }

            // 1) Configurar WorldCanvas en modo HUDAnchor (arriba-izquierda)
            var worldCanvas = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (worldCanvas == null)
                worldCanvas = Undo.AddComponent<PasteurizerWorldCanvas>(canvasGO);

            Undo.RecordObject(worldCanvas, "HUD Anchor Setup");
            worldCanvas.followMode = PasteurizerWorldCanvas.FollowMode.HUDAnchor;
            worldCanvas.viewportAnchor = new Vector2(0.18f, 0.82f); // ~18% derecha del borde izq, ~82% arriba (= esquina sup-izq con margen)
            worldCanvas.hudDistance = 0.8f;
            worldCanvas.hudSmoothing = false;   // pegado rígido (típico HUD scifi)
            worldCanvas.worldScale = 0.0008f;   // un poco más chico para que entre cómodo
            worldCanvas.showOnlyWhenPinned = true;

            // Conectar hover si no está
            var hover = Object.FindFirstObjectByType<PasteurizerHoverHandler>();
            if (worldCanvas.hover == null) worldCanvas.hover = hover;

            // Asegurar Canvas en WorldSpace
            var canvas = canvasGO.GetComponent<Canvas>();
            if (canvas != null)
            {
                Undo.RecordObject(canvas, "Canvas WorldSpace");
                canvas.renderMode = RenderMode.WorldSpace;
                if (canvas.worldCamera == null)
                {
                    var cam = Camera.main;
                    if (cam == null && Camera.allCameras.Length > 0) cam = Camera.allCameras[0];
                    canvas.worldCamera = cam;
                }
            }

            // 2) Buscar la DescriptionCard y reestilizarla
            var card = canvasGO.GetComponentInChildren<PasteurizerDescriptionCard>(true);
            if (card == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    "No encontré DescriptionCard. Corré antes el paso 3.", "OK");
                return;
            }

            RestylizeCard(card);

            // 3) Ajustar el RectTransform del Canvas a un tamaño cómodo para HUD
            //    (más grande ahora para acompañar las fuentes más grandes)
            var canvasRT = canvasGO.GetComponent<RectTransform>();
            if (canvasRT != null)
            {
                Undo.RecordObject(canvasRT, "HUD Canvas RT");
                canvasRT.sizeDelta = new Vector2(900f, 460f);
                canvasRT.localScale = Vector3.one * worldCanvas.worldScale;
            }

            // 4) Card: anclar al centro del canvas y darle tamaño grande para HUD
            var cardRT = card.GetComponent<RectTransform>();
            if (cardRT != null)
            {
                Undo.RecordObject(cardRT, "HUD Card RT");
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.anchoredPosition = Vector2.zero;
                cardRT.sizeDelta = new Vector2(880f, 440f);
            }

            // 5) Hide SidePanel (no en HUD)
            var panel = canvasGO.GetComponentInChildren<PasteurizerSidePanel>(true);
            if (panel != null)
            {
                Undo.RecordObject(panel.gameObject, "Hide SidePanel HUD");
                panel.gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = canvasGO;

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                "Estilo HUD cibernético aplicado:\n\n" +
                "• Anclado arriba-izquierda del campo visual\n" +
                "• Borde cyan con emisión\n" +
                "• Corner brackets sci-fi en las 4 esquinas\n" +
                "• Línea decorativa bajo el título\n" +
                "• Solo aparece al hacer click en una pieza\n\n" +
                "Entrá a Play y hacé click en cualquier pieza.\n" +
                "Para mover el HUD: ajustá 'Viewport Anchor' en\n" +
                "el componente PasteurizerWorldCanvas.",
                "OK");

            Debug.Log("<color=cyan>[Pasteurizador HTST]</color> HUD cibernético aplicado.");
        }

        // ====================================================================
        // Restilización de la card
        // ====================================================================
        private static void RestylizeCard(PasteurizerDescriptionCard card)
        {
            var go = card.gameObject;
            Undo.RegisterFullObjectHierarchyUndo(go, "HUD Restyle Card");

            // Border = cyan con emisión
            if (card.borderImage != null)
            {
                card.borderImage.color = HUDCyan;
                EnsureEmissive(card.borderImage, HUDCyan * 1.5f);
            }

            // Background — buscarlo entre los hijos
            var bgT = go.transform.Find("Background");
            if (bgT != null)
            {
                var bgImg = bgT.GetComponent<Image>();
                if (bgImg != null) bgImg.color = HUDBgDark;
            }

            // Tag background: el color lo asigna runtime con el color del subsistema
            // pero le damos un estilo de "chip" con altura mejor
            if (card.tagBackground != null)
            {
                var tagRT = card.tagBackground.rectTransform;
                tagRT.sizeDelta = new Vector2(110f, 32f);
                tagRT.anchoredPosition = new Vector2(14f, -14f);
                EnsureEmissive(card.tagBackground, Color.white * 0.4f);
            }
            if (card.tagLabel != null)
            {
                card.tagLabel.fontSize = 22f;
                card.tagLabel.fontStyle = FontStyles.Bold;
                card.tagLabel.color = Color.black;
            }

            // Tag más grande
            if (card.tagBackground != null)
            {
                var tagRT2 = card.tagBackground.rectTransform;
                tagRT2.sizeDelta = new Vector2(150f, 44f);
                tagRT2.anchoredPosition = new Vector2(16f, -16f);
            }

            // Título (subsystem.title): cyan brillante, GRANDE
            if (card.titleLabel != null)
            {
                card.titleLabel.color = HUDCyan;
                card.titleLabel.fontSize = 32f;
                card.titleLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                var trt = card.titleLabel.rectTransform;
                trt.anchoredPosition = new Vector2(180f, -16f);
                trt.sizeDelta = new Vector2(-190f, 44f);
            }

            // Part name (técnico) - más grande
            if (card.partNameLabel != null)
            {
                card.partNameLabel.color = HUDTextDim;
                card.partNameLabel.fontSize = 18f;
                card.partNameLabel.fontStyle = FontStyles.Italic;
                var prt = card.partNameLabel.rectTransform;
                prt.anchoredPosition = new Vector2(20f, -72f);
                prt.sizeDelta = new Vector2(-40f, 24f);
            }

            // Descripción: blanco-azulado, GRANDE para que se lea en VR
            if (card.descriptionLabel != null)
            {
                card.descriptionLabel.color = HUDTextMain;
                card.descriptionLabel.fontSize = 22f;
                card.descriptionLabel.enableWordWrapping = true;
                var drt = card.descriptionLabel.rectTransform;
                drt.anchorMin = new Vector2(0f, 0f);
                drt.anchorMax = new Vector2(1f, 1f);
                drt.offsetMin = new Vector2(24f, 24f);
                drt.offsetMax = new Vector2(-24f, -110f);
            }

            // Línea decorativa horizontal bajo el header (más abajo por las fuentes grandes)
            EnsureDecorLine(go.transform, "HUD_Divider", new Vector2(0, 1),
                new Vector2(24f, -104f), new Vector2(-24f, 3f), HUDLine);

            // 4 Corner brackets
            EnsureCornerBracket(go.transform, "Corner_TL", new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, -1), HUDCyan);
            EnsureCornerBracket(go.transform, "Corner_TR", new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(-1, -1), HUDCyan);
            EnsureCornerBracket(go.transform, "Corner_BL", new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(0, 0), new Vector2(1, 1), HUDCyan);
            EnsureCornerBracket(go.transform, "Corner_BR", new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(-1, 1), HUDCyan);
        }

        // ---- helpers ----
        private static void EnsureEmissive(Graphic g, Color emission)
        {
            // En UI/Default los shaders no tienen _EmissionColor real, pero
            // si el material default acepta tint emissive lo intentamos.
            // (Para shader UI/Default no hace nada: lo dejamos por si custom.)
            var mat = g.material;
            if (mat != null && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }

        /// Una línea horizontal/vertical decorativa
        private static void EnsureDecorLine(Transform parent, string name,
            Vector2 anchor, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            Transform existing = null;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) { existing = parent.GetChild(i); break; }
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
            }
            else go = existing.gameObject;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, anchor.y);
            rt.anchorMax = new Vector2(1, anchor.y);
            rt.pivot = new Vector2(0.5f, anchor.y);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        /// Crea un corner bracket "L" en una esquina. La forma se compone de
        /// 2 finas barras unidas; el sign (signX, signY) define hacia dónde
        /// se extienden las barras desde la esquina.
        private static void EnsureCornerBracket(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sign, Color color)
        {
            Transform existing = null;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) { existing = parent.GetChild(i); break; }

            GameObject root;
            if (existing == null)
            {
                root = new GameObject(name, typeof(RectTransform));
                root.transform.SetParent(parent, false);
            }
            else root = existing.gameObject;

            var rt = (RectTransform)root.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(28f, 28f);

            // limpiar hijos previos
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            // Barra horizontal
            CreateBar(root.transform, "H", new Vector2(28f, 3f),
                new Vector2(sign.x * 14f, sign.y * 0f), color);
            // Barra vertical
            CreateBar(root.transform, "V", new Vector2(3f, 28f),
                new Vector2(sign.x * 0f, sign.y * 14f), color);
        }

        private static void CreateBar(Transform parent, string name, Vector2 size, Vector2 offset, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        // ====================================================================
        // Configuradores rápidos de posición del HUD
        // ====================================================================
        [MenuItem("Viroo/Pasteurizador HTST/11. Mover HUD a arriba-izquierda (texto grande)", priority = 113)]
        public static void RepositionTopLeftBig()
        {
            var canvasGO = GameObject.Find(CanvasName);
            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No encontré '{CanvasName}'. Corré antes los pasos 4 + 10.", "OK");
                return;
            }

            // 1) Forzar modo HUDAnchor arriba-izquierda.
            //    KEY FIX: viewportAnchor define la POSICIÓN del PIVOT del canvas.
            //    Si el pivot está en (0.5, 0.5) — centro — viewportAnchor=(0.20,0.80)
            //    coloca el CENTRO en esa posición, y la mitad izquierda se sale
            //    del viewport. Solución: pivot=(0, 1) → top-left del canvas
            //    queda exactamente en viewportAnchor.
            var wc = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (wc == null) wc = Undo.AddComponent<PasteurizerWorldCanvas>(canvasGO);
            Undo.RecordObject(wc, "HUD TopLeft");
            wc.followMode = PasteurizerWorldCanvas.FollowMode.HUDAnchor;
            wc.viewportAnchor = new Vector2(0.04f, 0.96f);   // esquina top-left con 4% margen
            wc.hudDistance = 0.9f;
            wc.worldScale = 0.0006f;                          // un poco más chico para que entre cómodo
            wc.hudSmoothing = false;

            // 2) Re-aplicar el estilo + tamaños grandes (idempotente)
            var card = canvasGO.GetComponentInChildren<PasteurizerDescriptionCard>(true);
            if (card != null) RestylizeCard(card);

            // 3) Re-conectar hover si quedó suelto
            var hover = Object.FindFirstObjectByType<PasteurizerHoverHandler>();
            if (wc.hover == null) wc.hover = hover;
            if (card != null && card.hover == null) card.hover = hover;

            // 4) Canvas: pivot top-left + tamaño grande
            var canvasRT = canvasGO.GetComponent<RectTransform>();
            if (canvasRT != null)
            {
                Undo.RecordObject(canvasRT, "Canvas RT");
                canvasRT.pivot = new Vector2(0f, 1f);   // ← KEY: top-left
                canvasRT.sizeDelta = new Vector2(900f, 460f);
                canvasRT.localScale = Vector3.one * wc.worldScale;
            }

            // 5) Card: estirar para llenar el canvas (sin centrar más, ya está en top-left)
            if (card != null)
            {
                var cardRT = card.GetComponent<RectTransform>();
                Undo.RecordObject(cardRT, "Card RT");
                // anclar a las 4 esquinas del canvas (stretch)
                cardRT.anchorMin = new Vector2(0f, 0f);
                cardRT.anchorMax = new Vector2(1f, 1f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.offsetMin = Vector2.zero;
                cardRT.offsetMax = Vector2.zero;
                cardRT.anchoredPosition = Vector2.zero;
                cardRT.sizeDelta = Vector2.zero;
            }

            EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            Selection.activeObject = canvasGO;

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                "HUD movido a esquina superior izquierda con texto grande.\n\n" +
                "• Pivot del Canvas = (0, 1) → esquina top-left exacta\n" +
                "• Viewport anchor = (0.04, 0.96) → 4% margen\n" +
                "• World scale = 0.0006 → ~54×28 cm en VR\n\n" +
                "Si querés más cerca/lejos: ajustá 'Hud Distance' en el componente.\n" +
                "Si querés más chico/grande: ajustá 'World Scale'.",
                "OK");
            Debug.Log("<color=cyan>[Pasteurizador HTST]</color> HUD reposicionado arriba-izq con pivot top-left.");
        }

        [MenuItem("Viroo/Pasteurizador HTST/HUD posición - Arriba Izquierda", priority = 130)]
        public static void HUDTopLeft() => SetHUDViewport(new Vector2(0.18f, 0.82f));

        [MenuItem("Viroo/Pasteurizador HTST/HUD posición - Arriba Derecha", priority = 131)]
        public static void HUDTopRight() => SetHUDViewport(new Vector2(0.82f, 0.82f));

        [MenuItem("Viroo/Pasteurizador HTST/HUD posición - Centro Abajo", priority = 132)]
        public static void HUDBottom() => SetHUDViewport(new Vector2(0.50f, 0.18f));

        [MenuItem("Viroo/Pasteurizador HTST/HUD posición - Centro", priority = 133)]
        public static void HUDCenter() => SetHUDViewport(new Vector2(0.50f, 0.50f));

        private static void SetHUDViewport(Vector2 anchor)
        {
            var canvasGO = GameObject.Find(CanvasName);
            if (canvasGO == null) return;
            var wc = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (wc == null) return;
            Undo.RecordObject(wc, "HUD Reposition");
            wc.viewportAnchor = anchor;
            EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> HUD reposicionado a viewport {anchor}");
        }
    }
}
#endif
