using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Lo que el participante VE cuando se le pide ir a su marca: un aro en el
    /// suelo y un cartel delante con lo que le falta.
    ///
    /// POR QUE EXISTE
    /// --------------
    /// Las marcas son objetos vacios, o sea invisibles, y el aviso de que hay que
    /// ir a una de ellas dependia de cablear dos UnityEvent en el Inspector. En
    /// la primera prueba real eso se tradujo en un participante parado a 1,8 m de
    /// su marca durante 59 segundos, hasta que salto la espera maxima: el
    /// mecanismo funcionaba y no habia forma de saberlo desde dentro del visor.
    ///
    /// Un mecanismo que depende de que alguien se acuerde de cablear su parte
    /// visible falla en silencio. Asi que la parte visible se crea sola.
    ///
    /// POR QUE EL ARO SE DIBUJA COMO INTERFAZ Y NO COMO GEOMETRIA
    /// ---------------------------------------------------------
    /// En las pruebas del 29 de septiembre de 2026, con las gafas puestas, el
    /// cartel SI se veia y el aro NO. O sea que el componente corria: lo que
    /// fallaba era solo como se dibujaba el disco.
    ///
    /// El disco era un cilindro primitivo al que se le modificaba el material en
    /// tiempo de ejecucion, encendiendo keywords a mano para volverlo
    /// translucido. Eso expone dos riesgos que la vista del editor no tiene:
    ///
    ///   1. El proyecto renderiza en Single Pass Instanced (m_renderMode: 1 en
    ///      Assets/XR/Settings/Open XR Package Settings.asset). En ese modo el
    ///      material tiene que saber dibujarse en los dos ojos en una sola
    ///      pasada. Un material manipulado por codigo es justo lo que se ve bien
    ///      en la vista mono del editor y falla en estereo instanciado.
    ///   2. Las variantes de shader que solo se piden en tiempo de ejecucion se
    ///      pueden eliminar al compilar, porque ningun material del proyecto las
    ///      declara.
    ///
    /// El cartel no sufre ninguno de los dos problemas porque es un lienzo de
    /// interfaz, y de ese camino tenemos constancia directa de que funciona en el
    /// visor. Asi que el aro se construye igual: un Canvas en espacio de mundo
    /// tendido en el suelo, con una textura generada por codigo.
    ///
    /// Generar una TEXTURA es seguro; manipular un MATERIAL no lo es. Una textura
    /// son datos, no una variante de shader: no se puede eliminar al compilar y
    /// no depende del modo estereo. Y el shader de interfaz de Unity siempre
    /// viaja en la compilacion.
    /// </summary>
    public class GuiaDeLaMarca : MonoBehaviour
    {
        // Tonos suaves y translucidos, del orden de los resaltes del reto 1. Un
        // amarillo saturado y opaco sobre el suelo de la planta se comia la
        // escena y no se leia como una marca, sino como una mancha.
        private static readonly Color Lejos  = new Color(0.98f, 0.80f, 0.30f, 0.34f);
        private static readonly Color Encima = new Color(0.36f, 0.86f, 0.52f, 0.34f);

        // Lado de la textura del aro, en pixeles. 256 basta: el aro se mira desde
        // arriba y de cerca, y la imagen se suaviza con el filtrado bilineal.
        private const int LadoTextura = 256;

        private Canvas _aroLienzo;
        private RawImage _aroPintura;
        private Texture2D _aroTextura;

        private Canvas _lienzo;
        private TextMeshProUGUI _texto;

        public static GuiaDeLaMarca Crear()
        {
            var obj = new GameObject("Guía de la marca");
            obj.hideFlags = HideFlags.DontSave;
            return obj.AddComponent<GuiaDeLaMarca>();
        }

        // ================================================================
        //  Aro en el suelo
        // ================================================================

        /// <summary>
        /// Dibuja la marca en una textura: un aro grueso en el borde y un relleno
        /// muy tenue por dentro, para que se lea como un sitio donde ponerse y no
        /// como una mancha.
        ///
        /// El color va entero en blanco y lo que varia es el alfa. Asi el tinte
        /// se aplica despues sobre la imagen, y cambiar de amarillo a verde no
        /// obliga a volver a generar la textura.
        /// </summary>
        private static Texture2D DibujarAro()
        {
            var tex = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false)
            {
                name = "Aro_Marca_Textura",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixeles = new Color32[LadoTextura * LadoTextura];
            float centro = (LadoTextura - 1) * 0.5f;

            // Radios en fraccion del radio total. El aro ocupa del 78 % al 98 %,
            // y el 2 % que queda libre por fuera evita que el borde se corte
            // contra el filo de la textura y aparezca dentado.
            const float bordeExterior = 0.98f;
            const float bordeInterior = 0.78f;
            // Ancho del degradado de los bordes, para que no se vean escalonados.
            const float suavizado = 0.03f;
            // Lo de dentro del aro se insinua, no se pinta.
            const float alfaRelleno = 0.22f;

            for (int y = 0; y < LadoTextura; y++)
            {
                for (int x = 0; x < LadoTextura; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / centro;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    // Pertenencia al aro: sube al entrar por el borde interior y
                    // baja al salir por el exterior.
                    float dentro = Mathf.SmoothStep(0f, 1f, (r - (bordeInterior - suavizado)) / suavizado);
                    float fuera  = 1f - Mathf.SmoothStep(0f, 1f, (r - (bordeExterior - suavizado)) / suavizado);
                    float aro = Mathf.Clamp01(Mathf.Min(dentro, fuera));

                    // Relleno: todo lo que queda por dentro del borde interior.
                    float relleno = (1f - Mathf.SmoothStep(0f, 1f,
                                        (r - (bordeInterior - suavizado)) / suavizado)) * alfaRelleno;

                    float alfa = Mathf.Clamp01(Mathf.Max(aro, relleno));
                    pixeles[y * LadoTextura + x] = new Color32(255, 255, 255, (byte)(alfa * 255f));
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply(false, false);
            return tex;
        }

        private void PrepararAro(float radio)
        {
            if (_aroLienzo == null)
            {
                var obj = new GameObject("Aro_Marca");
                obj.hideFlags = HideFlags.DontSave;

                _aroLienzo = obj.AddComponent<Canvas>();
                _aroLienzo.renderMode = RenderMode.WorldSpace;
                // Por debajo del cartel, que tiene 500: si alguna vez se cruzan
                // en pantalla, manda el texto.
                _aroLienzo.sortingOrder = 400;

                var img = new GameObject("Disco");
                img.transform.SetParent(obj.transform, false);
                _aroTextura = DibujarAro();
                _aroPintura = img.AddComponent<RawImage>();
                _aroPintura.texture = _aroTextura;
                // Que no se coma el rayo del puntero ni el teletransporte. El
                // disco antiguo tenia este mismo problema y se resolvia
                // quitandole el collider.
                _aroPintura.raycastTarget = false;
                _aroPintura.rectTransform.sizeDelta = new Vector2(LadoTextura, LadoTextura);
            }

            // El lienzo mide LadoTextura unidades de lado, asi que la escala que
            // lo deja del diametro pedido es (2 * radio) / LadoTextura.
            _aroLienzo.transform.localScale = Vector3.one * ((radio * 2f) / LadoTextura);
            // Tendido boca arriba. Un Canvas mira hacia +Z, de modo que girarlo
            // 90 grados sobre X lo deja mirando al cielo.
            _aroLienzo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // ================================================================
        //  Cartel
        // ================================================================

        private void PrepararCartel()
        {
            var camara = Camera.main != null ? Camera.main.transform : null;

            if (_lienzo == null)
            {
                var obj = new GameObject("Cartel_Marca");
                obj.hideFlags = HideFlags.DontSave;
                _lienzo = obj.AddComponent<Canvas>();
                _lienzo.renderMode = RenderMode.WorldSpace;
                _lienzo.sortingOrder = 500;

                var txt = new GameObject("Texto");
                txt.transform.SetParent(obj.transform, false);
                _texto = txt.AddComponent<TextMeshProUGUI>();
                _texto.alignment = TextAlignmentOptions.Center;
                _texto.fontSize = 42;
                _texto.enableWordWrapping = true;
                _texto.color = Color.white;
                _texto.raycastTarget = false;
                _texto.rectTransform.sizeDelta = new Vector2(900f, 260f);

                // Un borde oscuro para que se lea sobre cualquier fondo de la
                // planta, que tiene zonas muy claras y muy oscuras.
                _texto.fontMaterial.EnableKeyword("OUTLINE_ON");
                _texto.outlineWidth = 0.25f;
                _texto.outlineColor = new Color32(0, 0, 0, 255);
            }

            if (camara != null && _lienzo.transform.parent != camara)
                _lienzo.transform.SetParent(camara, false);

            // Abajo y al frente: no tapa la escena y cae donde se mira al andar.
            _lienzo.transform.localPosition = new Vector3(0f, -0.45f, 2f);
            _lienzo.transform.localRotation = Quaternion.identity;
            _lienzo.transform.localScale = Vector3.one * 0.0016f;
        }

        // ================================================================
        //  API
        // ================================================================

        /// <summary>Enciende la guía sobre la marca indicada.</summary>
        public void Mostrar(Transform marca, float radio)
        {
            if (marca == null) { Ocultar(); return; }

            PrepararAro(radio);
            PrepararCartel();

            // 12 cm de alzada. Las marcas estan a y=0 y el suelo de la planta
            // no lo esta: con el disco a 3 cm quedaba enterrado y no se veia,
            // mientras que el haz de metro y medio si asomaba. De ahi que
            // pareciera que el disco no se creaba.
            _aroLienzo.transform.position = marca.position + Vector3.up * 0.12f;
            _aroLienzo.gameObject.SetActive(true);

            _lienzo.gameObject.SetActive(true);

            // Que nazca ya del color que toca, sin esperar al primer Actualizar.
            Pintar(Lejos);
        }

        /// <summary>
        /// Refresca lo que se lee. <paramref name="giro"/> son los grados que le
        /// faltan por girar; se ignora si no se exige orientación.
        /// </summary>
        public void Actualizar(float distancia, bool enLaMarca, bool exigeGiro, float giro, float tolerancia)
        {
            if (_texto == null) return;

            if (!enLaMarca)
            {
                Pintar(Lejos);
                _texto.text = $"Ve a la marca del suelo\n<size=70%>te faltan {distancia:F1} m</size>";
                return;
            }

            Pintar(Encima);

            if (exigeGiro && Mathf.Abs(giro) > tolerancia)
            {
                string lado = giro > 0f ? "la derecha" : "la izquierda";
                _texto.text = $"Gira hacia {lado}\n<size=70%>{Mathf.Abs(giro):F0}°</size>";
                return;
            }

            _texto.text = "Quédate aquí\n<size=70%>esperando al resto</size>";
        }

        /// El tinte va sobre la imagen, no sobre un material: la transparencia la
        /// resuelve el alfa que ya trae la textura por el shader de interfaz, que
        /// siempre viaja en la compilacion y funciona en estereo instanciado.
        private void Pintar(Color c)
        {
            if (_aroPintura != null) _aroPintura.color = c;
        }

        /// <summary>Apaga la guía. Se llama al anunciar y al reiniciar.</summary>
        public void Ocultar()
        {
            if (_aroLienzo != null) _aroLienzo.gameObject.SetActive(false);
            if (_lienzo != null) _lienzo.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_aroLienzo != null) Destroy(_aroLienzo.gameObject);
            if (_lienzo != null) Destroy(_lienzo.gameObject);
            if (_aroTextura != null) Destroy(_aroTextura);
        }
    }
}
