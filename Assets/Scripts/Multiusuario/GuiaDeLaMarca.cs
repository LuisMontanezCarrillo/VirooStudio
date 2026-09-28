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
        // Opacos a proposito: la transparencia depende del modo de mezcla del
        // material, y el de la primitiva es opaco. Un aro solido se ve igual de
        // bien y no depende de nada.
        private static readonly Color Lejos = new Color(1f, 0.72f, 0.15f);
        private static readonly Color Encima = new Color(0.25f, 0.9f, 0.4f);

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

            _aro.position = marca.position + Vector3.up * 0.03f;
            _aro.gameObject.SetActive(true);

            if (_centro != null)
            {
                _centro.position = marca.position + Vector3.up * 0.031f;
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
            if (_aroPintura != null && _aroPintura.material != null)
                _aroPintura.material.color = c;
            if (_centroPintura != null && _centroPintura.material != null)
                _centroPintura.material.color = c * 0.45f;
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
