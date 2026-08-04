using UnityEngine;

namespace ViroLab.Pasteurizador
{
    /// Marca un collider que DETIENE el rayo del explorador de piezas.
    ///
    /// La UI de Unity no tiene colliders fisicos, asi que un raycast la atraviesa como
    /// si no existiera y golpea la maquina que hay detras: por eso pulsar ANTERIOR o
    /// SIGUIENTE en el carrusel abria la ficha de una pieza del pasteurizador.
    ///
    /// Detectar "estoy sobre UI" por eventos (uiHoverEntered, IsPointerOverGameObject)
    /// depende del modo de interaccion activo y falla en escritorio, donde VIROO usa su
    /// propio interactor de raton en lugar del puntero estandar del EventSystem. Por eso
    /// aqui se resuelve con geometria: se pone una caja invisible detras de cada panel y
    /// el rayo se detiene en ella.
    ///
    /// La caja va DETRAS del plano del canvas a proposito: si estuviera delante podria
    /// bloquear el raycast de la propia UI (TrackedDeviceGraphicRaycaster comprueba
    /// oclusiones) y los botones dejarian de responder.
    [DisallowMultipleComponent]
    public class PasteurizerRayBlocker : MonoBehaviour
    {
        private Canvas _canvas;
        private CanvasGroup _group;
        private bool _resuelto;

        /// Solo bloquea si su panel se esta viendo de verdad.
        ///
        /// Importa para paneles que se ocultan bajando el alpha en vez de desactivar el
        /// GameObject, como la tarjeta de descripcion: su collider seguiria existiendo y
        /// taparia permanentemente el centro del campo visual, impidiendo señalar piezas.
        public bool Blocks
        {
            get
            {
                if (!_resuelto)
                {
                    _canvas = GetComponentInParent<Canvas>();
                    if (_canvas != null) _group = _canvas.GetComponent<CanvasGroup>();
                    _resuelto = true;
                }

                if (_canvas == null) return true;                  // suelto: bloquea siempre
                if (!_canvas.isActiveAndEnabled) return false;     // panel apagado
                if (_group != null && _group.alpha < 0.05f) return false;  // panel transparente
                return true;
            }
        }
    }
}
