using UnityEngine;
using UnityEngine.UI;

namespace ViroLab.Pasteurizador.Simulator
{
    /// <summary>
    /// Ventana emergente inicial del simulador 2D, con el botón EMPEZAR.
    /// Al pulsarlo: oculta la ventana y arranca la guía por voz (bienvenida).
    /// La construye el builder del dashboard.
    /// </summary>
    public class PasteurizerStartOverlay : MonoBehaviour
    {
        [Header("Referencias")]
        public GameObject panel;              // la ventana emergente
        public Button startButton;            // botón EMPEZAR
        public PasteurizerVoiceGuide guide;   // guía por voz (auto si queda vacío)

        [Tooltip("Volver a mostrar la ventana EMPEZAR cuando se reinicia el lote.")]
        public bool showAgainOnReset = true;

        [Tooltip("Al reiniciar el lote, volver a narrar la guia por voz desde la " +
                 "bienvenida. Desactivalo si en las pruebas resulta repetitivo: la " +
                 "ventana EMPEZAR seguira apareciendo igual.")]
        public bool reiniciarGuiaAlReiniciar = true;

        void Awake()
        {
            if (guide == null) guide = FindObjectOfType<PasteurizerVoiceGuide>();
            if (panel == null) panel = gameObject;
        }

        void OnEnable()
        {
            if (panel != null) panel.SetActive(true);
            if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        }

        void OnDisable()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartPressed);
        }

        /// <summary>Pulsó EMPEZAR: cierra la ventana y arranca la narración.</summary>
        public void OnStartPressed()
        {
            if (guide == null) guide = FindObjectOfType<PasteurizerVoiceGuide>();
            if (guide != null) guide.StartGuide();
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>
        /// Vuelve a dejar el simulador como al entrar: ventana EMPEZAR visible y,
        /// si esta activado, la narracion lista para sonar desde la bienvenida.
        /// La llama el boton Reiniciar Lote del tablero.
        /// </summary>
        public void ShowAgain()
        {
            if (panel != null) panel.SetActive(true);

            if (!reiniciarGuiaAlReiniciar) return;
            if (guide == null) guide = FindFirstObjectByType<PasteurizerVoiceGuide>();
            if (guide != null) guide.ResetGuide();
        }
    }
}
