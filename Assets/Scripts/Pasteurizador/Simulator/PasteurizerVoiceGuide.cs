using UnityEngine;

namespace ViroLab.Pasteurizador.Simulator
{
    /// <summary>
    /// Guía por voz del simulador 2D (tablero SCADA).
    ///  - Al acercarse el estudiante al tablero: reproduce la Bienvenida.
    ///  - A medida que el proceso avanza (según el estado real del motor) reproduce
    ///    el audio que corresponde: Energía, Iniciar, Llenado, Calefactor, etc.
    ///  - Si el estudiante actúa en desorden, reproduce el audio de corrección
    ///    (según la pista/hint del motor).
    ///
    /// No requiere cablear los botones: observa el estado del PasteurizerSimEngine.
    /// Instalar con:  Viroo > Simulador > Instalar guía por voz (SCADA)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PasteurizerVoiceGuide : MonoBehaviour
    {
        [Header("Referencias (se autocompletan si quedan vacías)")]
        public PasteurizerSimEngine engine;
        public AudioSource source;
        [Tooltip("Punto de referencia para medir la cercanía del estudiante. Por defecto, este objeto.")]
        public Transform tablero;

        public enum StartMode { BotonEmpezar, Proximidad }
        [Header("¿Cómo arranca la guía?")]
        [Tooltip("BotonEmpezar: espera a que el estudiante pulse EMPEZAR en el tablero.\nProximidad: suena al acercarse.")]
        public StartMode startMode = StartMode.BotonEmpezar;

        [Header("Proximidad (solo si startMode = Proximidad)")]
        [Tooltip("Distancia (m) a la que se dispara la Bienvenida al acercarse.")]
        public float proximityDistance = 4f;
        [Tooltip("Si está activo, saluda al iniciar la escena sin esperar la cercanía.")]
        public bool greetOnStart = false;
        [Tooltip("Segundos a ignorar al iniciar (deja que el rig/cámara se posicione antes de medir cercanía).")]
        public float startupDelay = 0.75f;

        [Header("Audios · secuencia")]
        public AudioClip bienvenida;
        public AudioClip energia;
        public AudioClip iniciar;
        public AudioClip llenado;
        public AudioClip calefactor;
        public AudioClip tempLista;
        public AudioClip refrigerador;
        public AudioClip bombaProducto;
        public AudioClip acumulando;
        public AudioClip valido;
        public AudioClip alarma;
        public AudioClip tanqueLleno;
        public AudioClip finLote;

        [Header("Audios · corrección")]
        public AudioClip faltaEnergia;
        public AudioClip faltaIniciar;
        public AudioClip faltaTemp;
        public AudioClip faltaNivel;

        // --- estado previo para detectar transiciones ---
        bool _greeted;
        bool _armed; // true cuando el jugador ha estado lejos (para saludar solo al acercarse)
        bool pEnergy, pRunning, pFilled, pHot, pTempReady, pCold, pMilk, pAccum, pValid, pAlarm, pTankFull, pClosed;
        string _lastHint = "";

        void Reset() { source = GetComponent<AudioSource>(); tablero = transform; }

        void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            if (source != null) { source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f; }
            if (engine == null) engine = FindObjectOfType<PasteurizerSimEngine>();
            if (tablero == null) tablero = transform;
        }

        void OnEnable() { ResetGuide(); }

        void Update()
        {
            if (engine == null) { engine = FindObjectOfType<PasteurizerSimEngine>(); if (engine == null) return; }
            HandleProximity();
            HandleSequence();
            HandleCorrections();
        }

        /// <summary>Arranca la guía: reproduce la bienvenida. La llama el botón EMPEZAR del tablero.</summary>
        public void StartGuide()
        {
            if (_greeted) return;
            Play(bienvenida);
            _greeted = true;
        }

        void HandleProximity()
        {
            if (_greeted) return;
            if (startMode == StartMode.BotonEmpezar) return; // arranca con el botón EMPEZAR, no por cercanía
            if (greetOnStart) { Play(bienvenida); _greeted = true; return; }
            if (Time.timeSinceLevelLoad < startupDelay) return; // ignora los primeros frames (cámara posicionándose)
            var cam = GetPlayerCamera();
            if (cam == null) return;
            float d = Vector3.Distance(cam.position, tablero.position);
            if (d > proximityDistance) { _armed = true; return; } // el jugador está lejos: se arma
            if (_armed) { Play(bienvenida); _greeted = true; } // solo saluda si venía de lejos (se acercó al tablero)
        }

        void HandleSequence()
        {
            if (Edge(ref pEnergy, engine.energyOn)) Play(energia);
            if (Edge(ref pRunning, engine.running)) Play(iniciar);

            bool filled = engine.tankInVol >= PasteurizerSimEngine.TANK_MIN_VOL;
            if (Edge(ref pFilled, filled)) Play(llenado);

            if (Edge(ref pHot, engine.pumpHotOn)) Play(calefactor);

            bool tempReady = engine.pumpHotOn && engine.tempHeat >= PasteurizerSimEngine.T_PUMP_READY;
            if (Edge(ref pTempReady, tempReady)) Play(tempLista);

            if (Edge(ref pCold, engine.pumpColdOn)) Play(refrigerador);
            if (Edge(ref pMilk, engine.pumpMilkOn)) Play(bombaProducto);

            bool accumulating = engine.pumpMilkOn
                                && engine.tempHold >= PasteurizerSimEngine.SP_HOLD_MIN
                                && engine.holdTimer > 0.1f
                                && !engine.vProd;
            if (Edge(ref pAccum, accumulating)) Play(acumulando);

            if (Edge(ref pValid, engine.vProd)) Play(valido);
            if (Edge(ref pAlarm, engine.alarm)) Play(alarma);

            bool tankFull = engine.tankOutVol >= PasteurizerSimEngine.TANK_OUT_MAX - 0.5f;
            if (Edge(ref pTankFull, tankFull)) Play(tanqueLleno);

            if (Edge(ref pClosed, engine.batchClosed)) Play(finLote);
        }

        void HandleCorrections()
        {
            string h = engine.hint ?? "";
            if (h == _lastHint) return;   // solo al cambiar la pista
            _lastHint = h;
            string x = h.ToLower();

            if (x.Contains("primero encender energ") || x.Contains("energía debe estar") || x.Contains("energia debe estar"))
                Play(faltaEnergia);
            else if (x.Contains("falta pulsar iniciar") || x.Contains("iniciar antes"))
                Play(faltaIniciar);
            else if (x.Contains("esperar t calentamiento") || x.Contains("calefactor debe estar"))
                Play(faltaTemp);
            else if (x.Contains("nivel <"))
                Play(faltaNivel);
        }

        static bool Edge(ref bool prev, bool now) { bool e = now && !prev; prev = now; return e; }

        Transform GetPlayerCamera()
        {
            if (Camera.main != null) return Camera.main.transform;
            if (Camera.allCamerasCount > 0)
            {
                var cams = Camera.allCameras;
                if (cams.Length > 0 && cams[0] != null) return cams[0].transform;
            }
            return null;
        }

        void Play(AudioClip c)
        {
            if (c == null || source == null) return;
            source.Stop();
            source.clip = c;
            source.Play();
        }

        [ContextMenu("Reiniciar guía")]
        public void ResetGuide()
        {
            _greeted = false;
            _armed = false;
            pEnergy = pRunning = pFilled = pHot = pTempReady = pCold = pMilk = pAccum = pValid = pAlarm = pTankFull = pClosed = false;
            _lastHint = "";
        }
    }
}
