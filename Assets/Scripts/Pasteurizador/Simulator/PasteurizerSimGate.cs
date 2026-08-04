using UnityEngine;
using ViroLab.Pasteurizador.Simulator;   // PasteurizerSimDashboard vive aqui

namespace ViroLab.Pasteurizador
{
    /// Mantiene el tablero del simulador OCULTO hasta que llega su momento.
    ///
    /// El tablero solo debe aparecer en el tercer momento de la experiencia, despues
    /// de que termine el video que explica como funciona el proceso. Sin esta compuerta
    /// el panel personal se muestra desde el instante en que se pulsa Play, porque el
    /// canvas vive en la escena desde el principio.
    ///
    /// No se desactiva el GameObject: si se hiciera, este mismo componente dejaria de
    /// ejecutarse y nadie podria volver a encenderlo. En su lugar se apaga el Canvas y
    /// se detienen los componentes que trabajan por frame, que ademas es lo que evita
    /// que el tablero consuma recursos mientras no se usa.
    [DisallowMultipleComponent]
    public class PasteurizerSimGate : MonoBehaviour
    {
        [Header("Estado inicial")]
        [Tooltip("Si esta activo, el tablero arranca oculto y solo aparece al llamar Mostrar().")]
        public bool ocultarAlIniciar = true;

        [Header("Debug")]
        public bool logCambios = true;

        private Canvas _canvas;
        private CanvasGroup _group;
        private PasteurizerWorldCanvas _worldCanvas;
        private PasteurizerSimDashboard _dashboard;
        private bool _visible = true;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _group = GetComponent<CanvasGroup>();
            _worldCanvas = GetComponent<PasteurizerWorldCanvas>();
            _dashboard = GetComponent<PasteurizerSimDashboard>();

            if (ocultarAlIniciar) Ocultar();
        }

        /// Enciende el tablero y lo recoloca frente al estudiante.
        /// Pensado para cablearse al evento de fin del video de la Escena 3.
        [ContextMenu("Mostrar tablero (prueba)")]
        public void Mostrar()
        {
            if (_visible) return;
            _visible = true;

            if (_dashboard != null) _dashboard.enabled = true;
            if (_worldCanvas != null)
            {
                _worldCanvas.enabled = true;
                // Que aparezca delante del estudiante mire donde mire, en lugar de
                // en la posicion que tuviera guardada la escena.
                _worldCanvas.Recenter();
            }
            // El CanvasGroup lo crea PasteurizerWorldCanvas en su Awake, y el orden
            // entre Awakes del mismo GameObject no esta garantizado: se resuelve tarde.
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }
            if (_canvas != null) _canvas.enabled = true;

            if (logCambios)
                Debug.Log("<color=lime>[PasteurizerSimGate]</color> Tablero del simulador habilitado.");
        }

        [ContextMenu("Ocultar tablero (prueba)")]
        public void Ocultar()
        {
            _visible = false;

            if (_canvas != null) _canvas.enabled = false;
            // El CanvasGroup lo crea PasteurizerWorldCanvas en su Awake, y el orden
            // entre Awakes del mismo GameObject no esta garantizado: se resuelve tarde.
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }
            // Se apagan despues del canvas para que no vuelvan a escribir el alpha.
            if (_worldCanvas != null) _worldCanvas.enabled = false;
            if (_dashboard != null) _dashboard.enabled = false;

            if (logCambios)
                Debug.Log("<color=cyan>[PasteurizerSimGate]</color> Tablero del simulador oculto " +
                          "hasta que termine el video de la Escena 3.");
        }

        /// Util para cablear a un Toggle o para pruebas.
        public void Alternar()
        {
            if (_visible) Ocultar(); else Mostrar();
        }
    }
}
