using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Reúne a los participantes en marcas separadas del centro de la sala antes
    /// de que el grupo salte al reto siguiente.
    ///
    /// POR QUE NO SE LES JUNTA EN UN PUNTO
    /// -----------------------------------
    /// La tentacion es dibujar un circulo en el centro y pedir que se pongan
    /// todos encima. Seria justo lo contrario de lo que hace falta: en una sala
    /// VIROO la posicion virtual y la fisica son la misma, de modo que juntarlos
    /// en el mundo virtual es juntarlos de verdad, con dos personas a ciegas
    /// dentro de sendos visores. Aqui cada participante tiene SU marca, y las
    /// marcas estan separadas entre si.
    ///
    /// POR QUE ADEMAS CONVIENE
    /// -----------------------
    /// El TeleportAllAction de VIROO mueve al grupo conservando las posiciones
    /// relativas. Si el salto arranca con la gente desperdigada, esa dispersion
    /// viaja al reto siguiente y alguien puede aparecer fuera de la zona util.
    /// Saliendo desde marcas conocidas, la separacion de llegada tambien es
    /// conocida: la misma que haya entre las marcas.
    ///
    /// COMO SE USA
    /// -----------
    /// Donde antes se llamaba a AvanceEnGrupo.AnunciarQueTermine, se llama ahora
    /// a Convocar(). Este componente muestra el aviso, espera a que el
    /// participante local pise su marca y entonces anuncia por el.
    /// </summary>
    [AddComponentMenu("ViroLab/Multiusuario/Reunión en el centro")]
    public class ReunionEnElCentro : MonoBehaviour, IReiniciable
    {
        [Header("A dónde van")]
        [Tooltip("Una marca por participante, colocadas alrededor del centro de la " +
                 "sala y SEPARADAS entre sí. Con dos visores bastan dos, a un par " +
                 "de metros la una de la otra. El reparto va por orden de " +
                 "identificador de cliente, igual en todos los clientes.")]
        public Transform[] marcas;

        [Tooltip("Radio, en metros, dentro del cual se da por pisada la marca.")]
        public float radio = 0.8f;

        [Header("Cuándo convocar")]
        [Tooltip("OPCIONAL. Si se asigna, no se convoca a nadie hasta que este " +
                 "audio termine. Es para no cortar la locución de cierre del reto. " +
                 "En el reto 1 va aquí el SND_Click_Lavamanos, que reproduce los " +
                 "19 segundos de FX_Reto3Botas.")]
        public AudioSource esperarAQueTermine;

        [Tooltip("Segundos de espera adicionales antes de convocar, contando desde " +
                 "que termine el audio de arriba. Sirve para dar margen a lo que no " +
                 "sea audio, como la puerta, que tarda 5 segundos en abrirse.")]
        public float esperaAdicional = 0f;

        [Tooltip("Tope de seguridad, en segundos, esperando a ese audio. Si algo " +
                 "impide que termine, se convoca igualmente.")]
        public float esperaMaximaDelAudio = 30f;

        [Tooltip("Marcado: se convoca de inmediato y el participante camina hacia su " +
                 "marca MIENTRAS suena la locución; el salto sigue esperando a que " +
                 "termine. Ahorra el tiempo de caminar, que de otro modo se suma al " +
                 "de la locución. " +
                 "Sin marcar: primero se escucha entera y después se camina. Más " +
                 "reposado, pero cada tanda cuesta lo que se tarde en llegar.")]
        public bool caminarMientrasHabla = false;

        [Header("Qué pasa después")]
        [Tooltip("El AvanceEnGrupo al que se le anuncia el fin una vez reunidos. Si " +
                 "se deja vacío se busca en este mismo objeto, que es donde suele " +
                 "estar.")]
        public AvanceEnGrupo avance;

        [Tooltip("Segundos máximos esperando a que el participante pise su marca. " +
                 "Pasados, se anuncia igualmente: más vale un salto imperfecto que " +
                 "una sesión atascada. Pon 0 para esperar sin límite.")]
        public float esperaMaxima = 60f;

        [Header("Avisos para la interfaz")]
        [Tooltip("Al convocar. Úsalo para encender el cartel de «ve a tu marca».")]
        public UnityEvent AlConvocar;

        [Tooltip("Recibe la marca asignada a ESTE participante. Úsalo para " +
                 "encenderla o resaltarla, y para apagar las de los demás.")]
        public UnityEventTransform AlAsignarMarca;

        [Tooltip("Al pisar la marca. Úsalo para apagar el cartel.")]
        public UnityEvent AlLlegarALaMarca;

        [Tooltip("Distancia que falta, en metros, mientras el participante camina. " +
                 "Úsalo si quieres mostrarla.")]
        public UnityEventFloat AlCambiarLaDistancia;

        [Serializable] public class UnityEventTransform : UnityEvent<Transform> { }
        [Serializable] public class UnityEventFloat : UnityEvent<float> { }

        [Header("Diagnóstico")]
        public bool log = true;

        private bool _pedido;
        private bool _llego;
        private bool _convocado;
        private bool _yaAnuncie;
        private float _convocadoEn;
        private Transform _miMarca;
        private float _ultimaDistanciaAvisada = -1f;

        // ================================================================
        //  Puesta a punto
        // ================================================================

        /// Al anadir el componente desde el Inspector, deja el campo ya relleno si
        /// el AvanceEnGrupo esta en el mismo objeto.
        private void Reset()
        {
            if (avance == null) avance = GetComponent<AvanceEnGrupo>();
        }

        private void Awake()
        {
            // Red de seguridad: los dos componentes van siempre en el mismo objeto,
            // y olvidar el arrastre no se nota hasta que el estudiante ya ha pisado
            // su marca y el grupo no avanza.
            if (avance == null) avance = GetComponent<AvanceEnGrupo>();

            if (avance == null)
                Debug.LogWarning("[ReunionEnElCentro] No hay AvanceEnGrupo asignado ni en este " +
                                 "objeto. Asígnalo en el campo «Avance» o el grupo no avanzará.", this);
        }

        // ================================================================
        //  Entrada
        // ================================================================

        /// <summary>
        /// Pide al participante local que vaya a su marca. Conéctalo donde antes
        /// se llamaba a AvanceEnGrupo.AnunciarQueTermine.
        /// </summary>
        public void Convocar()
        {
            if (_pedido || _convocado || _yaAnuncie) return;
            _pedido = true;

            // Sin nada que esperar, o pudiendo caminar mientras habla, se convoca
            // en el acto. En el segundo caso quien espera al audio es el anuncio,
            // no la convocatoria.
            if (caminarMientrasHabla || (esperarAQueTermine == null && esperaAdicional <= 0f))
            {
                ConvocarYa();
                return;
            }

            StartCoroutine(ConvocarCuandoToque());
        }

        /// Espera a que acabe de hablar el reto antes de mandar a nadie a su marca.
        /// El aviso de reunion pisando la locucion de cierre se percibe como que la
        /// experiencia atropella al estudiante.
        private IEnumerator ConvocarCuandoToque()
        {
            if (esperarAQueTermine != null)
            {
                float tope = Time.unscaledTime + Mathf.Max(0f, esperaMaximaDelAudio);

                // Un fotograma de cortesia: si a Convocar lo llama el mismo evento
                // que arranca el audio, todavia no consta como sonando.
                yield return null;

                while (esperarAQueTermine.isPlaying && Time.unscaledTime < tope)
                    yield return null;

                if (Time.unscaledTime >= tope)
                    Debug.LogWarning($"[ReunionEnElCentro] El audio de cierre no terminó en " +
                                     $"{esperaMaximaDelAudio:F0} s. Se convoca igualmente.", this);
            }

            if (esperaAdicional > 0f) yield return new WaitForSeconds(esperaAdicional);

            ConvocarYa();
        }

        private void ConvocarYa()
        {
            if (_convocado || _yaAnuncie) return;

            _convocado = true;
            _convocadoEn = Time.unscaledTime;
            _miMarca = MarcaQueMeToca();

            if (log)
            {
                string cual = _miMarca != null ? _miMarca.name : "(ninguna)";
                Debug.Log($"[ReunionEnElCentro] Convocado al puesto " +
                          $"{SesionViroo.IndiceDelJugadorLocal()}: marca {cual}.");
            }

            AlConvocar?.Invoke();
            AlAsignarMarca?.Invoke(_miMarca);

            // Sin marcas configuradas no hay nada que esperar: se anuncia y ya.
            if (_miMarca == null)
            {
                Debug.LogWarning("[ReunionEnElCentro] No hay marca para este participante. " +
                                 "Se avanza sin reunir.", this);
                Anunciar("sin marca que pisar");
            }
        }

        /// <summary>Anuncia sin esperar. Para desatascar desde el cliente del moderador.</summary>
        public void ForzarAnuncio()
        {
            Debug.LogWarning("[ReunionEnElCentro] Anuncio forzado manualmente.", this);
            Anunciar("anuncio forzado");
        }

        // ================================================================
        //  Espera
        // ================================================================

        private void Update()
        {
            if (!_convocado || _yaAnuncie || _miMarca == null) return;

            float d = DistanciaALaMarca();

            // Solo se avisa al cambiar de decimetro: si no, es un evento por
            // fotograma para mover un texto que nadie lee tan fino.
            if (Mathf.Abs(d - _ultimaDistanciaAvisada) >= 0.1f)
            {
                _ultimaDistanciaAvisada = d;
                AlCambiarLaDistancia?.Invoke(d);
            }

            if (d <= radio)
            {
                if (!_llego)
                {
                    _llego = true;
                    AlLlegarALaMarca?.Invoke();
                }

                // Ya esta en su sitio, pero la locucion sigue: se espera a que
                // termine antes de anunciar, para no cortarla con el fundido.
                if (LaVozSigueHablando()) return;

                Anunciar("ha pisado su marca");
                return;
            }

            if (esperaMaxima > 0f && Time.unscaledTime - _convocadoEn >= esperaMaxima)
            {
                Debug.LogWarning($"[ReunionEnElCentro] Se agotaron los {esperaMaxima:F0} s de espera " +
                                 $"con el participante a {d:F1} m de su marca. Se avanza igualmente.", this);
                Anunciar("se agotó la espera");
            }
        }

        /// Cierto mientras el audio de cierre siga sonando, con el mismo tope de
        /// seguridad que usa la convocatoria diferida.
        private bool LaVozSigueHablando()
        {
            if (!caminarMientrasHabla || esperarAQueTermine == null) return false;
            if (Time.unscaledTime - _convocadoEn >= Mathf.Max(0f, esperaMaximaDelAudio)) return false;
            return esperarAQueTermine.isPlaying;
        }

        private float DistanciaALaMarca()
        {
            var cabeza = Camera.main != null ? Camera.main.transform : null;
            if (cabeza == null || _miMarca == null) return float.MaxValue;

            // En plano: la altura de la cabeza no dice nada sobre si esta en el
            // sitio, y entre una persona de 1,60 y una de 1,90 habria 30 cm de
            // diferencia sin haberse movido nadie.
            Vector3 a = cabeza.position;
            Vector3 b = _miMarca.position;
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private Transform MarcaQueMeToca()
        {
            if (marcas == null || marcas.Length == 0) return null;

            int i = SesionViroo.IndiceDelJugadorLocal();

            // Mas participantes que marcas no deberia pasar (la sala admite dos),
            // pero si pasa es mejor repartirlos que dejar a alguien sin sitio.
            if (i >= marcas.Length) i %= marcas.Length;
            return marcas[i];
        }

        private void Anunciar(string motivo)
        {
            if (_yaAnuncie) return;
            _yaAnuncie = true;

            if (avance == null)
            {
                Debug.LogError("[ReunionEnElCentro] Falta el AvanceEnGrupo: el grupo no avanzará.", this);
                return;
            }

            if (log) Debug.Log($"[ReunionEnElCentro] Anunciando el fin ({motivo}).");
            avance.AnunciarQueTermine();
        }

        // ================================================================
        //  Reinicio entre tandas
        // ================================================================

        public void ReiniciarAlInicio()
        {
            StopAllCoroutines();
            _pedido = false;
            _convocado = false;
            _llego = false;
            _yaAnuncie = false;
            _convocadoEn = 0f;
            _miMarca = null;
            _ultimaDistanciaAvisada = -1f;
        }

        // ================================================================
        //  Ayuda visual en el editor
        // ================================================================

        private void OnDrawGizmosSelected()
        {
            if (marcas == null) return;

            foreach (var m in marcas)
            {
                if (m == null) continue;
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
                Gizmos.DrawWireSphere(m.position, radio);
            }

            // La separacion entre marcas es la que tendran los participantes al
            // saltar, asi que conviene verla dibujada.
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            for (int i = 0; i < marcas.Length; i++)
                for (int j = i + 1; j < marcas.Length; j++)
                    if (marcas[i] != null && marcas[j] != null)
                        Gizmos.DrawLine(marcas[i].position, marcas[j].position);
        }
    }
}
