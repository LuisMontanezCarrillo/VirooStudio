using UnityEngine;
using UnityEngine.UI;

namespace ViroLab.Pasteurizador.Simulator
{
    /// <summary>
    /// Ventana emergente inicial del simulador 2D, con el botón EMPEZAR.
    /// Al pulsarlo: oculta la ventana y arranca la guía por voz (bienvenida).
    /// La construye el builder del dashboard.
    /// </summary>
    public class PasteurizerStartOverlay : MonoBehaviour, IReiniciable
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

        [Tooltip("Hacer que el velo de la ventana bloquee el rayo del mando. El builder " +
                 "lo crea con raycastTarget apagado, de modo que el estudiante podía " +
                 "pulsar los botones del tablero a través de la ventana, sin haber " +
                 "pulsado EMPEZAR.")]
        public bool elVeloBloqueaElRayo = true;

        void Awake()
        {
            if (guide == null) guide = FindObjectOfType<PasteurizerVoiceGuide>();
            if (panel == null) panel = gameObject;

            BloquearElVelo();
        }

        /// El velo es el rectangulo oscuro que cubre el tablero mientras la ventana
        /// esta abierta. AddImage lo crea con raycastTarget = false, igual que todo
        /// lo demas del builder, y por eso no bloqueaba nada: un docente reporto que
        /// alcanzaba los controles de detras. Aqui se corrige sin reconstruir.
        private void BloquearElVelo()
        {
            if (!elVeloBloqueaElRayo) return;

            var raiz = panel != null ? panel.transform : transform;
            foreach (var img in raiz.GetComponentsInChildren<Image>(true))
            {
                if (img == null || img.name != "Veil") continue;
                img.raycastTarget = true;
                return;
            }
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
        /// <summary>
        /// Devuelve el cartel de EMPEZAR delante del tablero. Sin esto, la tanda
        /// siguiente encontraria el simulador ya arrancado.
        /// </summary>
        public void ReiniciarAlInicio() { ShowAgain(); }

        public void ShowAgain()
        {
            if (panel != null) panel.SetActive(true);

            if (!reiniciarGuiaAlReiniciar) return;
            if (guide == null) guide = FindFirstObjectByType<PasteurizerVoiceGuide>();
            if (guide != null) guide.ResetGuide();
        }
    }
}
