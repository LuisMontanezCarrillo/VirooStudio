#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ViroLab.Pasteurizador.EditorTools
{
    // Construye los prefabs de UI (Description Card + Side Panel) por codigo.
    // Esto evita tener que armar los prefabs a mano en el editor.
    public static class PasteurizerUIBuilder
    {
        private const string UIDir = "Assets/Prefabs/Pasteurizador_HTST/UI";
        private const string CardPath = UIDir + "/DescriptionCard.prefab";
        private const string PanelPath = UIDir + "/SidePanel.prefab";
        private const string CanvasPath = UIDir + "/Pasteurizador_UI_Canvas.prefab";

        [MenuItem("Viroo/Pasteurizador HTST/3. Construir UI (Card + SidePanel)", priority = 102)]
        public static void BuildUI()
        {
            Directory.CreateDirectory(UIDir);

            var cardPrefab = BuildDescriptionCardPrefab();
            var groupRow = BuildGroupRowPrefab();
            var partRow = BuildPartRowPrefab();
            var panelPrefab = BuildSidePanelPrefab(groupRow, partRow);
            var canvasPrefab = BuildCanvasPrefab(cardPrefab, panelPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                "UI generada:\n\n" +
                "- " + CardPath + "\n" +
                "- " + PanelPath + "\n" +
                "- " + CanvasPath + "\n\n" +
                "Arrastra Pasteurizador_UI_Canvas a la escena (o usa el menu 4).",
                "OK");
        }

        [MenuItem("Viroo/Pasteurizador HTST/4. Instanciar UI Canvas en escena + conectar", priority = 103)]
        public static void InstantiateUIInScene()
        {
            var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath);
            if (canvasPrefab == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    "Primero ejecuta '3. Construir UI'.", "OK");
                return;
            }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            var existing = GameObject.Find("Pasteurizador_UI_Canvas");
            if (existing != null)
            {
                Selection.activeObject = existing;
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add Pasteurizador UI");

            // Conectar references al pasteurizador en escena
            var pasteurizador = GameObject.Find("Pasteurizador_HTST");
            if (pasteurizador != null)
            {
                var registry = pasteurizador.GetComponent<PasteurizerPartsRegistry>();
                var hover = pasteurizador.GetComponent<PasteurizerHoverHandler>();

                var card = instance.GetComponentInChildren<PasteurizerDescriptionCard>(true);
                if (card != null) card.hover = hover;
                var panel = instance.GetComponentInChildren<PasteurizerSidePanel>(true);
                if (panel != null) { panel.registry = registry; panel.hover = hover; }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = instance;
        }

        // ===========================================================
        // Description Card prefab
        // ===========================================================
        private static GameObject BuildDescriptionCardPrefab()
        {
            var root = new GameObject("DescriptionCard", typeof(RectTransform), typeof(CanvasGroup), typeof(PasteurizerDescriptionCard));
            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20);
            rt.sizeDelta = new Vector2(420, 220);

            var border = AddImage(root.transform, "Border", new Color(1f, 0.84f, 0f, 1f));
            FillParent(border.rectTransform);

            var bg = AddImage(root.transform, "Background", new Color(0.07f, 0.08f, 0.1f, 0.92f));
            var bgRT = bg.rectTransform;
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = new Vector2(2, 2); bgRT.offsetMax = new Vector2(-2, -2);

            var tagBG = AddImage(root.transform, "TagBackground", new Color(1f, 0.84f, 0f, 1f));
            tagBG.rectTransform.anchorMin = new Vector2(0, 1);
            tagBG.rectTransform.anchorMax = new Vector2(0, 1);
            tagBG.rectTransform.pivot = new Vector2(0, 1);
            tagBG.rectTransform.anchoredPosition = new Vector2(10, -10);
            tagBG.rectTransform.sizeDelta = new Vector2(80, 26);

            var tagLabel = AddText(tagBG.transform, "TagLabel", "TAG", 14, Color.black, TextAlignmentOptions.Center);
            FillParent(tagLabel.rectTransform);

            var titleLabel = AddText(root.transform, "TitleLabel", "Subsistema", 18, new Color(1f, 0.92f, 0.48f), TextAlignmentOptions.Left);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.rectTransform.anchorMin = new Vector2(0, 1);
            titleLabel.rectTransform.anchorMax = new Vector2(1, 1);
            titleLabel.rectTransform.pivot = new Vector2(0, 1);
            titleLabel.rectTransform.anchoredPosition = new Vector2(100, -10);
            titleLabel.rectTransform.sizeDelta = new Vector2(-110, 26);

            var partNameLabel = AddText(root.transform, "PartNameLabel", "part_name", 11, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Left);
            partNameLabel.fontStyle = FontStyles.Italic;
            partNameLabel.rectTransform.anchorMin = new Vector2(0, 1);
            partNameLabel.rectTransform.anchorMax = new Vector2(1, 1);
            partNameLabel.rectTransform.pivot = new Vector2(0, 1);
            partNameLabel.rectTransform.anchoredPosition = new Vector2(12, -42);
            partNameLabel.rectTransform.sizeDelta = new Vector2(-20, 18);

            var descLabel = AddText(root.transform, "DescriptionLabel", "Descripcion...", 13, new Color(0.81f, 0.82f, 0.84f), TextAlignmentOptions.TopLeft);
            descLabel.rectTransform.anchorMin = new Vector2(0, 0);
            descLabel.rectTransform.anchorMax = new Vector2(1, 1);
            descLabel.rectTransform.offsetMin = new Vector2(12, 12);
            descLabel.rectTransform.offsetMax = new Vector2(-12, -65);
            descLabel.enableWordWrapping = true;

            var closeBtn = AddButton(root.transform, "CloseButton", "x", 14, Color.white);
            closeBtn.rectTransform.anchorMin = new Vector2(1, 1);
            closeBtn.rectTransform.anchorMax = new Vector2(1, 1);
            closeBtn.rectTransform.pivot = new Vector2(1, 1);
            closeBtn.rectTransform.anchoredPosition = new Vector2(-8, -8);
            closeBtn.rectTransform.sizeDelta = new Vector2(24, 24);

            var card = root.GetComponent<PasteurizerDescriptionCard>();
            card.canvasGroup = root.GetComponent<CanvasGroup>();
            card.canvasGroup.alpha = 0f;
            card.canvasGroup.blocksRaycasts = false;
            card.borderImage = border;
            card.tagBackground = tagBG;
            card.tagLabel = tagLabel;
            card.titleLabel = titleLabel;
            card.partNameLabel = partNameLabel;
            card.descriptionLabel = descLabel;
            card.closeButton = closeBtn.GetComponent<Button>();
            card.fadeAlpha = 0f;
            card.showOnPin = true;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ===========================================================
        // Side Panel prefab
        // ===========================================================
        private static GameObject BuildSidePanelPrefab(GameObject groupRowPrefab, GameObject partRowPrefab)
        {
            var root = new GameObject("SidePanel", typeof(RectTransform), typeof(PasteurizerSidePanel));
            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(360, 0);
            rt.anchoredPosition = Vector2.zero;

            var bg = AddImage(root.transform, "Background", new Color(0.137f, 0.153f, 0.180f, 0.95f));
            FillParent(bg.rectTransform);

            var title = AddText(root.transform, "Title", "Pasteurizador HTST", 16, new Color(0.54f, 0.71f, 0.97f), TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -10);
            title.rectTransform.sizeDelta = new Vector2(-20, 24);

            // Botones
            var btnRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnRow.transform.SetParent(root.transform, false);
            var brRT = (RectTransform)btnRow.transform;
            brRT.anchorMin = new Vector2(0, 1); brRT.anchorMax = new Vector2(1, 1);
            brRT.pivot = new Vector2(0.5f, 1);
            brRT.anchoredPosition = new Vector2(0, -42);
            brRT.sizeDelta = new Vector2(-20, 28);
            var hlg = btnRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            var btnAll = AddButton(btnRow.transform, "BtnShowAll", "Mostrar todo", 11, Color.white);
            var btnNone = AddButton(btnRow.transform, "BtnHideAll", "Ocultar todo", 11, Color.white);
            var btnFocus = AddButton(btnRow.transform, "BtnFocus", "Centrar", 11, Color.white);

            // Search
            var searchGO = new GameObject("Search", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            searchGO.transform.SetParent(root.transform, false);
            var srt = (RectTransform)searchGO.transform;
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1); srt.anchoredPosition = new Vector2(0, -76);
            srt.sizeDelta = new Vector2(-20, 28);
            searchGO.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.14f, 1f);
            var searchInput = searchGO.GetComponent<TMP_InputField>();
            var placeholder = AddText(searchGO.transform, "Placeholder", "Buscar parte...", 12, new Color(0.5f, 0.55f, 0.6f), TextAlignmentOptions.Left);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(8, 4); placeholder.rectTransform.offsetMax = new Vector2(-8, -4);
            placeholder.fontStyle = FontStyles.Italic;
            var textArea = AddText(searchGO.transform, "Text", "", 12, Color.white, TextAlignmentOptions.Left);
            textArea.rectTransform.anchorMin = Vector2.zero;
            textArea.rectTransform.anchorMax = Vector2.one;
            textArea.rectTransform.offsetMin = new Vector2(8, 4); textArea.rectTransform.offsetMax = new Vector2(-8, -4);
            searchInput.textComponent = textArea;
            searchInput.placeholder = placeholder;

            // ScrollRect con Viewport y Content
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(root.transform, false);
            var scRT = (RectTransform)scrollGO.transform;
            scRT.anchorMin = new Vector2(0, 0); scRT.anchorMax = new Vector2(1, 1);
            scRT.offsetMin = new Vector2(10, 10); scRT.offsetMax = new Vector2(-10, -110);
            scrollGO.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.12f, 1f);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = (RectTransform)viewport.transform;
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            sr.viewport = vpRT;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRT = (RectTransform)content.transform;
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0, 0);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 2; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = cRT;

            var panel = root.GetComponent<PasteurizerSidePanel>();
            panel.contentRoot = cRT;
            panel.searchField = searchInput;
            panel.groupRowPrefab = groupRowPrefab;
            panel.partRowPrefab = partRowPrefab;
            panel.btnShowAll = btnAll.GetComponent<Button>();
            panel.btnHideAll = btnNone.GetComponent<Button>();
            panel.btnFocus = btnFocus.GetComponent<Button>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildGroupRowPrefab()
        {
            var root = new GameObject("GroupRow", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var vlg = root.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 1; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var fitter = root.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            header.transform.SetParent(root.transform, false);
            header.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.235f);
            header.GetComponent<LayoutElement>().preferredHeight = 26;
            var toggle = header.AddComponent<Toggle>();
            toggle.targetGraphic = header.GetComponent<Image>();
            var dot = AddImage(header.transform, "ColorDot", Color.white);
            dot.rectTransform.anchorMin = new Vector2(0, 0.5f);
            dot.rectTransform.anchorMax = new Vector2(0, 0.5f);
            dot.rectTransform.pivot = new Vector2(0, 0.5f);
            dot.rectTransform.anchoredPosition = new Vector2(8, 0);
            dot.rectTransform.sizeDelta = new Vector2(10, 10);

            var label = AddText(header.transform, "Label", "Subsystem", 12, new Color(0.85f, 0.87f, 0.89f), TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = new Vector2(0, 0);
            label.rectTransform.anchorMax = new Vector2(1, 1);
            label.rectTransform.offsetMin = new Vector2(24, 0);
            label.rectTransform.offsetMax = new Vector2(-4, 0);
            label.fontStyle = FontStyles.Bold;

            // Children container
            var children = new GameObject("Children", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            children.transform.SetParent(root.transform, false);
            var chvlg = children.GetComponent<VerticalLayoutGroup>();
            chvlg.spacing = 1; chvlg.childForceExpandWidth = true; chvlg.childForceExpandHeight = false;
            chvlg.padding = new RectOffset(20, 0, 0, 0);
            children.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, UIDir + "/GroupRow.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildPartRowPrefab()
        {
            var root = new GameObject("PartRow", typeof(RectTransform), typeof(LayoutElement));
            root.GetComponent<LayoutElement>().preferredHeight = 20;

            var toggle = root.AddComponent<Toggle>();
            var bg = AddImage(root.transform, "Background", new Color(0.12f, 0.14f, 0.16f, 0.2f));
            FillParent(bg.rectTransform);
            toggle.targetGraphic = bg;

            var label = AddText(root.transform, "Label", "part_name", 11, new Color(0.80f, 0.82f, 0.84f), TextAlignmentOptions.Left);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8, 0);
            label.rectTransform.offsetMax = new Vector2(-30, 0);

            var focusBtn = AddButton(root.transform, "FocusBtn", "->", 10, Color.white);
            focusBtn.rectTransform.anchorMin = new Vector2(1, 0.5f);
            focusBtn.rectTransform.anchorMax = new Vector2(1, 0.5f);
            focusBtn.rectTransform.pivot = new Vector2(1, 0.5f);
            focusBtn.rectTransform.anchoredPosition = new Vector2(-4, 0);
            focusBtn.rectTransform.sizeDelta = new Vector2(20, 16);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, UIDir + "/PartRow.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildCanvasPrefab(GameObject cardPrefab, GameObject panelPrefab)
        {
            var root = new GameObject("Pasteurizador_UI_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var card = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
            card.transform.SetParent(root.transform, false);
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab);
            panel.transform.SetParent(root.transform, false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ----- UI helpers -----

        private static Image AddImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static TMP_Text AddText(Transform parent, string name, string text, int size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        private static Image AddButton(Transform parent, string name, string text, int size, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.232f, 0.259f, 0.314f, 1f);
            var lbl = AddText(go.transform, "Label", text, size, textColor, TextAlignmentOptions.Center);
            FillParent(lbl.rectTransform);
            return img;
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
