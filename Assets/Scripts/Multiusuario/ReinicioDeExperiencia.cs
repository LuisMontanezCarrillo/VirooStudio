using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Viroo.Interactions;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Devuelve la experiencia completa al punto de partida sin cerrar la
    /// aplicacion, para encadenar tandas de estudiantes.
    ///
    /// EL PROBLEMA
    /// -----------
    /// La muestra son cuarenta estudiantes y el laboratorio autoriza dos a la
    /// vez: veinte pasadas en una jornada de nueve horas. Medido sobre los
    /// archivos del proyecto, solo el video y la locucion que no se pueden
    /// saltar suman 12 minutos y 10 segundos por pasada; el resto del tiempo se
    /// va en ponerse el visor, calibrar y operar. Si ademas hay que cerrar y
    /// relanzar la aplicacion veinte veces desde el Room Player, con su carga de
    /// escena y su emparejamiento de visores, el dia no da.
    ///
    /// COMO FUNCIONA
    /// -------------
    /// 1. El moderador pulsa una sola vez, desde su cliente de escritorio.
    /// 2. La peticion viaja por una accion de difusion de VIROO, de modo que los
    ///    dos visores se reinician a la vez y no se queda uno a medias.
    /// 3. Con la pantalla ya en negro, cada componente que sabe reiniciarse se
    ///    reinicia solo (ver IReiniciable). Lo que no se puede expresar en
    ///    codigo, que son los lienzos y las puertas, se cablea en los dos
    ///    eventos de abajo.
    /// 4. El grupo vuelve al punto de partida con el teletransporte propio de
    ///    VIROO, que mueve a todos conservando las posiciones relativas. Moverlos
    ///    por separado rompe la proteccion frente a choques.
    /// </summary>
    [AddComponentMenu("ViroLab/Multiusuario/Reinicio de la experiencia")]
    public class ReinicioDeExperiencia : MonoBehaviour
    {
        [Header("Acciones de VIROO")]
        [Tooltip("UnityEventAction (de difusión) cuyo evento debe llamar a " +
                 "ReiniciarAqui de ESTE componente. Es lo que hace que los dos " +
                 "visores se reinicien a la vez.")]
        public ObjectAction avisoDeReinicio;

        [Tooltip("TeleportAllAction que devuelve al grupo al punto de partida. Su " +
                 "campo Teleport Position se fija desde aquí antes de ejecutarlo.")]
        public TeleportAllAction saltoAlInicio;

        [Tooltip("Dónde empieza la experiencia. Es el transform donde se situará el " +
                 "primer participante; el resto conserva su posición relativa.")]
        public Transform puntoDeInicio;

        [Header("Ritmo")]
        [Tooltip("Duración del fundido de salida y de entrada, en segundos.")]
        public float duracionFundido = 1.5f;

        [Tooltip("Pausa tras el salto, antes de aclarar. Da margen a que VIROO " +
                 "coloque a los participantes y a que los lienzos del estado " +
                 "inicial estén encendidos cuando vuelva la vista.")]
        public float margenTrasElSalto = 0.5f;

        [Tooltip("Segundos durante los que se ignora una segunda petición. Evita " +
                 "que un doble clic del moderador reinicie dos veces.")]
        public float margenAntirrebote = 3f;

        [Header("Estado de la escena (cableado en el Inspector)")]
        [Tooltip("Se dispara con la pantalla aún clara, al empezar. Úsalo para APAGAR " +
                 "lo que pertenece a los retos 2 y 3: el lienzo del cuestionario, el " +
                 "del vídeo, el tablero del simulador.")]
        public UnityEvent AlEmpezarElReinicio;

        [Tooltip("Se dispara con la pantalla aún en negro, al terminar. Úsalo para " +
                 "ENCENDER el estado de partida: el lienzo del reto 1, la puerta " +
                 "cerrada, los casilleros llenos.")]
        public UnityEvent AlTerminarElReinicio;

        [Header("Diagnóstico")]
        public bool log = true;

        private float _ultimoReinicio = -999f;
        private Coroutine _enCurso;

        // ================================================================
        //  Entradas
        // ================================================================

        /// <summary>
        /// Lo llama el moderador. Difunde la peticion para que TODOS los clientes
        /// se reinicien, incluido el suyo.
        /// </summary>
        public void PedirReinicio()
        {
            if (avisoDeReinicio == null)
            {
                Debug.LogWarning("[ReinicioDeExperiencia] No hay acción de difusión asignada: " +
                                 "se reinicia solo este cliente. Con dos visores en sala, el otro " +
                                 "se quedaría donde estaba.", this);
                ReiniciarAqui();
                return;
            }

            if (log) Debug.Log("[ReinicioDeExperiencia] Difundiendo la petición de reinicio.");
            avisoDeReinicio.Execute();
        }

        /// <summary>
        /// Reinicia ESTE cliente. Lo llama el evento de la accion difundida.
        /// </summary>
        public void ReiniciarAqui()
        {
            if (Time.unscaledTime - _ultimoReinicio < margenAntirrebote)
            {
                if (log) Debug.Log("[ReinicioDeExperiencia] Petición repetida demasiado pronto; se ignora.");
                return;
            }
            _ultimoReinicio = Time.unscaledTime;

            if (_enCurso != null) StopCoroutine(_enCurso);
            _enCurso = StartCoroutine(Rutina());
        }

        // ================================================================
        //  La vuelta al principio
        // ================================================================

        private IEnumerator Rutina()
        {
            if (log) Debug.Log("[ReinicioDeExperiencia] Devolviendo la experiencia al punto de partida.");

            // Callar primero: durante el reinicio no debe quedar ni una voz ni un
            // video sonando del recorrido anterior.
            NarradorUnico.Callar();

            AlEmpezarElReinicio?.Invoke();

            var fundido = FundidoDePantalla.Instancia;
            yield return fundido.Oscurecer(duracionFundido);

            int reiniciados = ReiniciarComponentes();
            if (log) Debug.Log("[ReinicioDeExperiencia] " + reiniciados +
                               " componentes devueltos a su estado inicial.");

            DevolverAlGrupoAlInicio();

            yield return new WaitForSeconds(margenTrasElSalto);

            AlTerminarElReinicio?.Invoke();

            yield return fundido.Aclarar(duracionFundido);

            _enCurso = null;
            if (log) Debug.Log("[ReinicioDeExperiencia] Listo para la tanda siguiente.");
        }

        /// <summary>
        /// Llama a ReiniciarAlInicio en todo lo que lo implemente. Incluye los
        /// objetos apagados a proposito: el lienzo del cuestionario arranca
        /// inactivo en la escena y aun asi guarda por que pregunta iba.
        /// </summary>
        private int ReiniciarComponentes()
        {
            var todos = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            int n = 0;
            foreach (var mb in todos)
            {
                if (mb is not IReiniciable r) continue;

                // Un componente que falle al reiniciarse no puede dejar a medias a
                // los demas, ni dejar la pantalla en negro.
                try { r.ReiniciarAlInicio(); n++; }
                catch (System.Exception e)
                {
                    Debug.LogError("[ReinicioDeExperiencia] " + mb.GetType().Name +
                                   " falló al reiniciarse: " + e.Message, mb);
                }
            }
            return n;
        }

        private void DevolverAlGrupoAlInicio()
        {
            if (saltoAlInicio == null)
            {
                Debug.LogError("[ReinicioDeExperiencia] Falta el TeleportAllAction. El grupo se " +
                               "queda donde esté. NO lo sustituyas moviendo el PlayerRoot a mano: " +
                               "eso rompe la protección frente a choques.", this);
                return;
            }

            // Un solo cliente lo ejecuta. Si lo ejecutaran los dos, el grupo
            // saltaria dos veces seguidas.
            if (!SesionViroo.SoyElClienteDesignado())
            {
                if (log) Debug.Log("[ReinicioDeExperiencia] El salto al inicio lo ejecuta otro cliente.");
                return;
            }

            if (puntoDeInicio != null) saltoAlInicio.TeleportPosition = puntoDeInicio;
            saltoAlInicio.Execute();
        }
    }
}
