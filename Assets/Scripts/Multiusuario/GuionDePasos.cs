using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// El panel que contesta «¿qué hago ahora?» sin que nadie tenga que
    /// preguntarlo: la lista de pasos de la zona, con lo ya hecho marcado y lo
    /// que toca resaltado, acompañando la mirada del participante.
    ///
    /// POR QUE EXISTE
    /// --------------
    /// Es la observacion que mas se repitio en las pruebas de usuario del 29 de
    /// septiembre de 2026: los participantes se pierden y no saben que sigue. El
    /// perfil lo explica — son estudiantes de Ingenieria de Alimentos, con poca
    /// practica en RV, a los que la experiencia les habla una sola vez, por voz,
    /// al entrar en cada zona. Quien se distrae en esa frase, o quien llega
    /// tarde a ella, no tiene a donde volver a mirar.
    ///
    /// Una locucion es un mensaje que se emite; una lista es un mensaje que se
    /// queda. Este componente convierte lo segundo en lo que el participante ve
    /// mientras camina.
    ///
    /// POR QUE LA LISTA ENTERA Y NO SOLO EL PASO ACTUAL
    /// -----------------------------------------------
    /// A quien esta desorientado le tranquiliza tanto saber cuanto le falta como
    /// saber que le toca. Ver tres pasos tachados y dos por delante situa la
    /// actividad; una sola frase suelta, no.
    ///
    /// POR QUE SE ENGANCHA AL EXITO Y NO AL CLIC
    /// ----------------------------------------
    /// El maniqui y el lavamanos pasan por ValidadorSecuencia, que distingue
    /// AlTenerExito de AlTenerError. Si los pasos se marcaran al hacer clic, un
    /// clic fuera de orden daria por cumplido un paso que no se hizo, y se lo
    /// diria justo al participante que mas perdido esta. Por eso Cumplir() se
    /// cablea en AlTenerExito, nunca en el m_SelectEntered del interactable.
    ///
    /// POR QUE CADA DISPARADOR DICE SU NUMERO
    /// -------------------------------------
    /// Cumplir recibe el indice del paso en vez de limitarse a avanzar. Dos
    /// razones: un disparador que se dispare dos veces no adelanta la lista, y
    /// los pasos que no estan encadenados entre si (el video y las gafas no se
    /// bloquean el uno al otro) se pueden hacer en cualquier orden sin que la
    /// lista quede descolocada.
    ///
    /// POR QUE NO CUELGA DE LA CAMARA
    /// -----------------------------
    /// GuiaDeLaMarca si lo hace, y para un aviso de unos segundos esta bien. Un
    /// panel permanente soldado a la vista cansa: se mueve con cada microgiro de
    /// la cabeza y no se deja leer.
    ///
    /// Hay dos maneras de resolverlo y las dos estan implementadas, porque no
    /// sirven para lo mismo:
    ///
    /// - FijoMirandoAlUsuario, que es la de serie. El panel es un tablero
    ///   clavado en un punto de la sala que solo gira en yaw para quedar de
    ///   frente a quien lo mira. Es el comportamiento del tablero de
    ///   descripciones del pasteurizador (PasteurizerWorldCanvas, modo
    ///   AnchorFacingUser) y se eligio por lo mismo: al no desplazarse no
    ///   estorba el paso ni tapa el equipo, y al girar se lee desde cualquier
    ///   punto. El precio es que el participante tiene que saber donde esta el
    ///   tablero, y eso lo paga la locucion de entrada a la zona.
    ///
    /// - SigueLaMirada, el comportamiento anterior. El panel se queda quieto
    ///   mientras la cabeza se mueve poco y solo cuando el giro pasa de
    ///   `holgura` va a buscar la mirada, con retardo. Sirve en zonas donde no
    ///   hay un punto franco donde colgar un tablero.
    ///
    /// POR QUE LA LETRA SE MIDE EN PIXELES Y NO EN METROS
    /// -------------------------------------------------
    /// El lienzo es un Canvas en World Space, asi que lo que se ve depende de
    /// dos numeros multiplicados: `tamanoDeLetra`, en pixeles de la textura, y
    /// `escala`, los metros que mide cada pixel. El alto real de la letra es el
    /// producto de los dos, y de ahi sale la regla practica: un tablero que se
    /// lee a cinco metros necesita una letra de unos ocho centimetros, porque
    /// por debajo de medio grado de angulo visual el texto deja de resolverse
    /// con las gafas puestas. El resto de las medidas de la placa se derivan de
    /// `tamanoDeLetra`, de modo que subirla ensancha el tablero en vez de
    /// partir las lineas.
    ///
    /// LO QUE NO SE HACE AQUI, Y POR QUE
    /// --------------------------------
    /// No se toca ningun material en tiempo de ejecucion. El proyecto renderiza
    /// en Single Pass Instanced y ya hubo un disco que se veia en el editor y no
    /// en el visor por manipular un material a mano (ver GuiaDeLaMarca). Todo lo
    /// que se dibuja aqui es una textura generada y el shader de interfaz, que
    /// es el camino del que tenemos constancia de que funciona con las gafas
    /// puestas.
    /// </summary>
    [AddComponentMenu("ViroLab/Guía/Guion de pasos")]
    public class GuionDePasos : MonoBehaviour, IReiniciable
    {
        [Serializable]
        public class Paso
        {
            [Tooltip("Lo que lee el participante. En imperativo y corto.")]
            [TextArea(1, 2)]
            public string texto;

            [Tooltip("Sangra la línea. Para los apartados de un paso con varias partes.")]
            public bool sangrado;
        }

        /// <summary>Dónde se pone el panel y cómo se comporta al moverse el usuario.</summary>
        public enum Colocacion
        {
            /// <summary>Tablero clavado en un punto de la sala que gira para mirar al usuario.</summary>
            FijoMirandoAlUsuario,

            /// <summary>Acompaña la mirada con holgura y retardo. El comportamiento anterior.</summary>
            SigueLaMirada
        }

        // ================================================================
        //  Lo que se configura
        // ================================================================

        [Header("Contenido")]
        [Tooltip("Encabezado del panel. Dejar vacío para no mostrarlo.")]
        public string titulo = "ZONA DE ALISTAMIENTO";

        public List<Paso> pasos = new List<Paso>();

        [Header("Colocación")]
        [Tooltip("Fijo en la sala girando hacia el usuario, o acompañando la mirada.")]
        public Colocacion colocacion = Colocacion.FijoMirandoAlUsuario;

        [Tooltip("Metros que mide cada píxel del lienzo. El alto de la letra es " +
                 "este número por el tamaño de letra.")]
        public float escala = 0.0018f;

        [Header("Fijo mirando al usuario")]
        [Tooltip("Dónde se clava el tablero. Si se deja vacío, se usa este mismo " +
                 "objeto: basta con moverlo en la escena.")]
        public Transform ancla;

        [Tooltip("Metros respecto al ancla. La Y es la altura del centro del tablero.")]
        public Vector3 desplazamientoDelAncla = new Vector3(0f, 1.80f, 0f);

        [Tooltip("Velocidad del giro hacia el usuario. 0 lo deja instantáneo.")]
        [Range(0f, 20f)] public float giroSuavizado = 6f;

        [Header("Sigue la mirada")]
        [Tooltip("Metros por delante de los ojos.")]
        public float distancia = 1.2f;

        [Tooltip("Metros respecto al eje visual. Negativo baja el panel.")]
        public float altura = -0.30f;

        [Tooltip("Grados que puede girar la cabeza antes de que el panel la siga.")]
        public float holgura = 14f;

        [Tooltip("Segundos que tarda el panel en alcanzar la mirada.")]
        public float suavizado = 0.45f;

        [Header("Letra")]
        [Tooltip("Alto de la letra en píxeles del lienzo. Las demás medidas de la " +
                 "placa se derivan de este número, así que subirlo ensancha el " +
                 "tablero en vez de partir las líneas.")]
        public float tamanoDeLetra = 44f;

        [Header("Marcas")]
        // Estos dos son campos y no constantes a proposito. La fuente por
        // defecto de TextMeshPro no siempre trae estos dos signos en su atlas, y
        // cuando falta uno se dibuja un cuadrito vacio. Si eso pasa, se cambian
        // aqui por "OK" y ">" sin tocar codigo.
        [Tooltip("Signo del paso ya hecho. Si sale un cuadrito vacío, ponga OK.")]
        public string marcaHecho = "✓";

        [Tooltip("Signo del paso actual. Si sale un cuadrito vacío, ponga >.")]
        public string marcaActual = "▶";

        [Tooltip("Marca de los pasos que quedaron sin hacer cuando el reto se "
                 + "cerró. No dice que estén hechos: dice que ya no se pueden hacer.")]
        public string marcaOmitido = "—";

        [Header("Pasos saltados")]
        [Tooltip("Marcado: cumplir un paso da por hechos todos los anteriores. "
                 + "Sirve para los retos cuyos pasos van en el orden físico de la "
                 + "zona, como el 1: el estudiante que se salta el video y va al "
                 + "casillero ve el tablero al día y sabe qué sigue. "
                 + "Desmárquelo donde los pasos sean actividades independientes: "
                 + "en el reto 2, ir derecho al cuestionario daría por explorados "
                 + "el carrusel y el pasteurizador, y eso es afirmar algo que no "
                 + "ocurrió.")]
        public bool cumplirMarcaLosAnteriores = true;

        [Header("Al empezar")]
        [Tooltip("Marcado: el panel no se enciende solo; espera a que algo llame " +
                 "a Mostrar(). Es lo que hace falta en los retos 2 y 3, que no " +
                 "empiezan cuando empieza la experiencia.")]
        public bool empezarOculto;

        [Header("Al terminar")]
        public bool ocultarAlTerminar = true;

        [Tooltip("Segundos que se deja ver la lista completa antes de apagarla.")]
        public float segundosAntesDeOcultar = 4f;

        [Tooltip("Qué ocurre cuando se cumplen todos los pasos.")]
        public UnityEvent AlTerminar;

        public bool log = true;

        // ================================================================
        //  Estado
        // ================================================================

        private bool[] _hechos;
        private bool _visible;
        private bool _yaTermine;
        private float _apagarEn = -1f;

        private Canvas _lienzo;
        private Image _placa;
        private Texture2D _placaTextura;
        private Sprite _placaSprite;
        private TextMeshProUGUI _texto;

        private Transform _camara;
        private float _giro;
        private float _velGiro;
        private Vector3 _velPos;
        private bool _persiguiendo;
        private bool _reciencolocado;

        // Con que medidas se construyo la placa que hay en pantalla. Sirven para
        // rehacerla si se cambia el tamano de letra con la experiencia en marcha,
        // que es como se ajusta esto de verdad: con las gafas puestas y mirando
        // el tablero desde donde va a estar el participante.
        // Reto dado por terminado. Lo pone Cerrar() y lo suelta ReiniciarAlInicio().
        private bool _cerrado;

        // Pasos que el reto dejo atras sin que nadie los cumpliera. Se lleva en un
        // arreglo aparte y no como un tercer valor de _hechos para no tocar nada de
        // lo que ya funciona: _hechos sigue significando "hecho" y solo eso.
        private bool[] _omitidos;
        private float _letraConstruida = -1f;
        private int _pasosConstruidos = -1;

        // Si alguien ya pidió encender este panel. Solo cuenta cuando
        // `empezarOculto` está marcado.
        private bool _yaMeEncendieron;

        // Medidas del lienzo en pixeles. El alto se calcula al construir, segun
        // cuantos pasos haya: una lista de tres y una de ocho no piden la misma
        // placa.
        //
        // Las cuatro se derivan de `tamanoDeLetra` y no son constantes a
        // proposito. Si el ancho se quedara fijo, subir la letra partiria las
        // lineas largas en dos; derivandolo, el tablero se ensancha con la letra
        // y la lista sigue cabiendo en un renglon por paso. El factor 25 sale de
        // medir el paso mas largo de la zona de alistamiento: cuarenta y un
        // caracteres mas la marca, que a media anchura de letra piden unas
        // veintidos letras de espacio, y el resto es el margen lateral.
        private float Ancho => tamanoDeLetra * 25f;
        private float MargenVertical => tamanoDeLetra * 1.8f;
        private float AltoDeLinea => tamanoDeLetra * 1.4f;
        private float AltoDelTitulo => tamanoDeLetra * 2f;

        // Radio de las esquinas de la placa, en píxeles de la textura. Es
        // también el borde de los nueve trozos, de modo que las esquinas se
        // dibujan a este tamaño sea cual sea lo que mida el panel.
        private const float RadioDeLaPlaca = 36f;

        // ================================================================
        //  Ciclo
        // ================================================================

        private void Awake()
        {
            _hechos = new bool[pasos.Count];
        }

        private void OnEnable()
        {
            // Los retos 2 y 3 no empiezan cuando empieza la experiencia. Su panel
            // espera a que el grupo llegue a la zona, y quien lo enciende es el
            // AvanceEnGrupo del reto anterior, llamando a Mostrar() en
            // DespuesDelSalto. Sin esto, los tres paneles se encenderian a la vez
            // en el minuto cero y el participante veria la lista de una zona en
            // la que todavia no esta.
            if (empezarOculto && !_yaMeEncendieron) return;
            Mostrar();
        }

        private void LateUpdate()
        {
            if (_apagarEn > 0f && Time.unscaledTime >= _apagarEn)
            {
                _apagarEn = -1f;
                Ocultar();
                return;
            }

            if (!_visible || _lienzo == null) return;

            RehacerSiCambioLaMedida();
            if (_lienzo == null) return;

            var cam = Camara();
            if (cam == null) return;

            _lienzo.transform.localScale = Vector3.one * escala;

            if (colocacion == Colocacion.FijoMirandoAlUsuario)
            {
                ColocarFijoMirandoAlUsuario(cam);
                return;
            }

            // El panel solo va a buscar la mirada cuando el giro de la cabeza
            // pasa de la holgura. Mientras tanto se queda donde esta, que es lo
            // que lo hace legible: un texto que se mueve no se lee.
            float objetivo = cam.eulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(_giro, objetivo)) > holgura) _persiguiendo = true;

            if (_persiguiendo)
            {
                _giro = Mathf.SmoothDampAngle(_giro, objetivo, ref _velGiro, suavizado);
                if (Mathf.Abs(Mathf.DeltaAngle(_giro, objetivo)) < 0.5f) _persiguiendo = false;
            }

            var rot = Quaternion.Euler(0f, _giro, 0f);
            Vector3 meta = cam.position + rot * Vector3.forward * distancia + Vector3.up * altura;

            if (_reciencolocado)
            {
                // Al encenderlo no se le ve venir desde el origen del mundo.
                _reciencolocado = false;
                _velPos = Vector3.zero;
                _lienzo.transform.position = meta;
            }
            else
            {
                _lienzo.transform.position = Vector3.SmoothDamp(
                    _lienzo.transform.position, meta, ref _velPos, suavizado * 0.5f);
            }

            _lienzo.transform.rotation = rot;
        }

        /// <summary>
        /// Posición fija, la del ancla, con el tablero girado para quedar de
        /// frente a quien lo mira. Es el mismo convenio que usa el tablero de
        /// descripciones del pasteurizador: el forward del Canvas apunta en
        /// contra del observador, que es como se lee de frente una interfaz en
        /// World Space.
        ///
        /// Solo gira en yaw. El vector hacia el usuario se aplana en el plano
        /// horizontal para que el tablero se mantenga vertical y no se incline
        /// cuando el participante mira hacia arriba o se agacha.
        /// </summary>
        private void ColocarFijoMirandoAlUsuario(Transform cam)
        {
            var baseAncla = ancla != null ? ancla : transform;

            // Se respeta la orientacion del ancla pero no su escala. El tablero
            // de descripciones usa TransformPoint, que si la aplica, y ahi no
            // estorba porque su ancla vive sin escalar. Aqui el ancla puede
            // acabar colgada de Planta_Piloto, que esta a 0,6, y entonces una
            // altura de 1,80 se convertiria en 1,08 sin que nada lo avise.
            Vector3 meta = baseAncla.position + baseAncla.rotation * desplazamientoDelAncla;
            _lienzo.transform.position = meta;

            Vector3 contra = meta - cam.position;
            contra.y = 0f;

            // El usuario, justo encima o debajo del tablero: no hay yaw que
            // calcular, y conservar la orientacion anterior evita un giro brusco.
            if (contra.sqrMagnitude < 0.0001f) return;

            var objetivo = Quaternion.LookRotation(contra.normalized, Vector3.up);
            _lienzo.transform.rotation = giroSuavizado > 0f && !_reciencolocado
                ? Quaternion.Slerp(_lienzo.transform.rotation, objetivo,
                                   Time.deltaTime * giroSuavizado)
                : objetivo;
            _reciencolocado = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Dibuja el tablero a tamaño real en la vista de escena, para poder
        /// colocarlo sin entrar en el visor. Sin esto hay que dar Play, mirar,
        /// salir y corregir, y cada vuelta cuesta un par de minutos.
        ///
        /// Se dibuja con la orientación del ancla y no mirando a la cámara de la
        /// escena: en ejecución el tablero gira en yaw, así que lo que importa
        /// aquí es dónde se planta, cuánto ocupa y a qué altura llega, no hacia
        /// dónde apunta.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (colocacion != Colocacion.FijoMirandoAlUsuario) return;
            if (pasos == null || pasos.Count == 0) return;

            var baseAncla = ancla != null ? ancla : transform;
            Vector3 centro = baseAncla.position + baseAncla.rotation * desplazamientoDelAncla;

            float ancho = Ancho * escala;
            float alto = AltoDelPanel() * escala;
            float letra = tamanoDeLetra * escala;

            // El poste imaginario: de dónde cuelga y hasta dónde sube.
            Gizmos.color = new Color(1f, 0.76f, 0.29f, 0.9f);
            Gizmos.DrawLine(baseAncla.position, centro);
            Gizmos.DrawSphere(baseAncla.position, 0.06f);

            var previa = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(centro, baseAncla.rotation, Vector3.one);

            Gizmos.color = new Color(0.04f, 0.07f, 0.10f, 0.35f);
            Gizmos.DrawCube(Vector3.zero, new Vector3(ancho, alto, 0.01f));
            Gizmos.color = new Color(1f, 0.76f, 0.29f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(ancho, alto, 0.01f));

            // Una letra, a tamaño real, en el borde izquierdo. Es la medida que
            // de verdad decide si se lee desde el otro extremo de la zona.
            Gizmos.color = new Color(0.44f, 0.81f, 0.59f, 1f);
            float x = -ancho * 0.5f - letra * 0.6f;
            Gizmos.DrawLine(new Vector3(x, -letra * 0.5f, 0f), new Vector3(x, letra * 0.5f, 0f));

            Gizmos.matrix = previa;
        }
#endif

        private void OnDestroy()
        {
            if (_lienzo != null) Destroy(_lienzo.gameObject);
            if (_placaSprite != null) Destroy(_placaSprite);
            if (_placaTextura != null) Destroy(_placaTextura);
        }

        // ================================================================
        //  API
        // ================================================================

        /// <summary>
        /// Da por cumplido el paso número <paramref name="indice"/>, contando
        /// desde 0. Es lo que se cablea en el Inspector.
        /// </summary>
        public void Cumplir(int indice)
        {
            if (_hechos == null || _hechos.Length != pasos.Count)
                _hechos = new bool[pasos.Count];

            if (indice < 0 || indice >= _hechos.Length)
            {
                Debug.LogError($"[GuionDePasos] Se pidió cumplir el paso {indice} y la lista " +
                               $"tiene {_hechos.Length}. Revise el número en el Inspector.", this);
                return;
            }

            if (_hechos[indice]) return;
            _hechos[indice] = true;
            if (_omitidos != null && indice < _omitidos.Length) _omitidos[indice] = false;

            if (cumplirMarcaLosAnteriores) DarPorHechosLosAnteriores(indice);

            if (log)
            {
                string t = pasos[indice] != null ? pasos[indice].texto : "(sin texto)";
                Debug.Log($"[GuionDePasos] Paso {indice} cumplido: {t}", this);
            }

            Mostrar();
            Repintar();

            if (!_yaTermine && TodoHecho())
            {
                _yaTermine = true;
                if (log) Debug.Log("[GuionDePasos] Lista completa.", this);
                AlTerminar?.Invoke();
                if (ocultarAlTerminar)
                    _apagarEn = Time.unscaledTime + Mathf.Max(0f, segundosAntesDeOcultar);
            }
        }

        /// <summary>Enciende el panel.</summary>
        public void Mostrar()
        {
            if (pasos == null || pasos.Count == 0) return;
            if (_cerrado) return;

            _yaMeEncendieron = true;

            Construir();
            RehacerSiCambioLaMedida();
            if (_lienzo == null) return;

            if (!_visible)
            {
                _visible = true;
                _reciencolocado = true;
                _persiguiendo = false;
                var cam = Camara();
                _giro = cam != null ? cam.eulerAngles.y : 0f;
            }

            _lienzo.gameObject.SetActive(true);
            Repintar();
        }

        /// <summary>Apaga el panel sin olvidar lo que ya está cumplido.</summary>
        public void Ocultar()
        {
            _visible = false;
            _apagarEn = -1f;
            if (_lienzo != null) _lienzo.gameObject.SetActive(false);
        }

        /// <summary>
        /// Da el reto por terminado: apaga el panel y lo deja apagado.
        ///
        /// Hace falta porque Ocultar() no basta. Los disparadores de un reto
        /// siguen ahí cuando el grupo ya se fue al siguiente: el pasteurizador y
        /// el carrusel del reto 2 siguen siendo pulsables durante el reto 3, y
        /// Cumplir llama a Mostrar(), de modo que un panel apagado con Ocultar()
        /// reaparecería en cuanto el estudiante toque uno de ellos.
        ///
        /// Los pasos se siguen apuntando: lo que se apaga es el panel, no el
        /// registro. Así da igual en qué orden se invoquen las filas de un mismo
        /// evento, porque Cumplir y Cerrar ya no se estorban.
        /// </summary>
        public void Cerrar()
        {
            if (_cerrado) return;
            _cerrado = true;

            MarcarLosOmitidos();
            Repintar();

            // Si el panel esta a la vista se queda los segundos configurados, para
            // que el estudiante alcance a leer lo que se salto. Si estaba apagado
            // no hay por que encenderlo ahora: el reto ya termino.
            if (_visible && segundosAntesDeOcultar > 0f)
                _apagarEn = Time.unscaledTime + segundosAntesDeOcultar;
            else
                Ocultar();
        }

        /// <summary>
        /// Da por hechos los pasos anteriores al que se acaba de cumplir.
        ///
        /// Sin esto, el estudiante que se salta un paso ve el tablero congelado:
        /// el paso saltado se queda resaltado como «el que toca» aunque ya haya
        /// pasado de largo, y el que de verdad le toca no se distingue. Con esto
        /// el tablero avanza con el, que es para lo que esta puesto.
        ///
        /// Tiene un coste y por eso es opcional: afirma que el estudiante hizo
        /// algo que pudo no hacer.
        /// </summary>
        private void DarPorHechosLosAnteriores(int indice)
        {
            for (int i = 0; i < indice && i < _hechos.Length; i++)
            {
                if (_hechos[i]) continue;
                _hechos[i] = true;
                if (_omitidos != null && i < _omitidos.Length) _omitidos[i] = false;

                if (log)
                {
                    string t = pasos[i] != null ? pasos[i].texto : "(sin texto)";
                    Debug.Log($"[GuionDePasos] Paso {i} dado por hecho al cumplirse el " +
                              $"{indice}: {t}", this);
                }
            }
        }

        /// <summary>
        /// Apunta como omitido todo lo que quedo sin cumplir. No los da por
        /// hechos: en el reto 1 el video y las gafas no son requisito de los
        /// pasos siguientes, de modo que un estudiante puede terminar el reto sin
        /// haberlos tocado, y decir que los hizo seria falso.
        /// </summary>
        private void MarcarLosOmitidos()
        {
            if (_hechos == null || _hechos.Length != pasos.Count)
                _hechos = new bool[pasos.Count];
            if (_omitidos == null || _omitidos.Length != pasos.Count)
                _omitidos = new bool[pasos.Count];

            int cuantos = 0;
            for (int i = 0; i < pasos.Count; i++)
            {
                if (_hechos[i] || _omitidos[i]) continue;
                _omitidos[i] = true;
                cuantos++;
                if (log)
                {
                    string t = pasos[i] != null ? pasos[i].texto : "(sin texto)";
                    Debug.Log($"[GuionDePasos] Paso {i} sin hacer al cerrar el reto: {t}", this);
                }
            }

            if (log && cuantos == 0)
                Debug.Log("[GuionDePasos] Reto cerrado con todos los pasos hechos.", this);
        }

        /// <summary>
        /// Deja la lista como al abrir la experiencia. Lo llama
        /// ReinicioDeExperiencia entre una tanda y la siguiente.
        /// </summary>
        public void ReiniciarAlInicio()
        {
            _hechos = new bool[pasos.Count];
            _omitidos = new bool[pasos.Count];
            _yaTermine = false;
            _apagarEn = -1f;
            _yaMeEncendieron = false;
            _cerrado = false;
            if (empezarOculto) Ocultar();
            // Se repinta pero no se enciende: el reinicio corre con la pantalla
            // en negro, y lo que deba verse al llegar a la zona se enciende al
            // llegar a la zona.
            Repintar();
        }

        // ================================================================
        //  Interfaz
        // ================================================================

        private Transform Camara()
        {
            if (_camara == null && Camera.main != null) _camara = Camera.main.transform;
            return _camara;
        }

        private float AltoDelPanel()
        {
            float alto = MargenVertical * 2f + pasos.Count * AltoDeLinea;
            if (!string.IsNullOrWhiteSpace(titulo)) alto += AltoDelTitulo;
            return alto;
        }

        /// <summary>
        /// Rehace la placa si cambiaron las medidas con las que se construyó.
        /// Se llama desde LateUpdate para que el ajuste del tamaño de letra se
        /// vea sin salir del modo de juego.
        /// </summary>
        private void RehacerSiCambioLaMedida()
        {
            if (_lienzo == null) return;
            if (Mathf.Approximately(_letraConstruida, tamanoDeLetra)
                && _pasosConstruidos == pasos.Count) return;

            // El sprite y la textura se generan en Construir, asi que hay que
            // soltar los anteriores. Sin esto, cada ajuste del tamano de letra
            // deja una textura huerfana en memoria.
            Destroy(_lienzo.gameObject);
            if (_placaSprite != null) Destroy(_placaSprite);
            if (_placaTextura != null) Destroy(_placaTextura);
            _lienzo = null;
            _placa = null;
            _texto = null;
            _placaSprite = null;
            _placaTextura = null;
            Construir();
            if (_lienzo != null)
            {
                _lienzo.gameObject.SetActive(_visible);
                Repintar();
            }
        }

        private void Construir()
        {
            if (_lienzo != null) return;

            var obj = new GameObject("Guion_De_Pasos");
            obj.hideFlags = HideFlags.DontSave;

            _lienzo = obj.AddComponent<Canvas>();
            _lienzo.renderMode = RenderMode.WorldSpace;
            // Por encima del cartel de la marca (500): si coinciden en pantalla,
            // lo que manda es saber que hay que hacer.
            _lienzo.sortingOrder = 600;

            float alto = AltoDelPanel();

            // La placa de fondo. Sin ella el texto compite con el suelo claro y
            // las paredes oscuras de la planta, y en el video de streaming se
            // pierde del todo.
            var fondo = new GameObject("Placa");
            fondo.transform.SetParent(obj.transform, false);
            _placaTextura = DibujarPlaca();
            // En nueve trozos: la textura es cuadrada y pequeña, y el panel es
            // ancho y de alto variable. Estirándola sin más, las esquinas
            // redondeadas saldrían ovaladas; con el borde declarado, Unity
            // conserva las cuatro esquinas y estira solo los tramos rectos.
            _placaSprite = Sprite.Create(
                _placaTextura,
                new Rect(0f, 0f, _placaTextura.width, _placaTextura.height),
                new Vector2(0.5f, 0.5f),
                100f, 0u, SpriteMeshType.FullRect,
                new Vector4(RadioDeLaPlaca, RadioDeLaPlaca, RadioDeLaPlaca, RadioDeLaPlaca));
            _placaSprite.name = "Guion_Placa_Sprite";
            _placaSprite.hideFlags = HideFlags.DontSave;

            _placa = fondo.AddComponent<Image>();
            _placa.sprite = _placaSprite;
            _placa.type = Image.Type.Sliced;
            _placa.color = new Color(0.04f, 0.07f, 0.10f, 0.82f);
            _placa.raycastTarget = false;
            _placa.rectTransform.sizeDelta = new Vector2(Ancho, alto);

            var txt = new GameObject("Texto");
            txt.transform.SetParent(obj.transform, false);
            _texto = txt.AddComponent<TextMeshProUGUI>();
            _texto.alignment = TextAlignmentOptions.TopLeft;
            _texto.fontSize = tamanoDeLetra;
            _texto.lineSpacing = tamanoDeLetra * 0.45f;
            _texto.enableWordWrapping = true;
            _texto.raycastTarget = false;
            _texto.color = Color.white;
            _texto.rectTransform.sizeDelta = new Vector2(
                Ancho - tamanoDeLetra * 2.3f, alto - tamanoDeLetra * 1.4f);

            _letraConstruida = tamanoDeLetra;
            _pasosConstruidos = pasos.Count;

            _texto.fontMaterial.EnableKeyword("OUTLINE_ON");
            _texto.outlineWidth = 0.18f;
            _texto.outlineColor = new Color32(0, 0, 0, 255);
        }

        /// <summary>
        /// Un rectángulo de esquinas redondeadas, en blanco, para teñirlo
        /// después con el color de la placa. Va en blanco entero y lo que varía
        /// es el alfa: así el tinte se aplica sobre la imagen y cambiar de color
        /// no obliga a volver a generar la textura, igual que en GuiaDeLaMarca.
        /// </summary>
        private static Texture2D DibujarPlaca()
        {
            const int lado = 128;
            const float radio = RadioDeLaPlaca;
            const float suave = 1.5f;

            var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
            {
                name = "Guion_Placa_Textura",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixeles = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    // Distancia al rectángulo interior; dentro de él es 0.
                    float dx = Mathf.Max(radio - x - 0.5f, (x + 0.5f) - (lado - radio), 0f);
                    float dy = Mathf.Max(radio - y - 0.5f, (y + 0.5f) - (lado - radio), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float alfa = 1f - Mathf.SmoothStep(0f, 1f, (d - (radio - suave)) / suave);
                    pixeles[y * lado + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alfa) * 255f));
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply(false, false);
            return tex;
        }

        private int PrimeroSinHacer()
        {
            if (_hechos == null) return 0;
            for (int i = 0; i < _hechos.Length; i++)
                if (!_hechos[i]) return i;
            return -1;
        }

        private bool TodoHecho()
        {
            return PrimeroSinHacer() < 0;
        }

        private void Repintar()
        {
            if (_texto == null) return;
            if (_hechos == null || _hechos.Length != pasos.Count)
                _hechos = new bool[pasos.Count];
            if (_omitidos == null || _omitidos.Length != pasos.Count)
                _omitidos = new bool[pasos.Count];

            int actual = PrimeroSinHacer();
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(titulo))
                sb.Append("<size=80%><color=#FFFFFF>").Append(titulo).Append("</color></size>\n\n");

            for (int i = 0; i < pasos.Count; i++)
            {
                string t = pasos[i] != null ? pasos[i].texto : string.Empty;
                string sangria = pasos[i] != null && pasos[i].sangrado ? "    " : string.Empty;

                if (_hechos[i])
                    sb.Append("<color=#6FCF97>").Append(marcaHecho).Append("</color> ")
                      .Append(sangria).Append("<color=#7E939F>").Append(t).Append("</color>");
                // El omitido va antes que el "en curso" a proposito: al cerrarse el
                // reto, PrimeroSinHacer() sigue senalando el primer paso sin hacer,
                // que ahora es uno omitido, y sin esta rama saldria resaltado en
                // blanco como si todavia hubiera algo que hacer.
                else if (_omitidos[i])
                    sb.Append("<color=#6B7A84>").Append(marcaOmitido).Append("</color> ")
                      .Append(sangria).Append("<color=#6B7A84>").Append(t).Append("</color>");
                else if (i == actual)
                    sb.Append("<color=#FFC24B>").Append(marcaActual).Append("</color> ")
                      .Append(sangria).Append("<b><color=#FFFFFF>").Append(t).Append("</color></b>");
                else
                    sb.Append("<color=#5C6B75>·</color> ")
                      .Append(sangria).Append("<color=#93A3AD>").Append(t).Append("</color>");

                if (i < pasos.Count - 1) sb.Append('\n');
            }

            _texto.text = sb.ToString();
        }
    }
}
