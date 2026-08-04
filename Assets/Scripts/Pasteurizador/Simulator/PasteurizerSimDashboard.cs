using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViroLab.Pasteurizador.Simulator
{
    /// Conecta el motor de simulación con un Canvas UI.
    /// Cada Update lee del engine y actualiza textos / colores / sliders / estados.
    /// El builder Editor (PasteurizerSimDashboardBuilder) crea el Canvas y
    /// asigna automáticamente las referencias de este componente.
    [RequireComponent(typeof(Canvas))]
    public class PasteurizerSimDashboard : MonoBehaviour
    {
        [Header("Motor")]
        public PasteurizerSimEngine engine;

        [Header("Topbar - Temp Calentamiento (grande)")]
        public TMP_Text heatDisplay;

        [Header("Secuencia (6 pasos)")]
        public Image[] stepDots = new Image[6];
        public TMP_Text[] stepLabels = new TMP_Text[6];

        [Header("Lecturas")]
        public TMP_Text heatTemp;
        public TMP_Text holdTemp;
        public TMP_Text outTemp;
        public TMP_Text holdTime;
        public TMP_Text psi;
        public TMP_Text pulmonLvl;

        [Header("Válvulas")]
        public TMP_Text vProdTxt;
        public TMP_Text vRetTxt;
        public TMP_Text vDesTxt;
        public TMP_Text trapStateTxt;

        [Header("Estado grande")]
        public TMP_Text alarmTxt;
        public TMP_Text cipTxt;

        [Header("Botones de control")]
        public Button energyBtn;
        public Button startBtn;
        public Button stopBtn;
        public Button modeBtn;
        public Button fillBtn;
        public Button hotPumpBtn;
        public Button coldPumpBtn;
        public Button milkPumpBtn;
        public Button resetBatchBtn;
        [Tooltip("Ventana EMPEZAR. Si queda vacio se busca en la escena al reiniciar.")]
        public PasteurizerStartOverlay startOverlay;

        [Header("Labels de botones (refrescamos texto ON/OFF)")]
        public TMP_Text energyLbl;
        public TMP_Text modeLbl;
        public TMP_Text fillLbl;
        public TMP_Text hotPumpLbl;
        public TMP_Text coldPumpLbl;
        public TMP_Text milkPumpLbl;

        [Header("Sliders")]
        public Slider targetSlider;
        public TMP_Text targetVal;
        public Slider flowSlider;
        public TMP_Text flowVal;

        [Header("Métricas de lote")]
        public TMP_Text mFilled;
        public TMP_Text mFinal;
        public TMP_Text mRecirc;
        public TMP_Text mEvap;
        public TMP_Text mLoss;
        public TMP_Text mYield;

        [Header("Niveles de tanques (fills 0..1)")]
        public Image tankInFill;
        public Image tankOutFill;
        public TMP_Text tankInVolTxt;
        public TMP_Text tankOutVolTxt;
        public Image pulmonFill;

        [Header("Hint al pie")]
        public TMP_Text hintTxt;

        // ====================================================================
        //  PLANT (diagrama SVG portado a UGUI)
        // ====================================================================
        [Header("Plant - Tanques")]
        public Image plantTankInFill;
        public Image plantTankOutFill;
        public Image plantPulmonFill;
        public TMP_Text plantTankInVolTxt;
        public TMP_Text plantTankOutVolTxt;
        public TMP_Text plantRetDisplay;

        [Header("Plant - Bombas (aspas que giran)")]
        public RectTransform pumpMilkBlade;
        public RectTransform pumpHotBlade;
        public float pumpRpsActive = 1.2f;  // vueltas/seg cuando ON

        [Header("Plant - Tuberías (cambian color/alpha al fluir)")]
        public Image pipeInlet;
        public Image[] pipeRawSegments;       // 1+ rects que componen pipeRaw
        public Image[] pipeToHeatSegments;
        public Image[] pipeToRetSegments;
        public Image[] pipeFromRetSegments;
        public Image[] pipeToCoolingSegments;
        public Image[] pipeOutSegments;
        public Image[] pipeReturnSegments;
        public Image[] pipeHotSupplySegments;
        public Image[] pipeHotReturnSegments;
        public Image[] pipeHotBackSegments;
        public Image[] pipeColdSegments;
        public Image[] pipeColdReturnSegments;
        public Image[] pipeMakeupSegments;
        public Image[] pipePulmonOutSegments;
        public Image plantRetFlowOverlay;     // serpentina

        [Header("Plant - Válvulas (handle rota 45° al abrir)")]
        public RectTransform valveProdHandle;
        public RectTransform valveRetHandle;
        public RectTransform valveDesHandle;
        public RectTransform valveFillHandle;
        public Image valveProdBody;
        public Image valveRetBody;
        public Image valveDesBody;
        public Image valveFillBody;

        [Header("Plant - Indicadores")]
        public Image flameIcon;
        public Image refriLed;
        public Image trapDrip;
        public RectTransform psiNeedle;
        public TMP_Text psiTextOnGauge;

        [Header("Plant - Colores tuberías")]
        public Color pipeOffColor    = new Color(0.30f, 0.34f, 0.38f, 1f);
        public Color pipeMilkColor   = new Color(0.96f, 0.95f, 0.86f, 1f); // crema
        public Color pipeMilkWarm    = new Color(1.00f, 0.72f, 0.48f, 1f); // naranja claro
        public Color pipeMilkHot     = new Color(1.00f, 0.48f, 0.30f, 1f); // naranja fuerte
        public Color pipeMilkCold    = new Color(0.40f, 0.75f, 1.00f, 1f); // azul claro
        public Color pipeWaterHot    = new Color(0.92f, 0.30f, 0.30f, 1f); // rojo
        public Color pipeWaterCold   = new Color(0.30f, 0.55f, 1.00f, 1f); // azul

        // Estado interno para animaciones
        private float _pumpMilkAngle = 0f;
        private float _pumpHotAngle  = 0f;

        [Header("Paleta")]
        public Color colorOk     = new Color(0.20f, 0.92f, 0.55f);
        public Color colorWarn   = new Color(1.00f, 0.75f, 0.10f);
        public Color colorAlarm  = new Color(1.00f, 0.30f, 0.30f);
        public Color colorOff    = new Color(0.45f, 0.50f, 0.55f);
        public Color colorActive = new Color(0.00f, 0.88f, 1.00f);
        public Color colorBtnOn  = new Color(0.00f, 0.55f, 0.85f);
        public Color colorBtnOff = new Color(0.16f, 0.20f, 0.25f);
        public Color colorBtnDis = new Color(0.10f, 0.12f, 0.14f);

        private void Awake()
        {
            if (engine == null) engine = GetComponentInParent<PasteurizerSimEngine>();
            if (engine == null) engine = FindFirstObjectByType<PasteurizerSimEngine>();
            BindButtons();
            BindSliders();
        }

        private void BindButtons()
        {
            if (energyBtn   != null) energyBtn.onClick.AddListener(engine.ToggleEnergy);
            if (startBtn    != null) startBtn.onClick.AddListener(engine.StartPlant);
            if (stopBtn     != null) stopBtn.onClick.AddListener(engine.StopPlant);
            if (modeBtn     != null) modeBtn.onClick.AddListener(engine.ToggleMode);
            if (fillBtn     != null) fillBtn.onClick.AddListener(engine.ToggleFill);
            if (hotPumpBtn  != null) hotPumpBtn.onClick.AddListener(engine.ToggleHotPump);
            if (coldPumpBtn != null) coldPumpBtn.onClick.AddListener(engine.ToggleColdPump);
            if (milkPumpBtn != null) milkPumpBtn.onClick.AddListener(engine.ToggleMilkPump);
            if (resetBatchBtn != null)
            {
                resetBatchBtn.onClick.AddListener(engine.ResetBatch);
                // Ademas del motor, el reinicio devuelve la INTERFAZ a su estado de
                // entrada. PasteurizerStartOverlay.ShowAgain existia desde el
                // principio pero nadie lo llamaba nunca: esta es la conexion que
                // faltaba para que el simulador quede listo para el siguiente
                // estudiante.
                resetBatchBtn.onClick.AddListener(OnResetBatchPressed);
            }
        }

        /// Devuelve la ventana EMPEZAR y, si esta configurado, reinicia la narracion.
        private void OnResetBatchPressed()
        {
            if (startOverlay == null)
                startOverlay = FindFirstObjectByType<PasteurizerStartOverlay>(FindObjectsInactive.Include);
            if (startOverlay != null && startOverlay.showAgainOnReset)
                startOverlay.ShowAgain();
        }

        private void BindSliders()
        {
            if (targetSlider != null)
            {
                targetSlider.minValue = 10f;
                targetSlider.maxValue = PasteurizerSimEngine.TANK_IN_MAX;
                targetSlider.value = engine.targetFillVol;
                targetSlider.onValueChanged.AddListener(v => engine.targetFillVol = v);
            }
            if (flowSlider != null)
            {
                flowSlider.minValue = 0.1f;
                flowSlider.maxValue = 2.0f;
                flowSlider.value = engine.flowRate;
                flowSlider.onValueChanged.AddListener(v => engine.flowRate = v);
            }
        }

        private void Update()
        {
            if (engine == null) return;
            RefreshReadings();
            RefreshSteps();
            RefreshValves();
            RefreshButtons();
            RefreshSliders();
            RefreshTanks();
            RefreshBatchMetrics();
            RefreshHints();
            RefreshPlant();
        }

        // ====================================================================
        //  Plant animation (replica el SVG del visor web)
        // ====================================================================
        private void RefreshPlant()
        {
            // Tanques del plant (mismo data que los del panel métricas)
            if (plantTankInFill != null)
                plantTankInFill.fillAmount = Mathf.Clamp01(engine.tankInVol / PasteurizerSimEngine.TANK_IN_MAX);
            if (plantTankOutFill != null)
                plantTankOutFill.fillAmount = Mathf.Clamp01(engine.tankOutVol / PasteurizerSimEngine.TANK_OUT_MAX);
            if (plantPulmonFill != null)
            {
                plantPulmonFill.fillAmount = Mathf.Clamp01(engine.pulmonLevel);
                plantPulmonFill.color = engine.pulmonLevel < PasteurizerSimEngine.PULMON_OPTIMAL
                    ? new Color(0.62f, 0.83f, 1f) : new Color(0.77f, 0.40f, 1f);
            }
            if (plantTankInVolTxt  != null) plantTankInVolTxt.text  = $"{engine.tankInVol:F1} L";
            if (plantTankOutVolTxt != null) plantTankOutVolTxt.text = $"{engine.tankOutVol:F1} L";
            if (plantRetDisplay    != null) plantRetDisplay.text    = engine.tempHold.ToString("F0");

            // Bombas (aspas giran cuando ON)
            float dt = Time.deltaTime;
            if (engine.pumpMilkOn)
            {
                _pumpMilkAngle = (_pumpMilkAngle + pumpRpsActive * 360f * dt) % 360f;
                if (pumpMilkBlade != null)
                    pumpMilkBlade.localEulerAngles = new Vector3(0, 0, -_pumpMilkAngle);
            }
            if (engine.pumpHotOn)
            {
                _pumpHotAngle = (_pumpHotAngle + pumpRpsActive * 360f * dt) % 360f;
                if (pumpHotBlade != null)
                    pumpHotBlade.localEulerAngles = new Vector3(0, 0, -_pumpHotAngle);
            }

            // Flama caldera
            if (flameIcon != null)
            {
                bool fireOn = engine.pumpHotOn && engine.energyOn && engine.running;
                var c = flameIcon.color;
                c.a = fireOn ? Mathf.Lerp(0.7f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f)) : 0.1f;
                flameIcon.color = c;
            }

            // LED refrigerador
            if (refriLed != null)
                refriLed.color = engine.pumpColdOn
                    ? new Color(0.30f, 0.85f, 1f) : new Color(0.22f, 0.26f, 0.32f);

            // Trampa vapor drip
            if (trapDrip != null)
            {
                var c = trapDrip.color;
                c.a = engine.trapOpen ? Mathf.Lerp(0.3f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f)) : 0f;
                trapDrip.color = c;
            }

            // Manómetro
            if (psiNeedle != null)
            {
                float frac = Mathf.Clamp01(engine.steamPsi / PasteurizerSimEngine.STEAM_PSI_MAX);
                float angle = Mathf.Lerp(120f, -120f, frac);  // -120 a +120 grados, invertido
                psiNeedle.localEulerAngles = new Vector3(0, 0, angle);
            }
            if (psiTextOnGauge != null) psiTextOnGauge.text = engine.steamPsi.ToString("F0");

            // Tuberías (color según flujo y temperatura)
            bool milkRunning = engine.pumpMilkOn && engine.energyOn && engine.running;
            bool valid = engine.vProd;

            SetPipe(pipeInlet != null ? new[] { pipeInlet } : null, engine.fillOn, pipeMilkColor);
            SetPipe(pipeRawSegments, milkRunning, pipeMilkColor);

            Color toHeatColor = pipeMilkWarm;
            if (engine.tempHeat >= PasteurizerSimEngine.SP_HEAT - 5f) toHeatColor = pipeMilkHot;
            else if (!engine.pumpHotOn) toHeatColor = pipeMilkColor;
            SetPipe(pipeToHeatSegments, milkRunning, toHeatColor);

            SetPipe(pipeToRetSegments,   milkRunning && engine.tempHeat > 60f, pipeMilkHot);
            SetPipe(pipeFromRetSegments, milkRunning && engine.tempHeat > 60f, pipeMilkHot);
            SetPipe(pipeToCoolingSegments, milkRunning && valid, pipeMilkWarm);
            SetPipe(pipeOutSegments,        milkRunning && valid, pipeMilkCold);
            SetPipe(pipeReturnSegments,     milkRunning && engine.vRet, pipeMilkWarm);

            SetPipe(pipeHotSupplySegments, engine.pumpHotOn, pipeWaterHot);
            SetPipe(pipeHotReturnSegments, engine.pumpHotOn, pipeWaterHot);
            SetPipe(pipeHotBackSegments,   engine.pumpHotOn, pipeWaterHot);

            SetPipe(pipeMakeupSegments,    engine.makeupOn, pipeWaterCold);
            SetPipe(pipePulmonOutSegments, engine.pumpHotOn && engine.pulmonLevel > 0.05f, pipeWaterCold);
            SetPipe(pipeColdSegments,        engine.pumpColdOn, pipeWaterCold);
            SetPipe(pipeColdReturnSegments,  engine.pumpColdOn, pipeWaterCold);

            // Serpentina holding (resalta cuando hay flujo caliente)
            if (plantRetFlowOverlay != null)
            {
                var c = plantRetFlowOverlay.color;
                c.a = (milkRunning && engine.tempHeat > 60f) ? 1f : 0f;
                plantRetFlowOverlay.color = c;
            }

            // Válvulas: rota el handle 45° cuando se "abre"
            SetValveHandle(valveProdHandle, valveProdBody, engine.vProd,  engine.pumpMilkOn);
            SetValveHandle(valveRetHandle,  valveRetBody,  engine.vRet,   engine.pumpMilkOn);
            SetValveHandle(valveDesHandle,  valveDesBody,  engine.vDes,   engine.pumpMilkOn);
            SetValveHandle(valveFillHandle, valveFillBody, engine.fillOn, engine.energyOn);
        }

        private void SetPipe(Image[] segments, bool on, Color flowColor)
        {
            if (segments == null) return;
            var c = on ? flowColor : pipeOffColor;
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] != null) segments[i].color = c;
        }

        private void SetValveHandle(RectTransform handle, Image body, bool open, bool used)
        {
            if (handle != null)
                handle.localEulerAngles = new Vector3(0, 0, (used && open) ? 45f : 0f);
            if (body != null)
                body.color = used
                    ? (open ? new Color(0.30f, 0.85f, 0.40f) : new Color(0.85f, 0.30f, 0.30f))
                    : new Color(0.78f, 0.83f, 0.88f);
        }

        // ---- updaters ----

        private void RefreshReadings()
        {
            if (heatDisplay != null) heatDisplay.text = $"{engine.tempHeat:F1} °C";
            if (heatTemp    != null) heatTemp.text    = $"{engine.tempHeat:F1} C";
            if (holdTemp    != null) holdTemp.text    = $"{engine.tempHold:F1} C";
            if (outTemp     != null) outTemp.text     = $"{engine.tempOut:F1} C";
            if (holdTime    != null) holdTime.text    = $"{engine.holdTimer:F1} s";
            if (psi         != null) psi.text         = $"{engine.steamPsi:F1}";
            if (pulmonLvl   != null) pulmonLvl.text   = $"{engine.pulmonLevel * 100f:F0} %";
        }

        private void RefreshSteps()
        {
            bool s1 = engine.energyOn && engine.running;
            bool s2 = engine.tankInVol >= PasteurizerSimEngine.TANK_MIN_VOL;
            bool s3 = engine.pumpHotOn && engine.tempHeat >= PasteurizerSimEngine.T_PUMP_READY;
            bool s4 = engine.pumpColdOn;
            bool s5 = engine.pumpMilkOn;
            bool s6 = engine.vProd;
            SetStep(0, s1 ? StepState.Done : StepState.Active);
            SetStep(1, !s1 ? StepState.Idle : (s2 ? StepState.Done : StepState.Active));
            SetStep(2, !s2 ? StepState.Idle : (s3 ? StepState.Done : StepState.Active));
            SetStep(3, !s3 ? StepState.Idle : (s4 ? StepState.Done : StepState.Active));
            SetStep(4, !s4 ? StepState.Idle : (s5 ? StepState.Done : StepState.Active));
            SetStep(5, !s5 ? StepState.Idle : (s6 ? StepState.Done : StepState.Active));
        }

        private enum StepState { Idle, Active, Done }
        private void SetStep(int i, StepState st)
        {
            if (i < 0 || i >= stepDots.Length) return;
            if (stepDots[i] != null)
            {
                stepDots[i].color = st == StepState.Done   ? colorOk
                                  : st == StepState.Active ? colorActive
                                  : colorOff;
            }
            if (i < stepLabels.Length && stepLabels[i] != null)
            {
                stepLabels[i].color = st == StepState.Idle ? colorOff : Color.white;
            }
        }

        private void RefreshValves()
        {
            SetValveText(vProdTxt, engine.vProd);
            SetValveText(vRetTxt,  engine.vRet);
            SetValveText(vDesTxt,  engine.vDes);
            if (trapStateTxt != null)
            {
                trapStateTxt.text = engine.trapOpen ? "DRENA" : "CERR";
                trapStateTxt.color = engine.trapOpen ? colorOk : colorOff;
            }
            if (alarmTxt != null)
            {
                alarmTxt.text = engine.alarm ? "ALARMA" : "OK";
                alarmTxt.color = engine.alarm ? colorAlarm : colorOk;
            }
            if (cipTxt != null) cipTxt.text = engine.GetCipStatus();
        }

        private void SetValveText(TMP_Text txt, bool open)
        {
            if (txt == null) return;
            txt.text = open ? "ABR" : "CERR";
            txt.color = open ? colorOk : colorOff;
        }

        private void RefreshButtons()
        {
            // Labels en español, formato 2 líneas para grid 2-columnas
            if (energyLbl   != null) energyLbl.text   = $"1. ENERGÍA: {(engine.energyOn ? "ON" : "OFF")}";
            if (modeLbl     != null) modeLbl.text     = $"Modo:\n{(engine.mode == PasteurizerSimEngine.Mode.Production ? "Producción" : "CIP")}";
            if (fillLbl     != null) fillLbl.text     = $"V. Llenado\n{(engine.fillOn ? "ON" : "OFF")}";
            if (hotPumpLbl  != null) hotPumpLbl.text  = $"Calefactor\n{(engine.pumpHotOn ? "ON" : "OFF")}";
            if (coldPumpLbl != null) coldPumpLbl.text = $"Refrigerador\n{(engine.pumpColdOn ? "ON" : "OFF")}";
            if (milkPumpLbl != null) milkPumpLbl.text = $"B. Producto\n{(engine.pumpMilkOn ? "ON" : "OFF")}";

            SetBtnColor(energyBtn,   engine.energyOn);
            SetBtnColor(fillBtn,     engine.fillOn);
            SetBtnColor(hotPumpBtn,  engine.pumpHotOn);
            SetBtnColor(coldPumpBtn, engine.pumpColdOn);
            SetBtnColor(milkPumpBtn, engine.pumpMilkOn,
                disabled: !engine.CanStartMilk() && !engine.pumpMilkOn);
        }

        private void SetBtnColor(Button btn, bool on, bool disabled = false)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            if (disabled)   img.color = colorBtnDis;
            else if (on)    img.color = colorBtnOn;
            else            img.color = colorBtnOff;
        }

        private void RefreshSliders()
        {
            if (targetVal != null) targetVal.text = engine.targetFillVol.ToString("F0");
            if (flowVal   != null) flowVal.text   = engine.flowRate.ToString("F2");
        }

        private void RefreshTanks()
        {
            if (tankInFill != null)
                tankInFill.fillAmount = Mathf.Clamp01(engine.tankInVol / PasteurizerSimEngine.TANK_IN_MAX);
            if (tankOutFill != null)
                tankOutFill.fillAmount = Mathf.Clamp01(engine.tankOutVol / PasteurizerSimEngine.TANK_OUT_MAX);
            if (tankInVolTxt != null)
                tankInVolTxt.text = $"{engine.tankInVol:F1} L";
            if (tankOutVolTxt != null)
                tankOutVolTxt.text = $"{engine.tankOutVol:F1} L";
            if (pulmonFill != null)
            {
                pulmonFill.fillAmount = Mathf.Clamp01(engine.pulmonLevel);
                pulmonFill.color = engine.pulmonLevel < PasteurizerSimEngine.PULMON_OPTIMAL
                    ? new Color(0.62f, 0.83f, 1f) : new Color(0.77f, 0.40f, 1f);
            }
        }

        private void RefreshBatchMetrics()
        {
            if (mFilled != null) mFilled.text = $"{engine.vFilled:F1} L";
            if (mFinal  != null) mFinal.text  = $"{engine.vFinal:F1} L";
            if (mRecirc != null) mRecirc.text = $"{engine.vRecirc:F1} L";
            if (mEvap   != null) mEvap.text   = $"{engine.vEvap:F3} L";
            if (mLoss   != null) mLoss.text   = $"{engine.vLoss:F1} L";
            if (mYield  != null) mYield.text  = engine.vFilled > 0 ? $"{engine.YieldPct:F1} %" : "-- %";
        }

        private void RefreshHints()
        {
            if (hintTxt != null) hintTxt.text = engine.hint ?? "";
        }
    }
}
