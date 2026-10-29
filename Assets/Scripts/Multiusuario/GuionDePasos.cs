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
    /// la cabeza y no se deja leer. Aqui el panel se queda quieto mientras la
    /// cabeza se mueve poco, y solo cuando el giro pasa de `holgura` va a buscar
    /// la mirada, con retardo.
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

        // ================================================================
        //  Lo que se configura
        // ================================================================

        [Header("Contenido")]
        [Tooltip("Encabezado del panel. Dejar vacío para no mostrarlo.")]
        public string titulo = "ZONA DE ALISTAMIENTO";

        public List<Paso> pasos = new List<Paso>();

        [Header("Colocación")]
        [Tooltip("Metros por delante de los ojos.")]
        public float distancia = 1.2f;

        [Tooltip("Metros respecto al eje visual. Negativo baja el panel.")]
        public float altura = -0.30f;

        [Tooltip("Tamaño del panel. Súbalo si no se lee, bájelo si tapa demasiado.")]
        public float escala = 0.00075f;

        [Header("Seguimiento")]
        [Tooltip("Grados que puede girar la cabeza antes de que el panel la siga.")]
        public float holgura = 14f;

        [Tooltip("Segundos que tarda el panel en alcanzar la mirada.")]
        public float suavizado = 0.45f;

        [Header("Marcas")]
        // Estos dos son campos y no constantes a proposito. La fuente por
        // defecto de TextMeshPro no siempre trae estos dos signos en su atlas, y
        // cuando falta uno se dibuja un cuadrito vacio. Si eso pasa, se cambian
        // aqui por "OK" y ">" sin tocar codigo.
        [Tooltip("Signo del paso ya hecho. Si sale un cuadrito vacío, ponga OK.")]
        public string marcaHecho = "✓";

        [Tooltip("Signo del paso actual. Si sale un cuadrito vacío, ponga >.")]
        public string marcaActual = "▶";

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

        // Medidas del lienzo en pixeles. El alto se calcula al construir, segun
        // cuantos pasos haya: una lista de tres y una de ocho no piden la misma
        // placa.
        private const float Ancho = 900f;
        private const float MargenVertical = 70f;
        private const float AltoDeLinea = 52f;
        private const float AltoDelTitulo = 76f;

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

            var cam = Camara();
            if (cam == null) return;

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
            _lienzo.transform.localScale = Vector3.one * escala;
        }

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

            Construir();
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
        /// Deja la lista como al abrir la experiencia. Lo llama
        /// ReinicioDeExperiencia entre una tanda y la siguiente.
        /// </summary>
        public void ReiniciarAlInicio()
        {
            _hechos = new bool[pasos.Count];
            _yaTermine = false;
            _apagarEn = -1f;
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
            _texto.fontSize = 36;
            _texto.lineSpacing = 18f;
            _texto.enableWordWrapping = true;
            _texto.raycastTarget = false;
            _texto.color = Color.white;
            _texto.rectTransform.sizeDelta = new Vector2(Ancho - 100f, alto - 60f);

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

            int actual = PrimeroSinHacer();
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(titulo))
                sb.Append("<size=80%><color=#8FA9BC>").Append(titulo).Append("</color></size>\n\n");

            for (int i = 0; i < pasos.Count; i++)
            {
                string t = pasos[i] != null ? pasos[i].texto : string.Empty;
                string sangria = pasos[i] != null && pasos[i].sangrado ? "    " : string.Empty;

                if (_hechos[i])
                    sb.Append("<color=#6FCF97>").Append(marcaHecho).Append("</color> ")
                      .Append(sangria).Append("<color=#7E939F>").Append(t).Append("</color>");
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
