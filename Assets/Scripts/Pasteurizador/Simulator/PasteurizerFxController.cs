using System;
using UnityEngine;

namespace ViroLab.Pasteurizador.Simulator
{
    /// <summary>
    /// Efectos de sonido (FX) del pasteurizador. Dos comportamientos:
    ///  1) FX de PROCESO: se activan/desactivan según el estado del motor
    ///     (arranque, proceso en loop, bombas, caldera, vapor). Van "a la par"
    ///     de la narración/operación.
    ///  2) FX al TOCAR una pieza: al hacer clic en ciertas partes del modelo 3D
    ///     suena su efecto (tablero, caldera, válvulas...).
    ///
    /// Crea sus propios AudioSource. Instalar con:
    ///   Viroo > Simulador > Instalar FX de sonido
    /// </summary>
    public class PasteurizerFxController : MonoBehaviour
    {
        [Header("Referencias (auto si vacío)")]
        public PasteurizerSimEngine engine;
        public ViroLab.Pasteurizador.PasteurizerHoverHandler hover; // para el FX al tocar

        [Header("Volumen")]
        [Range(0f, 1f)] public float loopVolume = 0.6f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        [Range(0f, 1f)] public float touchVolume = 0.9f;

        [Header("FX de proceso · one-shots")]
        public AudioClip inicioClip;      // FX_Inicio Pasteurizador
        public AudioClip finalClip;       // FX_Pasteurizador final
        public AudioClip pumpStart, pumpStop;     // Bomba Pasteurizador Inicio/Apagado
        public AudioClip waterStart, waterStop;   // Bomba de Agua Inicio/Apagando
        public AudioClip boilerStart, boilerStop; // Caldera Inicio/Apagando

        [Header("FX de proceso · loops")]
        public AudioClip procLoopClip;    // FX_Pasteurizador Proceso (LOOP)
        public AudioClip pumpLoopClip;    // FX_Bomba Pasteurizador En Proceso
        public AudioClip waterLoopClip;   // FX_Bomba de Agua Andando
        public AudioClip boilerLoopClip;  // FX_Caldera Proceso
        public AudioClip vaporLoopClip;   // FX_Vapor Caldero

        [Serializable] public struct TouchFx { public string subsystemKey; public AudioClip clip; }
        [Header("FX al tocar una pieza (por subsistema)")]
        public TouchFx[] touchMap = new TouchFx[0];

        // AudioSources (creados en runtime)
        AudioSource _sfx, _touch, _procLoop, _pumpLoop, _waterLoop, _boilerLoop, _vaporLoop;

        // estado previo
        bool pRunning, pMilk, pWater, pBoiler, pVapor;

        void Awake()
        {
            if (engine == null) engine = FindObjectOfType<PasteurizerSimEngine>();
            if (hover == null)  hover  = FindObjectOfType<ViroLab.Pasteurizador.PasteurizerHoverHandler>();

            _sfx      = MakeSrc("FX_OneShot", false, sfxVolume);
            _touch    = MakeSrc("FX_Touch", false, touchVolume);
            _procLoop = MakeSrc("FX_ProcLoop", true, loopVolume);
            _pumpLoop = MakeSrc("FX_PumpLoop", true, loopVolume);
            _waterLoop= MakeSrc("FX_WaterLoop", true, loopVolume);
            _boilerLoop=MakeSrc("FX_BoilerLoop", true, loopVolume);
            _vaporLoop= MakeSrc("FX_VaporLoop", true, loopVolume);
        }

        void OnEnable()
        {
            if (hover != null && hover.OnPartPinned != null)
                hover.OnPartPinned.AddListener(OnPartTouched);
        }
        void OnDisable()
        {
            if (hover != null && hover.OnPartPinned != null)
                hover.OnPartPinned.RemoveListener(OnPartTouched);
            StopAllLoops();
        }

        AudioSource MakeSrc(string name, bool loop, float vol)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f; s.volume = vol;
            return s;
        }

        void Update()
        {
            if (engine == null) { engine = FindObjectOfType<PasteurizerSimEngine>(); if (engine == null) return; }

            // --- Planta en marcha ---
            if (Edge(ref pRunning, engine.running))
            {
                if (engine.running) { OneShot(inicioClip); Loop(_procLoop, procLoopClip, true); }
                else { Loop(_procLoop, procLoopClip, false); OneShot(finalClip); StopAllLoops(); }
            }

            // --- Bomba de producto ---
            if (Edge(ref pMilk, engine.pumpMilkOn))
            {
                if (engine.pumpMilkOn) { OneShot(pumpStart); Loop(_pumpLoop, pumpLoopClip, true); }
                else { Loop(_pumpLoop, pumpLoopClip, false); OneShot(pumpStop); }
            }

            // --- Bombas de agua (caliente o fría) ---
            bool water = engine.pumpHotOn || engine.pumpColdOn;
            if (Edge(ref pWater, water))
            {
                if (water) { OneShot(waterStart); Loop(_waterLoop, waterLoopClip, true); }
                else { Loop(_waterLoop, waterLoopClip, false); OneShot(waterStop); }
            }

            // --- Caldera (con energía) ---
            if (Edge(ref pBoiler, engine.energyOn))
            {
                if (engine.energyOn) { OneShot(boilerStart); Loop(_boilerLoop, boilerLoopClip, true); }
                else { Loop(_boilerLoop, boilerLoopClip, false); OneShot(boilerStop); }
            }

            // --- Vapor (mientras hay presión de vapor) ---
            bool vapor = engine.steamPsi > 1f;
            if (Edge(ref pVapor, vapor))
                Loop(_vaporLoop, vaporLoopClip, vapor);
        }

        void OnPartTouched(ViroLab.Pasteurizador.PasteurizerHoverHandler.PartHitInfo info)
        {
            if (info == null || string.IsNullOrEmpty(info.subsystemKey)) return;
            foreach (var m in touchMap)
                if (m.clip != null && m.subsystemKey == info.subsystemKey)
                {
                    _touch.Stop(); _touch.clip = m.clip; _touch.Play();
                    return;
                }
        }

        // helpers
        static bool Edge(ref bool prev, bool now) { bool e = now != prev; prev = now; return e; }
        void OneShot(AudioClip c) { if (c != null && _sfx != null) _sfx.PlayOneShot(c, sfxVolume); }
        void Loop(AudioSource src, AudioClip clip, bool on)
        {
            if (src == null) return;
            if (on) { if (clip != null && (!src.isPlaying || src.clip != clip)) { src.clip = clip; src.loop = true; src.Play(); } }
            else src.Stop();
        }
        void StopAllLoops()
        {
            foreach (var s in new[] { _procLoop, _pumpLoop, _waterLoop, _boilerLoop, _vaporLoop })
                if (s != null) s.Stop();
            pRunning = pMilk = pWater = pBoiler = pVapor = false;
        }
    }
}
