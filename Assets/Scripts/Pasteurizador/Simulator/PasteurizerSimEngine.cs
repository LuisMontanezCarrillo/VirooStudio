using UnityEngine;

namespace ViroLab.Pasteurizador.Simulator
{
    /// Motor de simulación HTST portado de web/app.js (537 líneas).
    /// Reproduce balance volumétrico, intercambiador, tanque pulmón, FDV,
    /// CIP workflow, alarmas y secuencia operativa.
    ///
    /// El estado es público para que el Dashboard UI lo lea cada frame.
    /// Los métodos Toggle* son los handlers de los botones de la UI.
    public class PasteurizerSimEngine : MonoBehaviour
    {
        // ====================================================================
        //  Constantes de proceso (idénticas a app.js)
        // ====================================================================
        public const float SP_HEAT      = 72f; // consigna de pasteurización HTST (Decreto 616/2006: 72–76 °C; teoria del cuestionario: 72 °C)
        public const float SP_HOLD_MIN  = 72f;
        public const float SP_HOLD_S    = 15f;
        public const float SP_OUT       = 4.5f;
        public const float T_PUMP_READY = 65f;

        public const float TANK_IN_MAX  = 100f;
        public const float TANK_OUT_MAX = 150f;
        public const float TANK_MIN_VOL = 10f;
        public const float FILL_RATE    = 1.5f;
        public const float HOLDUP_VOL   = 5.0f;
        public const float EVAP_FACTOR  = 0.002f;

        public const float PULMON_OPTIMAL = 0.70f;
        public const float PULMON_DRAIN   = 0.012f;
        public const float MAKEUP_RATE    = 0.05f;
        public const float STEAM_PSI_MAX  = 30f;

        public enum Mode { Production, Cip }

        [System.Serializable]
        public struct CipStep { public string name; public float duration; }
        public static readonly CipStep[] CipSteps =
        {
            new CipStep { name = "Enjuague preliminar", duration = 45f },
            new CipStep { name = "Limpieza alcalina",   duration = 90f },
            new CipStep { name = "Enjuague intermedio", duration = 45f },
            new CipStep { name = "Limpieza acida",      duration = 90f },
            new CipStep { name = "Enjuague final",      duration = 45f }
        };

        // ====================================================================
        //  Estado del proceso (espejo de `state` en app.js)
        // ====================================================================
        [Header("Energía y secuencia")]
        public bool energyOn = false;
        public bool running = false;
        public Mode mode = Mode.Production;
        public bool fillOn = false;
        public bool pumpHotOn = false;
        public bool pumpColdOn = false;
        public bool pumpMilkOn = false;

        [Header("Temperaturas y tiempo (°C, s)")]
        public float tempHeat = 20f;
        public float tempHold = 20f;
        public float tempOut = 13.6f;
        public float tempBoiler = 20f;
        public float holdTimer = 0f;

        [Header("Válvulas (P&ID FDV)")]
        public bool vProd = false;
        public bool vRet = false;
        public bool vDes = false;
        public bool alarm = false;
        public float lowTempAcc = 0f;

        [Header("Pulmón + caldera")]
        public float pulmonLevel = 0.80f;
        public bool makeupOn = false;
        public float steamPsi = 0f;
        public bool trapOpen = false;

        [Header("Balance volumétrico (L)")]
        public float tankInVol = 0f;
        public float tankOutVol = 0f;
        public float targetFillVol = 50f;
        public float flowRate = 2.5f; // L/s procesados: más rápido = drenaje/llenado de tanques visible

        [Header("Lote (L)")]
        public float vFilled = 0f;
        public float vFinal = 0f;
        public float vRecirc = 0f;
        public float vEvap = 0f;
        public float vLoss = 0f;
        public bool batchActive = false;
        public bool batchClosed = false;

        [Header("CIP")]
        public int cipStep = 0;
        public float cipTime = 0f;

        [Header("UX")]
        public string hint = "Encender Energía y luego pulsar Iniciar.";

        // ====================================================================
        //  Interlocks
        // ====================================================================
        public bool CanStartHot()  => energyOn && running;
        public bool CanStartCold() => energyOn && running;
        public bool CanStartMilk()
            => energyOn && running
            && pumpHotOn
            && tankInVol >= TANK_MIN_VOL
            && tempHeat >= T_PUMP_READY;
        public bool CanFill() => energyOn;

        // ====================================================================
        //  Handlers de UI (equivalentes a los onclick de app.js)
        // ====================================================================
        public void ToggleEnergy()
        {
            energyOn = !energyOn;
            if (!energyOn) ResetPlant();
            else hint = "Pulsar Iniciar.";
        }

        public void StartPlant()
        {
            if (!energyOn) { hint = "Primero encender Energía."; return; }
            running = true;
            hint = "Abrir V. Llenado y luego Calefactor.";
        }

        public void StopPlant()
        {
            running = false;
            fillOn = pumpMilkOn = pumpHotOn = pumpColdOn = false;
            hint = "Planta detenida.";
        }

        public void ToggleMode()
        {
            mode = mode == Mode.Production ? Mode.Cip : Mode.Production;
            if (mode == Mode.Cip) { cipStep = 0; cipTime = CipSteps[0].duration; }
            else                  { cipStep = 0; cipTime = 0f; }
        }

        public void ToggleFill()
        {
            if (!CanFill()) { hint = "Energía debe estar ON."; return; }
            if (!fillOn) StartNewBatchIfNeeded();
            fillOn = !fillOn;
        }

        public void ToggleHotPump()
        {
            if (!CanStartHot()) { hint = "Energía + Iniciar antes del Calefactor."; return; }
            pumpHotOn = !pumpHotOn;
        }

        public void ToggleColdPump()
        {
            if (!CanStartCold()) { hint = "Energía + Iniciar antes del Refrigerador."; return; }
            pumpColdOn = !pumpColdOn;
        }

        public void ToggleMilkPump()
        {
            if (pumpMilkOn) { pumpMilkOn = false; return; }
            if (!energyOn || !running)   { hint = "Falta pulsar Iniciar."; return; }
            if (!pumpHotOn)              { hint = "Calefactor debe estar ON."; return; }
            if (tankInVol < TANK_MIN_VOL){ hint = $"Nivel < {TANK_MIN_VOL} L. Abrir V. Llenado."; return; }
            if (tempHeat < T_PUMP_READY) { hint = $"Esperar T calentamiento ≥ {T_PUMP_READY} °C."; return; }
            pumpMilkOn = true;
            hint = "Bomba de producto activa. Esperando criterio de pasteurización...";
        }

        public void ResetBatch()
        {
            // Antes solo borraba volúmenes y métricas: las bombas seguían encendidas,
            // las temperaturas altas y las válvulas igual, así que "no pasaba nada".
            // Ahora deja el equipo listo para volver a operar desde el principio
            // (la planta sigue con energía; hay que pulsar Iniciar de nuevo).
            running = false;
            fillOn = pumpMilkOn = pumpHotOn = pumpColdOn = false;
            tempHeat = 20f; tempHold = 20f; tempOut = 13.6f; tempBoiler = 20f;
            tankInVol = 0f;
            tankOutVol = 0f;
            vFilled = vFinal = vRecirc = vEvap = vLoss = 0f;
            batchActive = batchClosed = false;
            holdTimer = 0f;
            vProd = vRet = vDes = false;
            alarm = false; lowTempAcc = 0f;
            cipStep = 0; cipTime = 0f;

            // Servicios de agua y vapor. Step() sale antes de calcularlos cuando la
            // planta no esta en marcha (if (!running) return), asi que al parar aqui
            // se quedaban CONGELADOS con el ultimo valor del lote anterior: el
            // manometro seguia marcando presion, la trampa seguia en "DRENA", el
            // pulmon a medio nivel y el agua de reposicion abierta. Esa es la razon
            // de que el boton pareciera no hacer nada: lo que si cambiaba (tanques y
            // temperaturas) pasaba desapercibido al lado de todo lo que no cambiaba.
            // Se devuelven a los valores con los que arrancan declarados arriba.
            pulmonLevel = 0.80f;
            makeupOn = false;
            steamPsi = 0f;
            trapOpen = false;

            hint = "Lote reiniciado. Pulsa Iniciar para comenzar de nuevo.";
        }

        // (resto de hints traducidos al español puro)

        private void StartNewBatchIfNeeded()
        {
            if (!batchActive)
            {
                vFilled = vFinal = vRecirc = vEvap = vLoss = 0f;
                tankOutVol = 0f;
                batchActive = true;
                batchClosed = false;
            }
        }

        private void CloseBatch()
        {
            if (!batchActive || batchClosed) return;
            vLoss += HOLDUP_VOL;
            batchClosed = true;
            batchActive = false;
            float yieldPct = vFilled > 0 ? (vFinal / vFilled) * 100f : 0f;
            hint = $"Lote cerrado. Procesado {vFilled:F1} L | Final {vFinal:F1} L | Rendimiento {yieldPct:F1}%";
        }

        private void ResetPlant()
        {
            running = false;
            fillOn = pumpMilkOn = pumpHotOn = pumpColdOn = false;
            tempHeat = 20f; tempHold = 20f; tempOut = 13.6f; tempBoiler = 20f;
            holdTimer = 0f;
            vProd = vRet = vDes = false;
            alarm = false; lowTempAcc = 0f;
            cipStep = 0; cipTime = 0f;
            hint = "Energía OFF.";
        }

        // ====================================================================
        //  Modelo de proceso (Update)
        // ====================================================================
        private void Update()
        {
            float dt = Mathf.Min(0.2f, Time.deltaTime);
            Step(dt);
        }

        public void Step(float dt)
        {
            if (!energyOn) return;

            // ---------- llenado
            if (fillOn)
            {
                float target = Mathf.Min(targetFillVol, TANK_IN_MAX);
                float room = target - tankInVol;
                if (room > 0f)
                {
                    float delta = Mathf.Min(FILL_RATE * dt, room);
                    tankInVol += delta;
                    vFilled += delta;
                }
                else
                {
                    fillOn = false;
                    hint = $"Tanque cargado a {tankInVol:F1} L. Encender Calefactor.";
                }
                // (hint en español, sin cambios)
            }

            if (!running) return;

            // ---------- CIP
            if (mode == Mode.Cip)
            {
                if (cipStep < CipSteps.Length)
                {
                    cipTime -= dt;
                    if (cipTime <= 0f)
                    {
                        cipStep++;
                        if (cipStep < CipSteps.Length) cipTime = CipSteps[cipStep].duration;
                        else running = false;
                    }
                }
                return;
            }

            // ---------- pulmón
            if (pumpHotOn)
                pulmonLevel = Mathf.Max(0f, pulmonLevel - PULMON_DRAIN * dt);
            makeupOn = pulmonLevel < PULMON_OPTIMAL;
            if (makeupOn)
                pulmonLevel = Mathf.Min(1f, pulmonLevel + MAKEUP_RATE * dt);

            // ---------- caldera + vapor
            bool haveWater = pulmonLevel > 0.05f;
            bool boilerActive = pumpHotOn && haveWater;
            float tBoilerTarget = boilerActive ? 95f : 25f;
            tempBoiler += (tBoilerTarget - tempBoiler) * Mathf.Min(1f, 0.4f * dt);

            float psiTarget = boilerActive
                ? STEAM_PSI_MAX * Mathf.Min(1f, (tempBoiler - 30f) / 65f)
                : 0f;
            steamPsi += (psiTarget - steamPsi) * Mathf.Min(1f, 0.5f * dt);

            trapOpen = boilerActive && tempHeat > 60f;

            // ---------- intercambiador
            // Coherencia con la teoria (72 C / 15 s) y la norma colombiana (banda 72-76 C):
            // - Sobreimpulso LIMITADO a SP_HEAT+3 (~75 C) en vez de seguir a la caldera (~87 C).
            // - Operacion estabilizada en SP_HEAT+1 (~73 C): T_ret = 72.7 C, cumple >=72 y queda en banda.
            float heatTarget = 20f;
            if (pumpHotOn) heatTarget = Mathf.Min(tempBoiler - 8f, SP_HEAT + 3f);
            if (pumpHotOn && (pumpColdOn || pumpMilkOn)) heatTarget = Mathf.Min(heatTarget, SP_HEAT + 1f);
            tempHeat += (heatTarget - tempHeat) * Mathf.Min(1f, 0.5f * dt);
            tempHold = tempHeat - 0.3f;

            // ---------- temporizador retención
            if (pumpMilkOn && tempHold >= SP_HOLD_MIN) holdTimer += dt;
            else if (pumpMilkOn)                       holdTimer = Mathf.Max(0f, holdTimer - dt * 2f);
            else                                       holdTimer = 0f;

            bool valid = tempHold >= SP_HOLD_MIN && holdTimer >= SP_HOLD_S;

            // ---------- válvulas FDV
            if (!pumpMilkOn)
            {
                vProd = vRet = vDes = false;
            }
            else if (valid)
            {
                vProd = true; vRet = false; vDes = false;
            }
            else
            {
                vProd = false; vRet = true; vDes = true;
                if (holdTimer < SP_HOLD_S && tempHold >= SP_HOLD_MIN)
                    hint = $"Acumulando tiempo de retención ({holdTimer:F1}/{SP_HOLD_S} s)...";
            }

            // ---------- balance volumétrico
            if (pumpMilkOn)
            {
                float draw = Mathf.Min(flowRate * dt, tankInVol);
                tankInVol -= draw;
                if (vProd)
                {
                    float ev = draw * EVAP_FACTOR;
                    float toFinal = draw - ev;
                    float room = TANK_OUT_MAX - tankOutVol;
                    float accepted = Mathf.Min(toFinal, room);
                    tankOutVol += accepted;
                    vFinal += accepted;
                    vEvap += ev;
                    if (accepted < toFinal) hint = "Tanque final lleno. Detener bomba.";
                }
                else
                {
                    tankInVol = Mathf.Min(TANK_IN_MAX, tankInVol + draw);
                    vRecirc += draw;
                }
            }

            // ---------- enfriamiento salida
            float outTarget;
            if (!pumpMilkOn || !vProd) outTarget = tempOut;
            else if (pumpColdOn)        outTarget = SP_OUT;
            else                        outTarget = 18f;
            tempOut += (outTarget - tempOut) * Mathf.Min(1f, 0.6f * dt);

            // ---------- alarmas
            if (pumpMilkOn && tempHold < (SP_HOLD_MIN - 2f)) lowTempAcc += dt;
            else                                              lowTempAcc = 0f;
            alarm = lowTempAcc > 8f;
            if (alarm) hint = "ALARMA: T_ret baja. Producto se desvía automáticamente.";

            // ---------- fin de lote por tanque vacío
            if (tankInVol <= 0.05f && pumpMilkOn)
            {
                tankInVol = 0f;
                pumpMilkOn = false;
                CloseBatch();
            }
        }

        // ====================================================================
        //  Helpers de estado para UI
        // ====================================================================
        public string GetCipStatus()
        {
            if (mode == Mode.Cip && cipStep < CipSteps.Length)
                return $"{CipSteps[cipStep].name} ({Mathf.Ceil(cipTime)} s)";
            return "N/A";
        }

        public float YieldPct => vFilled > 0f ? (vFinal / vFilled) * 100f : 0f;
    }
}
