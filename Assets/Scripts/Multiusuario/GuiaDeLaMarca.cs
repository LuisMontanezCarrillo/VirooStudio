using UnityEngine;
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
    /// Se construye con primitivas y con el TextMeshPro que ya usa el proyecto,
    /// sin depender de ningun material ni prefab que haya que preparar antes.
    /// </summary>
    public class GuiaDeLaMarca : MonoBehaviour
    {
        // Tonos suaves y translucidos, del orden de los resaltes del reto 1. Un
        // amarillo saturado y opaco sobre el suelo de la planta se comia la
        // escena y no se leia como una marca, sino como una mancha.
        private static readonly Color Lejos  = new Color(0.98f, 0.80f, 0.30f, 0.34f);
        private static readonly Color Encima = new Color(0.36f, 0.86f, 0.52f, 0.34f);

        private Transform _aro;
        private Renderer _aroPintura;
        private Transform _centro;
        private Renderer _centroPintura;
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

        private void PrepararAro(float radio)
        {
            if (_aro == null)
            {
                var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disco.name = "Aro_Marca";
                disco.hideFlags = HideFlags.DontSave;

                // Sin esto el disco bloquea el rayo de teletransporte y el propio
                // paso del participante.
                var col = disco.GetComponent<Collider>();
                if (col != null) Destroy(col);

                _aroPintura = disco.GetComponent<Renderer>();
                if (_aroPintura != null)
                {
                    HacerTransparente(_aroPintura.material);
                    // Se conserva el material que trae la primitiva y solo se le
                    // cambia el color. Buscar un shader por nombre con Shader.Find
                    // funciona en el editor pero devuelve nulo en una compilacion
                    // si ese shader no esta en Always Included Shaders, y el aro
                    // saldria rosa justo el dia de la prueba.
                    _aroPintura.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _aroPintura.receiveShadows = false;
                }

                _aro = disco.transform;
            }

            // El cilindro de Unity mide 2 de alto y 1 de diametro, de ahi el radio
            // sin multiplicar y el 0,015 de grosor (3 cm de disco).
            _aro.localScale = new Vector3(radio * 2f, 0.015f, radio * 2f);

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
            _aro.position = marca.position + Vector3.up * 0.12f;
            _aro.gameObject.SetActive(true);

            if (_centro != null)
            {
                _centro.position = marca.position + Vector3.up * 0.125f;
                _centro.gameObject.SetActive(true);
            }

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

        private void Pintar(Color c)
        {
            if (_aroPintura != null) Tenir(_aroPintura.material, c);

            // El centro, mas oscuro pero con la MISMA transparencia: multiplicar
            // el color entero rebajaba tambien el alfa y el disco interior
            // desaparecia.
            if (_centroPintura != null)
                Tenir(_centroPintura.material, new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, c.a));
        }

        /// Pone el color en la propiedad que corresponda. URP usa _BaseColor y el
        /// pipeline integrado _Color; se escriben las dos que existan en vez de
        /// dar por hecho cual es.
        private static void Tenir(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        /// <summary>
        /// Pasa un material a mezcla alfa.
        ///
        /// Este proyecto renderiza con URP (Desktop Universal Render Pipeline
        /// Asset), de modo que las primitivas nacen con URP/Lit y no con el
        /// Standard del pipeline integrado. Se escriben las propiedades de los
        /// dos, comprobando antes que existan, para no depender de cual este
        /// activo ni de buscar ningun shader por nombre.
        /// </summary>
        private static void HacerTransparente(Material m)
        {
            if (m == null) return;

            // URP
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // 1 = Transparent
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);       // 0 = Alpha
            // Pipeline integrado
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);         // 3 = Transparent

            if (m.HasProperty("_SrcBlend"))
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend"))
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);

            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>Apaga la guía. Se llama al anunciar y al reiniciar.</summary>
        public void Ocultar()
        {
            if (_aro != null) _aro.gameObject.SetActive(false);
            if (_centro != null) _centro.gameObject.SetActive(false);
            if (_lienzo != null) _lienzo.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_aro != null) Destroy(_aro.gameObject);
            if (_centro != null) Destroy(_centro.gameObject);
            if (_lienzo != null) Destroy(_lienzo.gameObject);
        }
    }
}
