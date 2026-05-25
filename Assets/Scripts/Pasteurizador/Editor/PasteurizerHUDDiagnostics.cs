#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Diagnostica el estado completo del sistema HUD del pasteurizador.
    /// Imprime un reporte línea por línea y aplica fixes automáticos si
    /// se detectan problemas obvios (auto-conectar references, mostrar la
    /// card aunque no haya pinned, etc).
    public static class PasteurizerHUDDiagnostics
    {
        private const string CanvasName = "Pasteurizador_UI_Canvas";
        private const string PrefabName = "Pasteurizador_HTST";

        [MenuItem("Viroo/Pasteurizador HTST/Diagnosticar HUD (consola)", priority = 140)]
        public static void Diagnose()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<color=cyan>===== PASTEURIZADOR HUD DIAGNOSTIC =====</color>");

            // 1) Pasteurizador en escena
            var paste = GameObject.Find(PrefabName);
            sb.AppendLine(paste != null
                ? $"✅ Pasteurizador en escena: '{paste.name}'"
                : $"❌ NO HAY '{PrefabName}' en escena. Corré paso 2.");

            PasteurizerHoverHandler hover = null;
            PasteurizerPartsRegistry registry = null;
            if (paste != null)
            {
                hover = paste.GetComponent<PasteurizerHoverHandler>();
                registry = paste.GetComponent<PasteurizerPartsRegistry>();
                sb.AppendLine(hover != null
                    ? "  ✅ Tiene PasteurizerHoverHandler"
                    : "  ❌ FALTA PasteurizerHoverHandler en el pasteurizador");
                sb.AppendLine(registry != null
                    ? "  ✅ Tiene PasteurizerPartsRegistry"
                    : "  ❌ FALTA PasteurizerPartsRegistry en el pasteurizador");
                if (registry != null && registry.Database != null)
                    sb.AppendLine($"  ✅ Database: {registry.Database.name} (cargado)");
                else
                    sb.AppendLine("  ⚠️  Database del Registry está vacío (se carga en Awake desde Resources)");
            }
            else
            {
                hover = Object.FindFirstObjectByType<PasteurizerHoverHandler>();
                if (hover != null)
                    sb.AppendLine($"  ⚠️  Encontré HoverHandler suelto en otro GO: '{hover.gameObject.name}'");
            }

            sb.AppendLine();

            // 2) Canvas
            var canvasGO = GameObject.Find(CanvasName);
            sb.AppendLine(canvasGO != null
                ? $"✅ Canvas en escena: '{canvasGO.name}'"
                : $"❌ NO HAY '{CanvasName}' en escena. Corré paso 4.");

            if (canvasGO == null) { Print(sb); return; }

            var canvas = canvasGO.GetComponent<Canvas>();
            sb.AppendLine($"  RenderMode = {canvas.renderMode} {(canvas.renderMode == RenderMode.WorldSpace ? "✅" : "❌ debería ser WorldSpace para VR")}");
            sb.AppendLine($"  Canvas.enabled = {canvas.enabled}");
            sb.AppendLine($"  Canvas.worldCamera = {(canvas.worldCamera != null ? canvas.worldCamera.name : "null ⚠️")}");

            var worldCanvas = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (worldCanvas == null)
            {
                sb.AppendLine("  ❌ FALTA PasteurizerWorldCanvas. Corré paso 9 o 10.");
            }
            else
            {
                sb.AppendLine("  ✅ Tiene PasteurizerWorldCanvas");
                sb.AppendLine($"    - followMode = {worldCanvas.followMode}");
                sb.AppendLine($"    - viewportAnchor = {worldCanvas.viewportAnchor}");
                sb.AppendLine($"    - hudDistance = {worldCanvas.hudDistance}");
                sb.AppendLine($"    - worldScale = {worldCanvas.worldScale}");
                sb.AppendLine($"    - showOnlyWhenPinned = {worldCanvas.showOnlyWhenPinned}");
                sb.AppendLine($"    - hover ref = {(worldCanvas.hover != null ? worldCanvas.hover.gameObject.name : "❌ NULL")}");
            }

            var canvasGroup = canvasGO.GetComponent<CanvasGroup>();
            sb.AppendLine(canvasGroup != null
                ? $"  ✅ CanvasGroup (alpha={canvasGroup.alpha:F2})"
                : "  ⚠️  Sin CanvasGroup (se agrega en Awake)");

            sb.AppendLine();

            // 3) DescriptionCard
            var card = canvasGO.GetComponentInChildren<PasteurizerDescriptionCard>(true);
            if (card == null)
            {
                sb.AppendLine("❌ NO HAY PasteurizerDescriptionCard dentro del Canvas. Corré paso 3.");
            }
            else
            {
                sb.AppendLine($"✅ DescriptionCard: '{card.gameObject.name}' (active={card.gameObject.activeInHierarchy})");
                sb.AppendLine($"  - hover ref = {(card.hover != null ? card.hover.gameObject.name : "❌ NULL")}");
                sb.AppendLine($"  - showOnPin = {card.showOnPin}");
                sb.AppendLine($"  - canvasGroup = {(card.canvasGroup != null ? $"alpha={card.canvasGroup.alpha:F2}" : "❌ NULL")}");
                sb.AppendLine($"  - titleLabel = {(card.titleLabel != null ? "OK" : "❌ NULL")}");
                sb.AppendLine($"  - descriptionLabel = {(card.descriptionLabel != null ? "OK" : "❌ NULL")}");
                sb.AppendLine($"  - tagLabel = {(card.tagLabel != null ? "OK" : "❌ NULL")}");
            }

            sb.AppendLine();

            // 4) Cámara
            var cam = Camera.main;
            if (cam == null && Camera.allCameras.Length > 0) cam = Camera.allCameras[0];
            sb.AppendLine(cam != null
                ? $"✅ Cámara activa: '{cam.name}' (tag={cam.tag})"
                : "❌ NO HAY cámara activa");

            sb.AppendLine();
            sb.AppendLine("<color=cyan>===== FIN DIAGNOSTIC =====</color>");
            Print(sb);
        }

        [MenuItem("Viroo/Pasteurizador HTST/Reparar HUD (auto-fix)", priority = 141)]
        public static void AutoFix()
        {
            int fixes = 0;
            var canvasGO = GameObject.Find(CanvasName);
            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    "No hay Canvas en escena. Corré primero los pasos 3, 4, 9 y 10.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvasGO, "HUD Auto-Fix");

            // Canvas en WorldSpace
            var canvas = canvasGO.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                fixes++;
            }
            if (canvas != null && canvas.worldCamera == null)
            {
                var cam = Camera.main;
                if (cam == null && Camera.allCameras.Length > 0) cam = Camera.allCameras[0];
                if (cam != null) { canvas.worldCamera = cam; fixes++; }
            }

            // Asegurar PasteurizerWorldCanvas con HUDAnchor arriba-izquierda
            var wc = canvasGO.GetComponent<PasteurizerWorldCanvas>();
            if (wc == null)
            {
                wc = Undo.AddComponent<PasteurizerWorldCanvas>(canvasGO);
                fixes++;
            }
            wc.followMode = PasteurizerWorldCanvas.FollowMode.HUDAnchor;
            if (wc.viewportAnchor == Vector2.zero) wc.viewportAnchor = new Vector2(0.18f, 0.82f);
            if (wc.hudDistance <= 0) wc.hudDistance = 0.8f;
            if (wc.worldScale <= 0) wc.worldScale = 0.0008f;
            wc.showOnlyWhenPinned = false;  // DESHABILITAR temporalmente para que VEAS la card

            // Conectar hover
            var hover = Object.FindFirstObjectByType<PasteurizerHoverHandler>();
            if (wc.hover == null && hover != null) { wc.hover = hover; fixes++; }

            // CanvasGroup
            var cg = canvasGO.GetComponent<CanvasGroup>();
            if (cg == null) { cg = canvasGO.AddComponent<CanvasGroup>(); fixes++; }
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            // Card: conectar hover, asegurar visible
            var card = canvasGO.GetComponentInChildren<PasteurizerDescriptionCard>(true);
            if (card != null)
            {
                if (card.hover == null && hover != null) { card.hover = hover; fixes++; }
                if (!card.gameObject.activeSelf) { card.gameObject.SetActive(true); fixes++; }
                if (card.canvasGroup != null)
                {
                    card.canvasGroup.alpha = 1f;     // visible
                    card.canvasGroup.blocksRaycasts = true;
                    card.canvasGroup.interactable = true;
                }
                // Inyectar texto placeholder para CONFIRMAR visualmente que la card está renderizando
                if (card.titleLabel != null) card.titleLabel.text = "HUD TEST OK";
                if (card.tagLabel != null) card.tagLabel.text = "TEST";
                if (card.descriptionLabel != null)
                    card.descriptionLabel.text = "Si ves este texto en Play, el HUD está bien posicionado. Hacé click en una pieza para reemplazarlo con la info del subsistema.";
                if (card.partNameLabel != null) card.partNameLabel.text = "diagnostic_placeholder";
            }

            // Tamaño canvas
            var rt = canvasGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(700f, 360f);
                rt.localScale = Vector3.one * wc.worldScale;
            }

            EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            Selection.activeObject = canvasGO;

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                $"Auto-fix aplicado ({fixes} cambios).\n\n" +
                "• showOnlyWhenPinned = FALSE (verás la card SIEMPRE en Play)\n" +
                "• Card con texto placeholder \"HUD TEST OK\"\n\n" +
                "Entrá a Play:\n" +
                "  - Si ves 'HUD TEST OK' arriba-izq → el HUD funciona, era cuestión de visibilidad\n" +
                "  - Si NO ves nada → el problema es de cámara/render. Revisá la consola.\n\n" +
                "Después de confirmar, podés reactivar showOnlyWhenPinned en el inspector.",
                "OK");

            Debug.Log($"<color=cyan>[Pasteurizador HTST]</color> Auto-fix: {fixes} cambios aplicados.");
        }

        private static void Print(StringBuilder sb)
        {
            Debug.Log(sb.ToString());
        }
    }
}
#endif
