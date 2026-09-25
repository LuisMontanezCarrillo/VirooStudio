using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Los mandos que quien modera la prueba necesita tener a mano el día de las
    /// pruebas: reiniciar para la tanda siguiente y desatascar un grupo.
    ///
    /// POR QUE POR TECLADO Y NO CON UN BOTON EN LA ESCENA
    /// --------------------------------------------------
    /// El 29 hay veinte tandas. Un boton dentro del mundo virtual obliga a
    /// buscarlo, apuntarlo y pulsarlo veinte veces, y ademas queda al alcance de
    /// los estudiantes. Una combinacion de teclas no esta en ninguna parte del
    /// mundo virtual, no se puede pulsar por accidente desde un visor, y el
    /// moderador la tiene siempre disponible desde su cliente de escritorio.
    ///
    /// Da igual desde que cliente se pulse: el reinicio se difunde por la red de
    /// VIROO y llega a los dos visores.
    ///
    /// SEGURIDAD
    /// ---------
    /// El reinicio pide la combinacion DOS VECES seguidas. Reiniciar por error a
    /// mitad de una tanda cuesta la tanda entera, y con cuarenta estudiantes
    /// citados eso no se recupera.
    /// </summary>
    [AddComponentMenu("ViroLab/Multiusuario/Mando del moderador")]
    public class MandoDelModerador : MonoBehaviour
    {
        [Header("A qué manda")]
        [Tooltip("Si se deja vacío se busca en la escena.")]
        public ReinicioDeExperiencia reinicio;

        [Tooltip("Si se deja vacío se buscan todos los de la escena.")]
        public AvanceEnGrupo[] avances;

        [Tooltip("Si se deja vacío se buscan todas las de la escena.")]
        public ReunionEnElCentro[] reuniones;

#if ENABLE_INPUT_SYSTEM
        [Header("Teclas")]
        [Tooltip("Reinicia la experiencia para la tanda siguiente. Pide confirmación.")]
        public Key teclaDeReinicio = Key.R;

        [Tooltip("Hace avanzar al grupo sin esperar a nadie. Para desatascar.")]
        public Key teclaDeAvanceForzado = Key.F;

        [Tooltip("Da por buena la reunión sin que nadie pise su marca.")]
        public Key teclaDeReunionForzada = Key.G;
#endif

        [Tooltip("Exigir Ctrl y Mayúsculas además de la tecla. Déjalo marcado: sin " +
                 "eso, una tecla suelta reinicia la sesión.")]
        public bool exigirCtrlYMayusculas = true;

        [Header("Confirmación del reinicio")]
        [Tooltip("El reinicio pide la combinación dos veces. Reiniciar por error a " +
                 "mitad de una tanda cuesta la tanda entera.")]
        public bool confirmacionDoble = true;

        [Tooltip("Segundos para confirmar. Pasados, hay que volver a empezar.")]
        public float ventanaDeConfirmacion = 4f;

        [Header("Avisos para la interfaz")]
        [Tooltip("Al pedir el reinicio la primera vez. Úsalo para mostrar «pulsa otra " +
                 "vez para confirmar» en la pantalla del moderador.")]
        public UnityEvent AlPedirConfirmacion;

        [Tooltip("Al caducar la confirmación sin repetir la combinación.")]
        public UnityEvent AlCancelarse;

        [Header("Diagnóstico")]
        public bool log = true;

        private float _pedidoEn = -999f;

        private void Awake()
        {
            if (reinicio == null) reinicio = FindFirstObjectByType<ReinicioDeExperiencia>(FindObjectsInactive.Include);
            if (avances == null || avances.Length == 0)
                avances = FindObjectsByType<AvanceEnGrupo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (reuniones == null || reuniones.Length == 0)
                reuniones = FindObjectsByType<ReunionEnElCentro>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (reinicio == null)
                Debug.LogWarning("[MandoDelModerador] No hay ReinicioDeExperiencia en la escena: " +
                                 "la tecla de reinicio no hará nada.", this);

            if (log)
                Debug.Log($"[MandoDelModerador] Listo. {avances.Length} avance(s) y " +
                          $"{reuniones.Length} reunión(es) bajo control." + Atajos());
        }

#if ENABLE_INPUT_SYSTEM
        private void Update()
        {
            var t = Keyboard.current;
            if (t == null) return;   // sin teclado (visor autonomo) no hay nada que leer

            if (exigirCtrlYMayusculas)
            {
                bool ctrl = t.leftCtrlKey.isPressed || t.rightCtrlKey.isPressed;
                bool may  = t.leftShiftKey.isPressed || t.rightShiftKey.isPressed;
                if (!ctrl || !may)
                {
                    CaducarConfirmacion();
                    return;
                }
            }

            if (Pulsada(t, teclaDeReinicio)) PedirReinicio();
            if (Pulsada(t, teclaDeAvanceForzado)) ForzarAvance();
            if (Pulsada(t, teclaDeReunionForzada)) ForzarReunion();

            CaducarConfirmacion();
        }

        private static bool Pulsada(Keyboard t, Key k)
        {
            return k != Key.None && t[k].wasPressedThisFrame;
        }
#endif

        // ================================================================
        //  Acciones, tambien invocables desde un boton del Inspector
        // ================================================================

        /// <summary>
        /// Reinicia para la tanda siguiente. Con la confirmación doble activa, la
        /// primera llamada solo avisa; la segunda, dentro de la ventana, ejecuta.
        /// </summary>
        public void PedirReinicio()
        {
            if (reinicio == null)
            {
                Debug.LogError("[MandoDelModerador] No hay ReinicioDeExperiencia asignado.", this);
                return;
            }

            if (confirmacionDoble && Time.unscaledTime - _pedidoEn > ventanaDeConfirmacion)
            {
                _pedidoEn = Time.unscaledTime;
                Debug.LogWarning($"[MandoDelModerador] ¿Reiniciar para la tanda siguiente? " +
                                 $"Repite la combinación en menos de {ventanaDeConfirmacion:F0} s.", this);
                AlPedirConfirmacion?.Invoke();
                return;
            }

            _pedidoEn = -999f;
            if (log) Debug.Log("[MandoDelModerador] Reinicio confirmado.");
            reinicio.PedirReinicio();
        }

        /// <summary>Hace avanzar al grupo sin esperar a nadie.</summary>
        public void ForzarAvance()
        {
            int n = 0;
            foreach (var a in avances) { if (a != null) { a.ForzarAvance(); n++; } }
            Debug.LogWarning($"[MandoDelModerador] Avance forzado en {n} punto(s) de transición.", this);
        }

        /// <summary>Da por buena la reunión sin que nadie pise su marca.</summary>
        public void ForzarReunion()
        {
            int n = 0;
            foreach (var r in reuniones) { if (r != null) { r.ForzarAnuncio(); n++; } }
            Debug.LogWarning($"[MandoDelModerador] Reunión dada por buena en {n} punto(s).", this);
        }

        private void CaducarConfirmacion()
        {
            if (_pedidoEn < 0f) return;
            if (Time.unscaledTime - _pedidoEn <= ventanaDeConfirmacion) return;

            _pedidoEn = -999f;
            if (log) Debug.Log("[MandoDelModerador] Confirmación caducada; el reinicio se cancela.");
            AlCancelarse?.Invoke();
        }

        private string Atajos()
        {
#if ENABLE_INPUT_SYSTEM
            string pre = exigirCtrlYMayusculas ? "Ctrl+Mayús+" : "";
            return $"  Reinicio: {pre}{teclaDeReinicio}  ·  Avance forzado: {pre}{teclaDeAvanceForzado}" +
                   $"  ·  Reunión forzada: {pre}{teclaDeReunionForzada}";
#else
            return "  (sin Input System, las teclas no funcionan)";
#endif
        }
    }
}
