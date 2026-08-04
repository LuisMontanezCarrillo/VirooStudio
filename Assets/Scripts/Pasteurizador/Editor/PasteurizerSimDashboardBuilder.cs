#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViroLab.Pasteurizador.Simulator;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Construye el Dashboard del simulador HTST como Canvas WorldSpace
    /// hijo del GameObject "Simulador" (el TV en la pared). Reproduce el
    /// layout de web/index.html con tiles HUD cibernéticos: topbar de
    /// lecturas, secuencia de 6 pasos LED, válvulas FDV, botones de
    /// control, sliders de target/flow, métricas de lote.
    public static class PasteurizerSimDashboardBuilder
    {
        private const string TvName = "Simulador";

        // ---- paleta HUD ----
        private static readonly Color CBgDark   = new Color(0.04f, 0.06f, 0.10f, 0.95f);
        private static readonly Color CTile     = new Color(0.07f, 0.10f, 0.14f, 0.85f);
        private static readonly Color CTileEdge = new Color(0.00f, 0.55f, 0.85f, 0.65f);
        private static readonly Color CCyan     = new Color(0.00f, 0.88f, 1.00f, 1.00f);
        private static readonly Color CTextHi   = new Color(0.92f, 0.96f, 1.00f, 1.00f);
        private static readonly Color CTextLo   = new Color(0.62f, 0.75f, 0.85f, 1.00f);
        private static readonly Color CTextDim  = new Color(0.45f, 0.55f, 0.65f, 1.00f);

        // ---- layout (en unidades de canvas) ----
        // Canvas total 1600x900 (16:9 TV), scale 0.001 → 1.6m x 0.9m
        private const float CanvasW = 1600f;
        private const float CanvasH = 900f;
        private const float WorldScale = 0.001f;

        [MenuItem("Viroo/Pasteurizador HTST/13. Crear Dashboard Simulador en el TV", priority = 130)]
        public static void Build()
        {
            // CHECK 1: NO debe estar en Play mode (los cambios se pierden al salir)
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    "⚠️ Estás en PLAY MODE.\n\n" +
                    "Salí de Play (pulsá el botón Play arriba para detenerlo) y\n" +
                    "después ejecutá este menú de nuevo. Los cambios hechos en\n" +
                    "Play se PIERDEN al salir.",
                    "OK, salgo de Play");
                return;
            }

            var tv = GameObject.Find(TvName);
            if (tv == null)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    $"No encontré GameObject '{TvName}' en escena.\n" +
                    "Seleccioná el TV de Smart led TV y renombralo a 'Simulador' o\n" +
                    "modificá la constante TvName en el script.", "OK");
                return;
            }

            // Borrar dashboard previo (idempotente)
            var existing = tv.transform.Find("_SimDashboard");
            if (existing != null)
            {
                Debug.Log($"<color=yellow>[SimDashboard]</color> Borrando _SimDashboard previo...");
                Object.DestroyImmediate(existing.gameObject);
            }
            Debug.Log("<color=cyan>[SimDashboard]</color> Construyendo dashboard nuevo (grid 2 cols, español)...");

            // Asegurar el engine en el TV (necesario para que el Dashboard.engine != null)
            var engine = tv.GetComponent<PasteurizerSimEngine>();
            if (engine == null) engine = Undo.AddComponent<PasteurizerSimEngine>(tv);

            // Crear el Canvas hijo
            var canvasGO = new GameObject("_SimDashboard",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(PasteurizerSimDashboard));
            canvasGO.transform.SetParent(tv.transform, false);
            var rt = (RectTransform)canvasGO.transform;
            rt.sizeDelta = new Vector2(CanvasW, CanvasH);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Posicionar el canvas un poquito delante del plano del TV (Z+ 0.02m)
            rt.localPosition = new Vector3(0f, 0f, -0.02f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * WorldScale;

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var cam = Camera.main;
            if (cam == null && Camera.allCameras.Length > 0) cam = Camera.allCameras[0];
            canvas.worldCamera = cam;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            // Fondo negro semitransparente full
            var bg = AddImage(canvasGO.transform, "Background", CBgDark);
            Stretch(bg.rectTransform);

            // ----------------------------------------------------------------
            // TOPBAR (alto 160px)
            // ----------------------------------------------------------------
            var topbar = NewContainer(canvasGO.transform, "TopBar",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(-20, 160));

            // Tile 1: Temp Calentamiento (grande)
            var (tile1, _) = BuildTile(topbar, "TileTempCal", "TEMP. CALENTAMIENTO",
                new Vector2(0, 0), new Vector2(0.20f, 1));
            var heatDisplay = AddText(tile1, "HeatDisplay", "0.0 °C", 48, CCyan, TextAlignmentOptions.Center);
            heatDisplay.fontStyle = FontStyles.Bold;
            Stretch(heatDisplay.rectTransform, new Vector4(8, 8, -8, -30));

            // Tile 2: Secuencia (6 pasos)
            var (tile2, _) = BuildTile(topbar, "TileSeq", "SECUENCIA",
                new Vector2(0.205f, 0), new Vector2(0.45f, 1));
            var stepRow = NewContainer(tile2, "Steps",
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0.5f, 0.5f),
                new Vector2(0, -8), new Vector2(-16, -38));
            var hlg = stepRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            string[] stepNames = { "Start", "Llenado", "Calefactor", "Refrig.", "B.Prod.", "Válido" };
            var dots = new Image[6];
            var lbls = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                var col = NewContainer(stepRow, $"Step{i+1}",
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 2; vlg.childAlignment = TextAnchor.MiddleCenter;
                var dot = AddImage(col, "Dot", CTextDim);
                dot.rectTransform.sizeDelta = new Vector2(16, 16);
                var le = dot.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 16; le.preferredHeight = 16;
                var lbl = AddText(col, "Lbl", stepNames[i], 16, CTextHi, TextAlignmentOptions.Center);
                var le2 = lbl.gameObject.AddComponent<LayoutElement>();
                le2.preferredHeight = 14;
                dots[i] = dot;
                lbls[i] = lbl;
            }

            // Tile 3: Lecturas (6 kv)
            var (tile3, _) = BuildTile(topbar, "TileReads", "LECTURAS",
                new Vector2(0.455f, 0), new Vector2(0.70f, 1));
            var (heatTempT, holdTempT, outTempT, holdTimeT, psiT, pulmonLvlT)
                = BuildKVGrid6(tile3,
                    ("T.Cal", "20.0 C"), ("T.Ret", "20.0 C"),
                    ("T.Sal", "10.0 C"), ("t.Ret", "0.0 s"),
                    ("PSI",   "0"),      ("Pulm.", "80 %"));

            // Tile 4: Válvulas (3 kv) + Trap
            var (tile4, _) = BuildTile(topbar, "TileValves", "VÁLVULAS",
                new Vector2(0.705f, 0), new Vector2(0.85f, 1));
            var (vProdT, vRetT, vDesT, trapT) = BuildKVGrid4(tile4,
                ("Prod.", "CERR"), ("Retor.", "CERR"), ("Desv.", "CERR"), ("Trap", "CERR"));

            // Tile 5: Estado (alarma + CIP)
            var (tile5, _) = BuildTile(topbar, "TileStatus", "ESTADO",
                new Vector2(0.855f, 0), new Vector2(1f, 1));
            var alarmT = AddText(tile5, "AlarmTxt", "OK", 28,
                new Color(0.20f, 0.92f, 0.55f), TextAlignmentOptions.Center);
            alarmT.fontStyle = FontStyles.Bold;
            alarmT.rectTransform.anchorMin = new Vector2(0, 0.5f);
            alarmT.rectTransform.anchorMax = new Vector2(1, 1);
            alarmT.rectTransform.offsetMin = new Vector2(4, -36);
            alarmT.rectTransform.offsetMax = new Vector2(-4, -4);
            var cipT = AddText(tile5, "CipTxt", "N/A", 16, CTextLo, TextAlignmentOptions.Center);
            cipT.rectTransform.anchorMin = new Vector2(0, 0);
            cipT.rectTransform.anchorMax = new Vector2(1, 0.5f);
            cipT.rectTransform.offsetMin = new Vector2(4, 4);
            cipT.rectTransform.offsetMax = new Vector2(-4, -4);

            // ----------------------------------------------------------------
            // PANEL CONTROL (izquierda, ancho 280px, debajo topbar)
            // ----------------------------------------------------------------
            var panel = NewContainer(canvasGO.transform, "PanelControl",
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(280, -180));
            // ajustamos top
            var panelRT = panel;
            panelRT.anchorMin = new Vector2(0, 0); panelRT.anchorMax = new Vector2(0, 1);
            panelRT.pivot = new Vector2(0, 1);
            panelRT.offsetMin = new Vector2(10, 10);
            panelRT.offsetMax = new Vector2(280, -180);
            panelRT.anchoredPosition = new Vector2(10, -180);

            var panelBg = AddImage(panel, "Bg", CTile);
            Stretch(panelBg.rectTransform);
            var panelTitle = AddText(panel, "Title", "CONTROL", 23, CCyan, TextAlignmentOptions.Center);
            panelTitle.fontStyle = FontStyles.Bold;
            panelTitle.rectTransform.anchorMin = new Vector2(0, 1);
            panelTitle.rectTransform.anchorMax = new Vector2(1, 1);
            panelTitle.rectTransform.offsetMin = new Vector2(8, -32);
            panelTitle.rectTransform.offsetMax = new Vector2(-8, -8);

            // ===== BOTÓN GRANDE "ENERGÍA" arriba (full width, destacado) =====
            var energyArea = NewContainer(panel, "EnergyArea",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1),
                new Vector2(0, -36), new Vector2(-16, 0));
            energyArea.sizeDelta = new Vector2(-16, 44);
            var energyBtn = BuildButton(energyArea, "EnergyBtn", "1. ENERGÍA: OFF", 16, out var energyLbl);
            var energyBtnRT = energyBtn.GetComponent<RectTransform>();
            energyBtnRT.anchorMin = Vector2.zero; energyBtnRT.anchorMax = Vector2.one;
            energyBtnRT.offsetMin = new Vector2(8, 0); energyBtnRT.offsetMax = new Vector2(-8, 0);
            // Hacer el botón energía más alto y bold
            var energyLE = energyBtn.GetComponent<LayoutElement>();
            if (energyLE != null) energyLE.preferredHeight = 44;

            // ===== Botones en GRID 2 columnas (más compacto) =====
            // Calculamos cellSize a partir del ancho del panel para asegurar
            // que SIEMPRE entren 2 columnas (panel 280px → cada celda ~120px)
            const float panelWidth = 280f;     // mismo valor que offsetMax.x del panel
            const float gridPadding = 8f;       // a cada lado
            const float gridSpacing = 6f;
            float cellW = (panelWidth - 2 * gridPadding - gridSpacing) * 0.5f - 8f; // ~118
            float cellH = 50f;

            var btnArea = NewContainer(panel, "ButtonsGrid",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1),
                new Vector2(0, -88), new Vector2(-16, 0));
            btnArea.sizeDelta = new Vector2(-16, (cellH + gridSpacing) * 4 + 12);
            var grid = btnArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellW, cellH);
            grid.spacing = new Vector2(gridSpacing, gridSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset((int)gridPadding, (int)gridPadding, 0, 0);

            var startBtn    = BuildButton(btnArea, "StartBtn",    "▶  INICIAR",        13, out _);
            var stopBtn     = BuildButton(btnArea, "StopBtn",     "■  DETENER",        13, out _);
            var modeBtn     = BuildButton(btnArea, "ModeBtn",     "Modo:\nProducción", 11, out var modeLbl);
            var fillBtn     = BuildButton(btnArea, "FillBtn",     "V. Llenado\nOFF",   11, out var fillLbl);
            var hotPumpBtn  = BuildButton(btnArea, "HotPumpBtn",  "Calefactor\nOFF",   11, out var hotPumpLbl);
            var coldPumpBtn = BuildButton(btnArea, "ColdPumpBtn", "Refrigerador\nOFF",11, out var coldPumpLbl);
            var milkPumpBtn = BuildButton(btnArea, "MilkPumpBtn", "B. Producto\nOFF",  11, out var milkPumpLbl);
            var resetBtn    = BuildButton(btnArea, "ResetBtn",    "↻  Reiniciar\nLote",11, out _);

            // Colorear Iniciar / Detener distintivamente
            var startImg = startBtn.GetComponent<Image>();
            if (startImg != null) startImg.color = new Color(0.20f, 0.55f, 0.30f); // verde
            var stopImg = stopBtn.GetComponent<Image>();
            if (stopImg != null) stopImg.color = new Color(0.65f, 0.20f, 0.20f); // rojo

            // ===== Sliders abajo del panel =====
            var sliderArea = NewContainer(panel, "Sliders",
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0),
                new Vector2(0, 8), new Vector2(-16, 110));
            sliderArea.sizeDelta = new Vector2(-16, 110);
            var vlgS = sliderArea.gameObject.AddComponent<VerticalLayoutGroup>();
            vlgS.spacing = 8; vlgS.childForceExpandHeight = false; vlgS.childForceExpandWidth = true;
            // IMPORTANTE: sin childControlWidth el grupo no asigna ancho a las filas y el
            // texto se parte letra por letra en vertical (Setpoint / Caudal).
            vlgS.childControlWidth = true; vlgS.childControlHeight = true;
            vlgS.padding = new RectOffset(8, 8, 4, 4);

            var (targetSlider, targetVal) = BuildSlider(sliderArea, "Target", "Setpoint (L)", 10f, 100f, 50f);
            var (flowSlider, flowVal)     = BuildSlider(sliderArea, "Flow",   "Caudal (L/s)",   0.1f, 2f, 0.5f);

            // ----------------------------------------------------------------
            // PANEL MÉTRICAS DE LOTE (derecha, 280px, sólo números)
            // ----------------------------------------------------------------
            var metrics = NewContainer(canvasGO.transform, "PanelMetrics",
                new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(1, 1),
                new Vector2(-10, -180), new Vector2(0, 0));
            metrics.anchorMin = new Vector2(1, 0); metrics.anchorMax = new Vector2(1, 1);
            metrics.pivot = new Vector2(1, 1);
            metrics.offsetMin = new Vector2(-280, 10);
            metrics.offsetMax = new Vector2(-10, -180);
            metrics.anchoredPosition = new Vector2(-10, -180);

            var metricsBg = AddImage(metrics, "Bg", CTile);
            Stretch(metricsBg.rectTransform);
            var metricsTitle = AddText(metrics, "Title", "MÉTRICAS DE LOTE", 18, CCyan, TextAlignmentOptions.Center);
            metricsTitle.fontStyle = FontStyles.Bold;
            metricsTitle.rectTransform.anchorMin = new Vector2(0, 1);
            metricsTitle.rectTransform.anchorMax = new Vector2(1, 1);
            metricsTitle.rectTransform.offsetMin = new Vector2(8, -28);
            metricsTitle.rectTransform.offsetMax = new Vector2(-8, -8);

            var mFilled = BuildMetricRowFull(metrics, 0, "Procesado",    "0.0 L");
            var mFinal  = BuildMetricRowFull(metrics, 1, "Final",        "0.0 L");
            var mRecirc = BuildMetricRowFull(metrics, 2, "Recirculado",  "0.0 L");
            var mEvap   = BuildMetricRowFull(metrics, 3, "Evaporado",    "0.000 L");
            var mLoss   = BuildMetricRowFull(metrics, 4, "Pérdida",      "0.0 L");
            var mYield  = BuildMetricRowFull(metrics, 5, "Rendimiento",  "-- %");

            // Los tanques visuales y pulmón van en el plant (no acá)
            Image tankInFill = null, tankOutFill = null, pulmonFill = null;
            TMP_Text tankInVolTxt = null, tankOutVolTxt = null;

            // ----------------------------------------------------------------
            // PLANT - DIAGRAMA SVG (centro, todo el espacio entre paneles)
            // ----------------------------------------------------------------
            var plantArea = NewContainer(canvasGO.transform, "PlantArea",
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            plantArea.anchorMin = new Vector2(0, 0); plantArea.anchorMax = new Vector2(1, 1);
            plantArea.pivot = new Vector2(0.5f, 0.5f);
            // Margen reducido para que el plant SE VEA COMPLETO
            plantArea.offsetMin = new Vector2(300, 60);
            plantArea.offsetMax = new Vector2(-300, -170);

            var plantBg = AddImage(plantArea, "Bg", new Color(0.93f, 0.95f, 0.98f, 1f));
            Stretch(plantBg.rectTransform);

            // Construir el SVG del plant dentro de plantArea
            var plantSize = plantArea.rect.size;
            if (plantSize.x <= 0 || plantSize.y <= 0)
            {
                // Si el RectTransform aún no se calculó (frame 0), usar el cálculo manual
                plantSize = new Vector2(CanvasW - 600, CanvasH - 240);
            }
            var plantRefs = PasteurizerSimPlantBuilder.Build(plantArea, plantSize);

            // ----------------------------------------------------------------
            // HINT (al pie, ancho completo entre paneles)
            // ----------------------------------------------------------------
            var hintBg = AddImage(canvasGO.transform, "HintBar", new Color(0, 0, 0, 0.6f));
            hintBg.rectTransform.anchorMin = new Vector2(0, 0);
            hintBg.rectTransform.anchorMax = new Vector2(1, 0);
            hintBg.rectTransform.pivot = new Vector2(0.5f, 0);
            hintBg.rectTransform.offsetMin = new Vector2(380, 10);
            hintBg.rectTransform.offsetMax = new Vector2(-380, 50);
            var hintTxt = AddText(hintBg.transform, "HintTxt", "Encender Energía y luego Start.",
                21, CTextHi, TextAlignmentOptions.Center);
            Stretch(hintTxt.rectTransform, new Vector4(12, 4, -12, -4));

            // ----------------------------------------------------------------
            // CONECTAR REFERENCIAS DEL DASHBOARD
            // ----------------------------------------------------------------
            var dash = canvasGO.GetComponent<PasteurizerSimDashboard>();
            dash.engine = engine;
            dash.heatDisplay = heatDisplay;
            dash.stepDots = dots;
            dash.stepLabels = lbls;
            dash.heatTemp = heatTempT;
            dash.holdTemp = holdTempT;
            dash.outTemp = outTempT;
            dash.holdTime = holdTimeT;
            dash.psi = psiT;
            dash.pulmonLvl = pulmonLvlT;
            dash.vProdTxt = vProdT;
            dash.vRetTxt = vRetT;
            dash.vDesTxt = vDesT;
            dash.trapStateTxt = trapT;
            dash.alarmTxt = alarmT;
            dash.cipTxt = cipT;
            dash.energyBtn = energyBtn; dash.energyLbl = energyLbl;
            dash.startBtn = startBtn;
            dash.stopBtn = stopBtn;
            dash.modeBtn = modeBtn; dash.modeLbl = modeLbl;
            dash.fillBtn = fillBtn; dash.fillLbl = fillLbl;
            dash.hotPumpBtn = hotPumpBtn; dash.hotPumpLbl = hotPumpLbl;
            dash.coldPumpBtn = coldPumpBtn; dash.coldPumpLbl = coldPumpLbl;
            dash.milkPumpBtn = milkPumpBtn; dash.milkPumpLbl = milkPumpLbl;
            dash.resetBatchBtn = resetBtn;
            dash.targetSlider = targetSlider; dash.targetVal = targetVal;
            dash.flowSlider = flowSlider; dash.flowVal = flowVal;
            dash.mFilled = mFilled; dash.mFinal = mFinal; dash.mRecirc = mRecirc;
            dash.mEvap = mEvap; dash.mLoss = mLoss; dash.mYield = mYield;
            dash.tankInFill = tankInFill; dash.tankInVolTxt = tankInVolTxt;
            dash.tankOutFill = tankOutFill; dash.tankOutVolTxt = tankOutVolTxt;
            dash.pulmonFill = pulmonFill;
            dash.hintTxt = hintTxt;

            // ----------------------------------------------------------------
            // CONECTAR REFS DEL PLANT
            // ----------------------------------------------------------------
            dash.plantTankInFill  = plantRefs.tankInFill;
            dash.plantTankOutFill = plantRefs.tankOutFill;
            dash.plantPulmonFill  = plantRefs.pulmonFill;
            dash.plantTankInVolTxt  = plantRefs.tankInVolTxt;
            dash.plantTankOutVolTxt = plantRefs.tankOutVolTxt;
            dash.plantRetDisplay  = plantRefs.retDisplay;
            dash.pumpMilkBlade    = plantRefs.pumpMilkBlade;
            dash.pumpHotBlade     = plantRefs.pumpHotBlade;
            dash.flameIcon        = plantRefs.flameIcon;
            dash.refriLed         = plantRefs.refriLed;
            dash.trapDrip         = plantRefs.trapDrip;
            dash.psiNeedle        = plantRefs.psiNeedle;
            dash.psiTextOnGauge   = plantRefs.psiText;
            dash.plantRetFlowOverlay = plantRefs.retFlow;
            dash.pipeInlet        = plantRefs.pipeInlet;
            dash.pipeRawSegments       = plantRefs.pipeRaw;
            dash.pipeToHeatSegments    = plantRefs.pipeToHeat;
            dash.pipeToRetSegments     = plantRefs.pipeToRet;
            dash.pipeFromRetSegments   = plantRefs.pipeFromRet;
            dash.pipeToCoolingSegments = plantRefs.pipeToCooling;
            dash.pipeOutSegments       = plantRefs.pipeOut;
            dash.pipeReturnSegments    = plantRefs.pipeReturn;
            dash.pipeHotSupplySegments = plantRefs.pipeHotSupply;
            dash.pipeHotReturnSegments = plantRefs.pipeHotReturn;
            dash.pipeHotBackSegments   = plantRefs.pipeHotBack;
            dash.pipeColdSegments      = plantRefs.pipeCold;
            dash.pipeColdReturnSegments= plantRefs.pipeColdReturn;
            dash.pipeMakeupSegments    = plantRefs.pipeMakeup;
            dash.pipePulmonOutSegments = plantRefs.pipePulmonOut;
            dash.valveProdHandle = plantRefs.valveProdHandle;
            dash.valveRetHandle  = plantRefs.valveRetHandle;
            dash.valveDesHandle  = plantRefs.valveDesHandle;
            dash.valveFillHandle = plantRefs.valveFillHandle;
            dash.valveProdBody = plantRefs.valveProdBody;
            dash.valveRetBody  = plantRefs.valveRetBody;
            dash.valveDesBody  = plantRefs.valveDesBody;
            dash.valveFillBody = plantRefs.valveFillBody;

            // ----------------------------------------------------------------
            // VENTANA EMERGENTE INICIAL (botón EMPEZAR) — arranca la guía por voz
            // ----------------------------------------------------------------
            BuildStartOverlay(canvasGO.transform);

            EditorSceneManager.MarkSceneDirty(tv.scene);
            Selection.activeObject = canvasGO;

            EditorUtility.DisplayDialog("Pasteurizador HTST",
                "✅ DASHBOARD RECONSTRUIDO con grid 2 columnas + español\n\n" +
                "Panel CONTROL ahora tiene:\n" +
                "  • Botón ENERGÍA full width arriba\n" +
                "  • 8 botones en grid 2 col x 4 fila:\n" +
                "      [▶ INICIAR]    [■ DETENER]\n" +
                "      [Modo]         [V. Llenado]\n" +
                "      [Calefactor]   [Refrigerador]\n" +
                "      [B. Producto]  [↻ Reiniciar Lote]\n" +
                "  • 2 sliders abajo (Setpoint / Caudal)\n\n" +
                "Entrá a Play y verificá que los botones estén\n" +
                "en 2 columnas y digan 'INICIAR' (no 'Start').",
                "OK");

            Debug.Log("<color=lime>[SimDashboard]</color> ✅ Reconstrucción COMPLETA. " +
                      "Botones en grid 2 col. Entrá a Play y deberías ver 'INICIAR' / 'DETENER'.");
        }

        // ====================================================================
        //  RECONSTRUCCIÓN TOTAL (mismo método pero con menú más visible)
        // ====================================================================
        [MenuItem("Viroo/Pasteurizador HTST/14. ⟳ FORZAR Rebuild Dashboard (si paso 13 no aplicó)", priority = 131)]
        public static void ForceRebuild()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Pasteurizador HTST",
                    "⚠️ Estás en PLAY MODE.\n\nSalí de Play y volvé a intentarlo.",
                    "OK");
                return;
            }

            // 1) Buscar TODOS los _SimDashboard en escena (por si quedaron huérfanos)
            var allCanvases = Object.FindObjectsByType<PasteurizerSimDashboard>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int killed = 0;
            foreach (var d in allCanvases)
            {
                Debug.Log($"<color=yellow>[ForceRebuild]</color> Borrando dashboard previo: {d.gameObject.name} (parent: {d.transform.parent?.name})");
                Object.DestroyImmediate(d.gameObject);
                killed++;
            }

            // 2) Buscar engines huérfanos
            var allEngines = Object.FindObjectsByType<PasteurizerSimEngine>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var e in allEngines)
            {
                // Solo borrar si el GameObject ya NO existe en TV
                if (e.gameObject.name != TvName)
                {
                    Debug.Log($"<color=yellow>[ForceRebuild]</color> Borrando engine huérfano: {e.gameObject.name}");
                    Object.DestroyImmediate(e);
                }
            }

            Debug.Log($"<color=cyan>[ForceRebuild]</color> Limpieza: {killed} dashboards eliminados. Reconstruyendo...");

            // 3) Re-ejecutar Build normal
            Build();
        }

        // ====================================================================
        //  Builders auxiliares
        // ====================================================================
        private static RectTransform NewContainer(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        private static (Transform tile, Image border) BuildTile(Transform parent, string name, string title,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var tile = NewContainer(parent, name, anchorMin, anchorMax,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tile.offsetMin = new Vector2(4, 4); tile.offsetMax = new Vector2(-4, -4);

            var border = AddImage(tile, "Border", CTileEdge);
            Stretch(border.rectTransform);
            var bg = AddImage(tile, "Bg", CTile);
            var bgRT = bg.rectTransform;
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = new Vector2(2, 2); bgRT.offsetMax = new Vector2(-2, -2);

            var titleTxt = AddText(tile, "Title", title, 16, CTextLo, TextAlignmentOptions.Center);
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.rectTransform.anchorMin = new Vector2(0, 1);
            titleTxt.rectTransform.anchorMax = new Vector2(1, 1);
            titleTxt.rectTransform.offsetMin = new Vector2(4, -22);
            titleTxt.rectTransform.offsetMax = new Vector2(-4, -4);
            return (tile, border);
        }

        private static (TMP_Text, TMP_Text, TMP_Text, TMP_Text, TMP_Text, TMP_Text)
            BuildKVGrid6(Transform parent, params (string key, string val)[] entries)
        {
            var grid = NewContainer(parent, "KVGrid",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            grid.offsetMin = new Vector2(8, 8); grid.offsetMax = new Vector2(-8, -28);
            var glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(120, 40);
            glg.spacing = new Vector2(8, 4);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;

            var results = new TMP_Text[6];
            for (int i = 0; i < entries.Length; i++)
            {
                var (key, val) = entries[i];
                var cell = NewContainer(grid, $"Cell_{i}",
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                var k = AddText(cell, "K", key, 14, CTextDim, TextAlignmentOptions.Left);
                k.rectTransform.anchorMin = new Vector2(0, 0); k.rectTransform.anchorMax = new Vector2(1, 0.5f);
                k.rectTransform.offsetMin = new Vector2(2, 2); k.rectTransform.offsetMax = new Vector2(-2, -2);
                var v = AddText(cell, "V", val, 23, CTextHi, TextAlignmentOptions.Right);
                v.fontStyle = FontStyles.Bold;
                v.rectTransform.anchorMin = new Vector2(0, 0.5f); v.rectTransform.anchorMax = new Vector2(1, 1);
                v.rectTransform.offsetMin = new Vector2(2, 2); v.rectTransform.offsetMax = new Vector2(-4, -2);
                results[i] = v;
            }
            return (results[0], results[1], results[2], results[3], results[4], results[5]);
        }

        private static (TMP_Text, TMP_Text, TMP_Text, TMP_Text)
            BuildKVGrid4(Transform parent, params (string key, string val)[] entries)
        {
            var grid = NewContainer(parent, "KVGrid",
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            grid.offsetMin = new Vector2(8, 8); grid.offsetMax = new Vector2(-8, -28);
            var glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80, 36);
            glg.spacing = new Vector2(6, 4);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            var results = new TMP_Text[4];
            for (int i = 0; i < entries.Length; i++)
            {
                var (key, val) = entries[i];
                var cell = NewContainer(grid, $"Cell_{i}",
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero);
                var k = AddText(cell, "K", key, 13, CTextDim, TextAlignmentOptions.Left);
                k.rectTransform.anchorMin = new Vector2(0, 0); k.rectTransform.anchorMax = new Vector2(1, 0.5f);
                k.rectTransform.offsetMin = new Vector2(2, 2); k.rectTransform.offsetMax = new Vector2(-2, -2);
                var v = AddText(cell, "V", val, 18, CTextHi, TextAlignmentOptions.Right);
                v.fontStyle = FontStyles.Bold;
                v.rectTransform.anchorMin = new Vector2(0, 0.5f); v.rectTransform.anchorMax = new Vector2(1, 1);
                v.rectTransform.offsetMin = new Vector2(2, 2); v.rectTransform.offsetMax = new Vector2(-4, -2);
                results[i] = v;
            }
            return (results[0], results[1], results[2], results[3]);
        }

        /// Ventana emergente inicial con el botón EMPEZAR (arranca la narración por voz).
        private static void BuildStartOverlay(Transform canvas)
        {
            var overlay = NewContainer(canvas, "_StartOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // velo que cubre el tablero
            var veil = AddImage(overlay, "Veil", new Color(0.02f, 0.04f, 0.07f, 0.94f));
            Stretch(veil.rectTransform);

            // caja central
            var box = NewContainer(overlay, "Box",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000, 420));
            var bg = AddImage(box, "Bg", CTile);
            Stretch(bg.rectTransform);

            var title = AddText(box, "Title", "SIMULADOR DE PASTEURIZACIÓN HTST", 46, CCyan, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.anchorMin = new Vector2(0, 1);
            title.rectTransform.anchorMax = new Vector2(1, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -40);
            title.rectTransform.sizeDelta = new Vector2(-60, 80);

            var sub = AddText(box, "Sub",
                "Pulsa EMPEZAR y sigue las indicaciones de voz para operar la planta.",
                26, CTextLo, TextAlignmentOptions.Center);
            sub.rectTransform.anchorMin = new Vector2(0, 1);
            sub.rectTransform.anchorMax = new Vector2(1, 1);
            sub.rectTransform.pivot = new Vector2(0.5f, 1);
            sub.rectTransform.anchoredPosition = new Vector2(0, -140);
            sub.rectTransform.sizeDelta = new Vector2(-120, 90);

            var btn = BuildButton(box, "BtnEmpezar", "EMPEZAR", 34, out _);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(0.5f, 0);
            brt.anchorMax = new Vector2(0.5f, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(0, 55);
            brt.sizeDelta = new Vector2(400, 95);
            var bimg = btn.GetComponent<Image>();
            if (bimg != null) bimg.color = new Color(0.16f, 0.55f, 0.30f); // verde

            var so = overlay.gameObject.AddComponent<PasteurizerStartOverlay>();
            so.panel = overlay.gameObject;
            so.startButton = btn;
            so.guide = Object.FindFirstObjectByType<PasteurizerVoiceGuide>();
        }

        private static Button BuildButton(Transform parent, string name, string text, int fontSize, out TMP_Text labelOut)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            // LayoutElement solo si NO está en un GridLayoutGroup (el grid ignora LE y usa cellSize)
            // Mantenemos preferredHeight para que VerticalLayout (sliders/energyArea) lo respete.
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44;
            le.flexibleWidth = 1f;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.20f, 0.25f, 1f);
            var lbl = AddText(go.transform, "Label", text, fontSize, CTextHi, TextAlignmentOptions.Center);
            lbl.fontStyle = FontStyles.Bold;
            lbl.enableWordWrapping = true;
            Stretch(lbl.rectTransform, new Vector4(4, 2, -4, -2));
            labelOut = lbl;
            return go.GetComponent<Button>();
        }

        private static (Slider, TMP_Text) BuildSlider(Transform parent, string name, string title,
            float minVal, float maxVal, float defVal)
        {
            var container = NewContainer(parent, name,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(0, 38));
            var leC = container.gameObject.AddComponent<LayoutElement>();
            leC.preferredHeight = 38;

            // Title
            var t = AddText(container, "T", title, 14, CTextDim, TextAlignmentOptions.Left);
            t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(0.7f, 1);
            t.rectTransform.pivot = new Vector2(0, 1);
            t.rectTransform.offsetMin = new Vector2(0, -14); t.rectTransform.offsetMax = new Vector2(0, 0);
            // Value display
            var v = AddText(container, "V", defVal.ToString("F1"), 17, CCyan, TextAlignmentOptions.Right);
            v.fontStyle = FontStyles.Bold;
            v.rectTransform.anchorMin = new Vector2(0.7f, 1); v.rectTransform.anchorMax = new Vector2(1, 1);
            v.rectTransform.pivot = new Vector2(1, 1);
            v.rectTransform.offsetMin = new Vector2(-2, -16); v.rectTransform.offsetMax = new Vector2(0, 0);

            // Slider
            var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(container, false);
            var sRT = (RectTransform)sliderGO.transform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0);
            sRT.pivot = new Vector2(0.5f, 0);
            sRT.offsetMin = new Vector2(0, 4); sRT.offsetMax = new Vector2(0, 20);
            var slider = sliderGO.GetComponent<Slider>();
            slider.minValue = minVal; slider.maxValue = maxVal; slider.value = defVal;

            // Background
            var sBg = AddImage(sliderGO.transform, "Background", new Color(0.10f, 0.12f, 0.14f, 1f));
            Stretch(sBg.rectTransform);

            // Fill area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGO.transform, false);
            var faRT = (RectTransform)fillArea.transform;
            faRT.anchorMin = new Vector2(0, 0.25f); faRT.anchorMax = new Vector2(1, 0.75f);
            faRT.offsetMin = new Vector2(8, 0); faRT.offsetMax = new Vector2(-8, 0);
            var fill = AddImage(fillArea.transform, "Fill", CCyan);
            Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.direction = Slider.Direction.LeftToRight;

            // Handle
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGO.transform, false);
            var haRT = (RectTransform)handleArea.transform;
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(8, 0); haRT.offsetMax = new Vector2(-8, 0);
            var handle = AddImage(handleArea.transform, "Handle", Color.white);
            handle.rectTransform.sizeDelta = new Vector2(14, 22);
            slider.handleRect = handle.rectTransform;

            return (slider, v);
        }

        private static TMP_Text BuildMetricRow(Transform parent, int index, string key, string val)
        {
            float topY = -36f - index * 22f;
            var row = NewContainer(parent, $"Row_{index}",
                new Vector2(0, 1), new Vector2(0.65f, 1), new Vector2(0, 1),
                new Vector2(8, topY), new Vector2(-8, 20));
            row.sizeDelta = new Vector2(-16, 20);
            var k = AddText(row, "K", key, 16, CTextDim, TextAlignmentOptions.Left);
            k.rectTransform.anchorMin = Vector2.zero; k.rectTransform.anchorMax = new Vector2(0.55f, 1);
            k.rectTransform.offsetMin = Vector2.zero; k.rectTransform.offsetMax = Vector2.zero;
            var v = AddText(row, "V", val, 17, CTextHi, TextAlignmentOptions.Right);
            v.fontStyle = FontStyles.Bold;
            v.rectTransform.anchorMin = new Vector2(0.55f, 0); v.rectTransform.anchorMax = Vector2.one;
            v.rectTransform.offsetMin = Vector2.zero; v.rectTransform.offsetMax = Vector2.zero;
            return v;
        }

        /// Versión a ancho completo del panel (sin tanques al lado).
        /// La 'value' del rendimiento está alineada a la derecha de toda la fila.
        private static TMP_Text BuildMetricRowFull(Transform parent, int index, string key, string val)
        {
            float topY = -36f - index * 28f;
            var row = NewContainer(parent, $"RowF_{index}",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, topY), new Vector2(-16, 26));
            row.sizeDelta = new Vector2(-16, 26);
            // Subbg sutil
            var sub = AddImage(row, "Sub", new Color(0.10f, 0.13f, 0.17f, 0.4f));
            Stretch(sub.rectTransform);

            var k = AddText(row, "K", key, 17, CTextDim, TextAlignmentOptions.Left);
            k.rectTransform.anchorMin = Vector2.zero; k.rectTransform.anchorMax = new Vector2(0.55f, 1);
            k.rectTransform.offsetMin = new Vector2(10, 0); k.rectTransform.offsetMax = new Vector2(-4, 0);

            var v = AddText(row, "V", val, 21, CCyan, TextAlignmentOptions.Right);
            v.fontStyle = FontStyles.Bold;
            v.rectTransform.anchorMin = new Vector2(0.45f, 0); v.rectTransform.anchorMax = Vector2.one;
            v.rectTransform.offsetMin = new Vector2(0, 0); v.rectTransform.offsetMax = new Vector2(-10, 0);
            return v;
        }

        private static (Image fill, TMP_Text vol) BuildTank(Transform parent, string name, string title, int slot)
        {
            // Tanques en columna derecha del panel métricas
            float xMin = 0.66f + (slot - 6) * 0.12f;
            float xMax = xMin + 0.10f;

            var container = NewContainer(parent, "Tank_" + name,
                new Vector2(xMin, 0), new Vector2(xMax, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            container.offsetMin = new Vector2(0, 12); container.offsetMax = new Vector2(0, -36);

            var bg = AddImage(container, "Bg", new Color(0.08f, 0.10f, 0.13f, 1f));
            Stretch(bg.rectTransform);
            var t = AddText(container, "T", title, 13, CTextDim, TextAlignmentOptions.Center);
            t.rectTransform.anchorMin = new Vector2(0, 0); t.rectTransform.anchorMax = new Vector2(1, 0);
            t.rectTransform.offsetMin = new Vector2(0, -16); t.rectTransform.offsetMax = new Vector2(0, 0);

            var v = AddText(container, "V", "0 L", 14, CTextHi, TextAlignmentOptions.Center);
            v.rectTransform.anchorMin = new Vector2(0, 1); v.rectTransform.anchorMax = new Vector2(1, 1);
            v.rectTransform.offsetMin = new Vector2(0, -16); v.rectTransform.offsetMax = new Vector2(0, 0);

            // Fill rectangular vertical
            var fill = AddImage(container, "Fill", CCyan);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;
            fill.rectTransform.anchorMin = new Vector2(0.15f, 0.07f);
            fill.rectTransform.anchorMax = new Vector2(0.85f, 0.85f);
            fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;

            return (fill, v);
        }

        // ---- generic helpers ----
        private static Image AddImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text AddText(Transform parent, string name, string text,
            int size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt, Vector4 padding = default)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding.x, padding.y);
            rt.offsetMax = new Vector2(padding.z, padding.w);
        }
    }
}
#endif
