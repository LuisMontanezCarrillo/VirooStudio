using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViroLab.Multiusuario;

namespace ViroLab.Pasteurizador.Simulator
{
    /// Conecta el motor de simulación con un Canvas UI.
    /// Cada Update lee del engine y actualiza textos / colores / sliders / estados.
    /// El builder Editor (PasteurizerSimDashboardBuilder) crea el Canvas y
    /// asigna automáticamente las referencias de este componente.
    [RequireComponent(typeof(Canvas))]
    public class PasteurizerSimDashboard : MonoBehaviour, IReiniciable
    {
        [Header("Motor")]
        public PasteurizerSimEngine engine;

        [Header("Topbar - Temp Calentamiento (grande)")]
        public TMP_Text heatDisplay;

        [Header("Secuencia (6 pasos)")]
        public Image[] stepDots = new Image[6];
        public TMP_Text[] stepLabels = new TMP_Text[6];

        [Header("Lecturas")]
        public TMP_Text heatTemp;
        public TMP_Text holdTemp;
        public TMP_Text outTemp;
        public TMP_Text holdTime;
        public TMP_Text psi;
        public TMP_Text pulmonLvl;

        [Header("Válvulas")]
        public TMP_Text vProdTxt;
        public TMP_Text vRetTxt;
        public TMP_Text vDesTxt;
        public TMP_Text trapStateTxt;

        [Header("Estado grande")]
        public TMP_Text alarmTxt;
        public TMP_Text cipTxt;

        [Header("Botones de control")]
        public Button energyBtn;
        public Button startBtn;
        public Button stopBtn;
        public Button modeBtn;
        public Button fillBtn;
        public Button hotPumpBtn;
        public Button coldPumpBtn;
        public Button milkPumpBtn;
        public Button resetBatchBtn;
        [Tooltip("Ventana EMPEZAR. Si queda vacio se busca en la escena al reiniciar.")]
        public PasteurizerStartOverlay startOverlay;

        [Header("Labels de botones (refrescamos texto ON/OFF)")]
        public TMP_Text energyLbl;
        public TMP_Text modeLbl;
        public TMP_Text fillLbl;
        public TMP_Text hotPumpLbl;
        public TMP_Text coldPumpLbl;
        public TMP_Text milkPumpLbl;

        [Header("Sliders")]
        public Slider targetSlider;
        public TMP_Text targetVal;
        public Slider flowSlider;
        public TMP_Text flowVal;

        [Header("Métricas de lote")]
        public TMP_Text mFilled;
        public TMP_Text mFinal;
        public TMP_Text mRecirc;
        public TMP_Text mEvap;
        public TMP_Text mLoss;
        public TMP_Text mYield;

        [Header("Niveles de tanques (fills 0..1)")]
        public Image tankInFill;
        public Image tankOutFill;
        public TMP_Text tankInVolTxt;
        public TMP_Text tankOutVolTxt;
        public Image pulmonFill;

        [Header("Hint al pie")]
        public TMP_Text hintTxt;

        // ====================================================================
        //  PLANT (diagrama SVG portado a UGUI)
        // ====================================================================
        [Header("Plant - Tanques")]
        public Image plantTankInFill;
        public Image plantTankOutFill;
        public Image plantPulmonFill;
        public TMP_Text plantTankInVolTxt;
        public TMP_Text plantTankOutVolTxt;
        public TMP_Text plantRetDisplay;

        [Header("Plant - Bombas (aspas que giran)")]
        public RectTransform pumpMilkBlade;
        public RectTransform pumpHotBlade;
        public float pumpRpsActive = 1.2f;  // vueltas/seg cuando ON

        [Header("Plant - Tuberías (cambian color/alpha al fluir)")]
        public Image pipeInlet;
        public Image[] pipeRawSegments;       // 1+ rects que componen pipeRaw
        public Image[] pipeToHeatSegments;
        public Image[] pipeToRetSegments;
        public Image[] pipeFromRetSegments;
        public Image[] pipeToCoolingSegments;
        public Image[] pipeOutSegments;
        public Image[] pipeReturnSegments;
        public Image[] pipeHotSupplySegments;
        public Image[] pipeHotReturnSegments;
        public Image[] pipeHotBackSegments;
        public Image[] pipeColdSegments;
        public Image[] pipeColdReturnSegments;
        public Image[] pipeMakeupSegments;
        public Image[] pipePulmonOutSegments;
        public Image plantRetFlowOverlay;     // serpentina

        [Header("Plant - Válvulas (handle rota 45° al abrir)")]
        public RectTransform valveProdHandle;
        public RectTransform valveRetHandle;
        public RectTransform valveDesHandle;
        public RectTransform valveFillHandle;
        public Image valveProdBody;
        public Image valveRetBody;
        public Image valveDesBody;
        public Image valveFillBody;

        [Header("Plant - Indicadores")]
        public Image flameIcon;
        public Image refriLed;
        public Image trapDrip;
        public RectTransform psiNeedle;
        public TMP_Text psiTextOnGauge;

        [Header("Plant - Colores tuberías")]
        public Color pipeOffColor    = new Color(0.30f, 0.34f, 0.38f, 1f);
        public Color pipeMilkColor   = new Color(0.96f, 0.95f, 0.86f, 1f); // crema
        public Color pipeMilkWarm    = new Color(1.00f, 0.72f, 0.48f, 1f); // naranja claro
        public Color pipeMilkHot     = new Color(1.00f, 0.48f, 0.30f, 1f); // naranja fuerte
        public Color pipeMilkCold    = new Color(0.40f, 0.75f, 1.00f, 1f); // azul claro
        public Color pipeWaterHot    = new Color(0.92f, 0.30f, 0.30f, 1f); // rojo
        public Color pipeWaterCold   = new Color(0.30f, 0.55f, 1.00f, 1f); // azul

        // Estado interno para animaciones
        private float _pumpMilkAngle = 0f;
        private float _pumpHotAngle  = 0f;

        [Header("Paleta")]
        public Color colorOk     = new Color(0.20f, 0.92f, 0.55f);
        public Color colorWarn   = new Color(1.00f, 0.75f, 0.10f);
        public Color colorAlarm  = new Color(1.00f, 0.30f, 0.30f);
        public Color colorOff    = new Color(0.45f, 0.50f, 0.55f);
        public Color colorActive = new Color(0.00f, 0.88f, 1.00f);
        public Color colorBtnOn  = new Color(0.00f, 0.55f, 0.85f);
        public Color colorBtnOff = new Color(0.16f, 0.20f, 0.25f);
        public Color colorBtnDis = new Color(0.10f, 0.12f, 0.14f);

        // ====================================================================
        //  Legibilidad y visibilidad (bloque A de los ajustes previos a las
        //  pruebas con estudiantes). Todo se resuelve en tiempo de ejecucion
        //  para no tener que reconstruir el tablero ni editar la escena, que
        //  es donde viven las referencias que el builder dejo asignadas.
        // ====================================================================

        [Header("Legibilidad")]
        [Tooltip("Tamaño mínimo de letra dentro de las zonas listadas abajo. El builder " +
                 "dejó las etiquetas de los botones en 11 pt, que a la escala del lienzo " +
                 "son 2 cm y resultaron ilegibles en las pruebas con docentes. Pon 0 para " +
                 "desactivar este ajuste y volver al tamaño original.")]
        public float minFontSize = 18f;

        [Tooltip("Zonas donde se aplica el tamaño mínimo, por nombre del contenedor. " +
                 "Fuera de ellas el texto conserva su tamaño: las celdas de la barra " +
                 "superior y los rótulos del diagrama están pegados a su sitio, y al " +
                 "agrandarlos se desbordan de su celda o se cruzan con las tuberías.")]
        public string[] zonasConTamanoMinimo = { "PanelControl", "PanelMetrics" };

        [Tooltip("Añadir un contorno claro a los rótulos del diagrama de planta. No " +
                 "cambia su tamaño; los separa visualmente de las tuberías que pasan " +
                 "por detrás, que es lo que impedía leerlos.")]
        public bool contornoEnRotulosDelDiagrama = true;

        [Tooltip("Tamaño mínimo para los rótulos del diagrama de planta. El constructor " +
                 "dejó los de las válvulas en 8 pt y los de las bombas en 9, que a la " +
                 "escala del lienzo son 1,44 y 1,62 cm: ilegibles a dos metros. Con 18 pt " +
                 "quedan en 3,24 cm, igual que el panel de control. Pon 0 para desactivarlo.")]
        public float minFontSizeDiagrama = 18f;

        [Tooltip("Separar en vertical los rótulos del diagrama que se solapen tras " +
                 "agrandarlos. El desplazamiento está acotado para que ninguno se aleje " +
                 "del equipo que nombra.")]
        public bool separarRotulosQueChocan = true;

        [Tooltip("Desplazamiento máximo, en unidades de lienzo, que se permite a un " +
                 "rótulo para dejar de solaparse con otro.")]
        public float separacionMaxima = 26f;

        [Tooltip("Nombre del contenedor del diagrama de planta.")]
        public string zonaDelDiagrama = "PlantArea";

        [Tooltip("Pasar la rejilla de botones de 2 columnas a 1. Es lo que deja sitio " +
                 "para los nombres completos: cada botón pasa de 118 a 264 unidades de " +
                 "ancho. Desmárcalo si prefieres la disposición anterior.")]
        public bool rejillaDeUnaColumna = true;

        [Tooltip("Alto de cada botón cuando la rejilla va a una columna.")]
        public float altoBotonUnaColumna = 50f;

        [Tooltip("Numerar del 1 al 6 los botones que componen el procedimiento y " +
                 "ordenarlos como se ejecutan. Los que no son un paso (modo, detener, " +
                 "reiniciar) bajan al final sin número.")]
        public bool numerarLosPasos = true;

        [Header("Aviso al pie")]
        [Tooltip("Segundos que dura el resalte de la barra de mensajes cuando cambia el aviso.")]
        public float duracionDelResalte = 2f;

        public Color colorResalteAviso = new Color(0.65f, 0.45f, 0.05f, 0.9f);
        public Color colorResalteTexto = new Color(1f, 0.85f, 0.35f);

        private Image _fondoAviso;
        private Color _colorAvisoReposo = new Color(0f, 0f, 0f, 0.6f);
        private string _avisoAnterior = "";
        private float _brilloAviso;

        [Header("Visibilidad")]
        [Tooltip("Mantener el tablero apagado hasta que el estudiante llegue al reto 3. " +
                 "Hasta ahora estaba encendido desde el arranque y se veía de lejos " +
                 "durante el reto 2, lo que confundía sobre en qué reto se estaba.")]
        public bool ocultarHastaElReto3 = true;

        [Tooltip("Distancia en metros por debajo de la cual el tablero se enciende solo, " +
                 "como red de seguridad para quien entre a la sesión ya empezada. Va en 0, " +
                 "es decir desactivada: con 6 m el tablero se encendía durante el reto 2, " +
                 "porque desde la zona del cuestionario ya se está dentro de ese radio. " +
                 "Súbela solo si se confirma que alguien puede llegar al reto 3 sin pasar " +
                 "por la transición.")]
        public float distanciaDeEncendido = 0f;

        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private bool _visible = true;
        private float _proximaComprobacionDistancia;

        private void Awake()
        {
            if (engine == null) engine = GetComponentInParent<PasteurizerSimEngine>();
            if (engine == null) engine = FindFirstObjectByType<PasteurizerSimEngine>();
            BindButtons();
            BindSliders();

            _canvas = GetComponent<Canvas>();
            _raycaster = GetComponent<GraphicRaycaster>();

            AplicarTamanoMinimoDeLetra();
            AplicarContornoDelDiagrama();
        }

        private void Start()
        {
            // Va en Start y no en Awake porque necesita el ancho ya resuelto del
            // area de botones, y RectTransform.rect no es fiable antes del primer
            // calculo de disposicion.
            AplicarRejillaDeUnaColumna();
            OrdenarYNumerarBotones();
            AplicarLegibilidadDelDiagrama();

            if (!ocultarHastaElReto3) return;

            FijarVisible(false);

            // Hay que enterarse de que el estudiante ha entrado al reto 3. Los
            // listeners se agregan en codigo y son LOCALES: encienden el tablero
            // de quien viajo, no el de los demas.
            var gestor = FindFirstObjectByType<GestorCuestionario>(FindObjectsInactive.Include);
            if (gestor == null)
            {
                Debug.LogWarning("[SimDashboard] No encuentro el GestorCuestionario: el tablero " +
                                 "se queda oculto y no hay quien lo encienda.", this);
                return;
            }

            // Camino antiguo: el cuestionario teletransportaba el mismo y avisaba
            // al terminar su transicion.
            if (gestor.OnTransitionComplete != null)
                gestor.OnTransitionComplete.AddListener(MostrarTablero);

            // Camino nuevo: avanza el grupo, y el cuestionario sale por yield break
            // sin llegar a invocar OnTransitionComplete. El aviso util pasa a ser
            // la LLEGADA al reto 3, que es justo lo que significa DespuesDelSalto.
            var avance = gestor.avanceEnGrupo;
            if (avance == null && gestor.reunion != null) avance = gestor.reunion.avance;

            if (avance != null && avance.DespuesDelSalto != null)
                avance.DespuesDelSalto.AddListener(MostrarTablero);
            else if (gestor.reunion != null || gestor.avanceEnGrupo != null)
                Debug.LogWarning("[SimDashboard] El cuestionario avanza en grupo pero no llego a " +
                                 "su AvanceEnGrupo, asi que nadie va a encender el tablero en el " +
                                 "reto 3. Revisa los campos Reunion y Avance En Grupo.", this);
        }

        /// <summary>
        /// Vuelve a ocultar el tablero para la tanda siguiente.
        ///
        /// La foto de la escena no basta aqui. FotoDeLaEscena guarda el estado en
        /// Awake y este tablero se oculta a si mismo en Start, que va despues: lo
        /// que quedo fotografiado es el lienzo ENCENDIDO, tal como esta guardado en
        /// la escena. Restaurar la foto lo dejaria visible desde el reto 1, que es
        /// justo lo que los docentes reportaron.
        ///
        /// El reinicio aplica la foto primero y los reinicios propios despues, de
        /// modo que esto manda sobre ella.
        /// </summary>
        public void ReiniciarAlInicio()
        {
            if (ocultarHastaElReto3) FijarVisible(false);
        }

        /// Enciende el tablero. La llama la transicion al reto 3 y la red de
        /// seguridad por distancia.
        public void MostrarTablero()
        {
            FijarVisible(true);
        }

        private void FijarVisible(bool visible)
        {
            _visible = visible;
            if (_canvas != null) _canvas.enabled = visible;
            if (_raycaster != null) _raycaster.enabled = visible;
        }

        /// Red de seguridad: si el estudiante llega al tablero sin haber pasado
        /// por la transicion, el tablero se enciende al acercarse.
        private void ComprobarDistancia()
        {
            if (_visible || !ocultarHastaElReto3) return;
            if (distanciaDeEncendido <= 0f) return;
            if (Time.unscaledTime < _proximaComprobacionDistancia) return;
            _proximaComprobacionDistancia = Time.unscaledTime + 0.5f;

            var cam = Camera.main;
            if (cam == null) return;

            var d = cam.transform.position - transform.position;
            if (d.sqrMagnitude <= distanciaDeEncendido * distanciaDeEncendido)
                MostrarTablero();
        }

        /// Sube a minFontSize los textos que esten por debajo, SOLO dentro de las
        /// zonas listadas. La primera version lo aplicaba a todo el tablero y el
        /// resultado fue peor que el problema: en la barra superior los valores
        /// ya estaban a 18 y las claves subieron encima de ellos, en la fila de
        /// SECUENCIA los seis rotulos se pisaron entre si, y en el diagrama de
        /// planta los nombres se montaron sobre las tuberias.
        private void AplicarTamanoMinimoDeLetra()
        {
            if (minFontSize <= 0f) return;

            int subidos = 0;
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == null || t.enableAutoSizing) continue;
                if (t.fontSize >= minFontSize) continue;
                if (!EstaDentroDe(t.transform, zonasConTamanoMinimo)) continue;
                t.fontSize = minFontSize;
                subidos++;
            }

            if (subidos > 0)
                Debug.Log($"[SimDashboard] Legibilidad: {subidos} textos subidos a {minFontSize} pt " +
                          $"dentro de {string.Join(", ", zonasConTamanoMinimo)}.");
        }

        /// Contornea los rotulos del diagrama para que se despeguen de las
        /// tuberias que pasan por detras, sin tocar su tamano ni su posicion.
        private void AplicarContornoDelDiagrama()
        {
            if (!contornoEnRotulosDelDiagrama || string.IsNullOrEmpty(zonaDelDiagrama)) return;

            var zona = new[] { zonaDelDiagrama };
            int tocados = 0;
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == null || !EstaDentroDe(t.transform, zona)) continue;
                t.outlineColor = new Color32(255, 255, 255, 235);
                t.outlineWidth = 0.22f;
                tocados++;
            }

            if (tocados > 0)
                Debug.Log($"[SimDashboard] Contorno aplicado a {tocados} rótulos del diagrama.");
        }

        /// Ordena los botones del panel en el orden real del procedimiento y
        /// numera los seis pasos que lo componen. Los tres que no son un paso
        /// (modo, detener, reiniciar) bajan al final y quedan sin numero.
        private void OrdenarYNumerarBotones()
        {
            if (!numerarLosPasos) return;

            var orden = new[] { startBtn, fillBtn, hotPumpBtn, coldPumpBtn, milkPumpBtn,
                                modeBtn, stopBtn, resetBatchBtn };
            int i = 0;
            foreach (var b in orden)
            {
                if (b == null) continue;
                b.transform.SetSiblingIndex(i++);
            }

            // Los rotulos de estos tres son fijos: el builder los escribe una vez
            // y RefreshButtons no los toca, asi que se ponen aqui.
            PonerTexto(startBtn,      "2. ▶ Iniciar");
            PonerTexto(stopBtn,       "■ Detener");
            PonerTexto(resetBatchBtn, "↻ Reiniciar lote");
        }

        /// Que falta para poder arrancar la bomba de producto, en el orden en que
        /// el motor lo comprueba. Mismo criterio que PasteurizerSimEngine.
        private string MotivoBombaProducto()
        {
            if (!engine.energyOn)  return "Falta paso 1";
            if (!engine.running)   return "Falta paso 2";
            if (!engine.pumpHotOn) return "Falta paso 4";
            if (engine.tankInVol < PasteurizerSimEngine.TANK_MIN_VOL)
                return $"Falta nivel: {PasteurizerSimEngine.TANK_MIN_VOL:F0} L";
            // Con la temperatura actual al lado se ve de un vistazo si sube o si
            // se quedo parada, que es justo lo que no se podia distinguir.
            return $"Faltan {PasteurizerSimEngine.T_PUMP_READY - engine.tempHeat:F0} °C " +
                   $"({engine.tempHeat:F0} de {PasteurizerSimEngine.T_PUMP_READY:F0})";
        }

        private static void PonerTexto(Button b, string texto)
        {
            if (b == null) return;
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.text = texto;
        }

        /// Resalta la barra de mensajes cuando el motor cambia el aviso. Sin esto,
        /// pulsar un boton cuyo enclavamiento no se cumple (el caso tipico es la
        /// bomba de producto, que exige energia, marcha, calefactor, nivel minimo
        /// y temperatura) parecia no hacer nada: el motor si respondia, pero lo
        /// hacia en un texto pequeño al pie que nadie miraba.
        private void ResaltarAvisoSiCambio()
        {
            if (hintTxt == null) return;

            if (_fondoAviso == null && hintTxt.transform.parent != null)
                _fondoAviso = hintTxt.transform.parent.GetComponent<Image>();

            string actual = engine != null ? (engine.hint ?? "") : "";
            if (actual != _avisoAnterior)
            {
                _avisoAnterior = actual;
                _brilloAviso = 1f;
            }

            if (_brilloAviso <= 0f) return;
            _brilloAviso = Mathf.Max(0f, _brilloAviso - Time.deltaTime / Mathf.Max(0.1f, duracionDelResalte));

            if (_fondoAviso != null)
                _fondoAviso.color = Color.Lerp(_colorAvisoReposo, colorResalteAviso, _brilloAviso);
            hintTxt.color = Color.Lerp(Color.white, colorResalteTexto, _brilloAviso);
        }

        /// Sube el tamaño de los rótulos del diagrama y, si al crecer se pisan
        /// entre ellos, los separa en vertical dentro de un margen acotado.
        ///
        /// Va aparte del tamaño minimo general porque estos rotulos estan
        /// colocados por coordenadas absolutas sobre el esquema, no dentro de una
        /// disposicion que los recoloque sola: aqui agrandar sin separar produce
        /// choques, y separar de mas aleja el rotulo de su equipo.
        private void AplicarLegibilidadDelDiagrama()
        {
            if (minFontSizeDiagrama <= 0f || string.IsNullOrEmpty(zonaDelDiagrama)) return;

            var zona = transform.Find("PlantArea/PlantSVG");
            if (zona == null) return;

            var rotulos = new List<RectTransform>();
            int subidos = 0;

            foreach (Transform hijo in zona)
            {
                if (!hijo.name.StartsWith("Lbl_")) continue;
                var rt = hijo as RectTransform;
                if (rt == null) continue;
                rotulos.Add(rt);

                foreach (var t in hijo.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t == null || t.enableAutoSizing) continue;
                    if (t.fontSize >= minFontSizeDiagrama) continue;
                    t.fontSize = minFontSizeDiagrama;
                    subidos++;
                }
            }

            if (subidos > 0)
                Debug.Log($"[SimDashboard] Diagrama: {subidos} rótulos subidos a {minFontSizeDiagrama} pt.");

            if (!separarRotulosQueChocan || rotulos.Count < 2) return;

            // El tamaño del recuadro lo calcula un ContentSizeFitter, de modo que
            // hay que forzar el calculo antes de medir nada.
            foreach (var rt in rotulos) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            var original = rotulos.Select(r => r.anchoredPosition.y).ToArray();
            int movidos = 0;

            for (int pasada = 0; pasada < 12; pasada++)
            {
                bool hubo = false;
                for (int i = 0; i < rotulos.Count; i++)
                    for (int j = i + 1; j < rotulos.Count; j++)
                    {
                        if (!SeTocan(rotulos[i], rotulos[j], out float empuje)) continue;
                        hubo = true;
                        Separar(rotulos[i], original[i], +empuje / 2f);
                        Separar(rotulos[j], original[j], -empuje / 2f);
                    }
                if (!hubo) break;
            }

            for (int i = 0; i < rotulos.Count; i++)
                if (Mathf.Abs(rotulos[i].anchoredPosition.y - original[i]) > 0.5f) movidos++;

            if (movidos > 0)
                Debug.Log($"[SimDashboard] Diagrama: {movidos} rótulos separados para no solaparse.");
        }

        private void Separar(RectTransform rt, float yOriginal, float delta)
        {
            var ap = rt.anchoredPosition;
            ap.y = Mathf.Clamp(ap.y + delta,
                               yOriginal - separacionMaxima,
                               yOriginal + separacionMaxima);
            rt.anchoredPosition = ap;
        }

        /// Solape en vertical de dos rótulos que además se cruzan en horizontal.
        private static bool SeTocan(RectTransform a, RectTransform b, out float empuje)
        {
            empuje = 0f;
            Rect ra = a.rect, rb = b.rect;
            Vector2 pa = a.anchoredPosition, pb = b.anchoredPosition;

            float ax0 = pa.x + ra.xMin, ax1 = pa.x + ra.xMax;
            float bx0 = pb.x + rb.xMin, bx1 = pb.x + rb.xMax;
            if (ax1 <= bx0 || bx1 <= ax0) return false;

            float ay0 = pa.y + ra.yMin, ay1 = pa.y + ra.yMax;
            float by0 = pb.y + rb.yMin, by1 = pb.y + rb.yMax;
            float solape = Mathf.Min(ay1, by1) - Mathf.Max(ay0, by0);
            if (solape <= 0f) return false;

            // El que esté más arriba sube; el otro baja.
            empuje = (pa.y >= pb.y ? 1f : -1f) * (solape + 2f);
            return true;
        }

        private static bool EstaDentroDe(Transform t, string[] nombres)
        {
            if (nombres == null || nombres.Length == 0) return false;
            for (var p = t; p != null; p = p.parent)
                foreach (var n in nombres)
                    if (!string.IsNullOrEmpty(n) && p.name == n) return true;
            return false;
        }

        /// Pasa la rejilla de botones a una sola columna y ensancha las celdas,
        /// que es lo que permite escribir los nombres completos sin que el texto
        /// se parta en tres lineas.
        private void AplicarRejillaDeUnaColumna()
        {
            if (!rejillaDeUnaColumna) return;

            var grid = GetComponentsInChildren<GridLayoutGroup>(true)
                       .FirstOrDefault(g => g != null && g.name == "ButtonsGrid");
            if (grid == null)
            {
                // El nombre lo pone el builder. Si cambio, se usa la primera
                // rejilla que aparezca dentro del panel de control.
                grid = GetComponentsInChildren<GridLayoutGroup>(true).FirstOrDefault();
            }
            if (grid == null) return;

            var area = grid.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(area);

            float anchoUtil = area.rect.width - grid.padding.left - grid.padding.right;
            // Si la disposicion todavia no esta resuelta, rect.width sale 0 o un
            // valor absurdo. Se cae entonces al ancho conocido del panel de
            // control (280) menos sus margenes.
            if (anchoUtil < 60f) anchoUtil = 264f;

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.cellSize = new Vector2(anchoUtil, altoBotonUnaColumna);

            int botones = grid.transform.childCount;
            float alto = botones * (altoBotonUnaColumna + grid.spacing.y) + 12f;
            area.sizeDelta = new Vector2(area.sizeDelta.x, alto);

            Debug.Log($"[SimDashboard] Rejilla de botones a 1 columna: " +
                      $"{botones} botones de {anchoUtil:F0} x {altoBotonUnaColumna:F0}.");
        }

        private void BindButtons()
        {
            if (energyBtn   != null) energyBtn.onClick.AddListener(engine.ToggleEnergy);
            if (startBtn    != null) startBtn.onClick.AddListener(engine.StartPlant);
            if (stopBtn     != null) stopBtn.onClick.AddListener(engine.StopPlant);
            if (modeBtn     != null) modeBtn.onClick.AddListener(engine.ToggleMode);
            if (fillBtn     != null) fillBtn.onClick.AddListener(engine.ToggleFill);
            if (hotPumpBtn  != null) hotPumpBtn.onClick.AddListener(engine.ToggleHotPump);
            if (coldPumpBtn != null) coldPumpBtn.onClick.AddListener(engine.ToggleColdPump);
            if (milkPumpBtn != null) milkPumpBtn.onClick.AddListener(engine.ToggleMilkPump);
            if (resetBatchBtn != null)
            {
                resetBatchBtn.onClick.AddListener(engine.ResetBatch);
                // Ademas del motor, el reinicio devuelve la INTERFAZ a su estado de
                // entrada. PasteurizerStartOverlay.ShowAgain existia desde el
                // principio pero nadie lo llamaba nunca: esta es la conexion que
                // faltaba para que el simulador quede listo para el siguiente
                // estudiante.
                resetBatchBtn.onClick.AddListener(OnResetBatchPressed);
            }
        }

        /// Devuelve la ventana EMPEZAR y, si esta configurado, reinicia la narracion.
        private void OnResetBatchPressed()
        {
            if (startOverlay == null)
                startOverlay = FindFirstObjectByType<PasteurizerStartOverlay>(FindObjectsInactive.Include);
            if (startOverlay != null && startOverlay.showAgainOnReset)
                startOverlay.ShowAgain();

            // Silencio explicito de la planta. Al cerrarse el lote la maquina
            // sigue en marcha y sus bucles continuan sonando, que es lo correcto,
            // pero al reiniciar deben callarse todos sin depender de que el motor
            // cambie de estado en el orden justo.
            if (_fx == null) _fx = FindFirstObjectByType<PasteurizerFxController>(FindObjectsInactive.Include);
            if (_fx != null) _fx.DetenerTodo();
        }

        private PasteurizerFxController _fx;

        private void BindSliders()
        {
            if (targetSlider != null)
            {
                targetSlider.minValue = 10f;
                targetSlider.maxValue = PasteurizerSimEngine.TANK_IN_MAX;
                targetSlider.value = engine.targetFillVol;
                targetSlider.onValueChanged.AddListener(v => engine.targetFillVol = v);
            }
            if (flowSlider != null)
            {
                flowSlider.minValue = 0.1f;
                flowSlider.maxValue = 2.0f;
                flowSlider.value = engine.flowRate;
                flowSlider.onValueChanged.AddListener(v => engine.flowRate = v);
            }
        }

        private void Update()
        {
            ComprobarDistancia();

            if (engine == null) return;
            RefreshReadings();
            RefreshSteps();
            RefreshValves();
            RefreshButtons();
            RefreshSliders();
            RefreshTanks();
            RefreshBatchMetrics();
            RefreshHints();
            RefreshPlant();
        }

        // ====================================================================
        //  Plant animation (replica el SVG del visor web)
        // ====================================================================
        private void RefreshPlant()
        {
            // Tanques del plant (mismo data que los del panel métricas)
            if (plantTankInFill != null)
                plantTankInFill.fillAmount = Mathf.Clamp01(engine.tankInVol / PasteurizerSimEngine.TANK_IN_MAX);
            if (plantTankOutFill != null)
                plantTankOutFill.fillAmount = Mathf.Clamp01(engine.tankOutVol / PasteurizerSimEngine.TANK_OUT_MAX);
            if (plantPulmonFill != null)
            {
                plantPulmonFill.fillAmount = Mathf.Clamp01(engine.pulmonLevel);
                plantPulmonFill.color = engine.pulmonLevel < PasteurizerSimEngine.PULMON_OPTIMAL
                    ? new Color(0.62f, 0.83f, 1f) : new Color(0.77f, 0.40f, 1f);
            }
            if (plantTankInVolTxt  != null) plantTankInVolTxt.text  = $"{engine.tankInVol:F1} L";
            if (plantTankOutVolTxt != null) plantTankOutVolTxt.text = $"{engine.tankOutVol:F1} L";
            if (plantRetDisplay    != null) plantRetDisplay.text    = engine.tempHold.ToString("F0");

            // Bombas (aspas giran cuando ON)
            float dt = Time.deltaTime;
            if (engine.pumpMilkOn)
            {
                _pumpMilkAngle = (_pumpMilkAngle + pumpRpsActive * 360f * dt) % 360f;
                if (pumpMilkBlade != null)
                    pumpMilkBlade.localEulerAngles = new Vector3(0, 0, -_pumpMilkAngle);
            }
            if (engine.pumpHotOn)
            {
                _pumpHotAngle = (_pumpHotAngle + pumpRpsActive * 360f * dt) % 360f;
                if (pumpHotBlade != null)
                    pumpHotBlade.localEulerAngles = new Vector3(0, 0, -_pumpHotAngle);
            }

            // Flama caldera
            if (flameIcon != null)
            {
                bool fireOn = engine.pumpHotOn && engine.energyOn && engine.running;
                var c = flameIcon.color;
                c.a = fireOn ? Mathf.Lerp(0.7f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f)) : 0.1f;
                flameIcon.color = c;
            }

            // LED refrigerador
            if (refriLed != null)
                refriLed.color = engine.pumpColdOn
                    ? new Color(0.30f, 0.85f, 1f) : new Color(0.22f, 0.26f, 0.32f);

            // Trampa vapor drip
            if (trapDrip != null)
            {
                var c = trapDrip.color;
                c.a = engine.trapOpen ? Mathf.Lerp(0.3f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * 4f)) : 0f;
                trapDrip.color = c;
            }

            // Manómetro
            if (psiNeedle != null)
            {
                float frac = Mathf.Clamp01(engine.steamPsi / PasteurizerSimEngine.STEAM_PSI_MAX);
                float angle = Mathf.Lerp(120f, -120f, frac);  // -120 a +120 grados, invertido
                psiNeedle.localEulerAngles = new Vector3(0, 0, angle);
            }
            if (psiTextOnGauge != null) psiTextOnGauge.text = engine.steamPsi.ToString("F0");

            // Tuberías (color según flujo y temperatura)
            bool milkRunning = engine.pumpMilkOn && engine.energyOn && engine.running;
            bool valid = engine.vProd;

            SetPipe(pipeInlet != null ? new[] { pipeInlet } : null, engine.fillOn, pipeMilkColor);
            SetPipe(pipeRawSegments, milkRunning, pipeMilkColor);

            Color toHeatColor = pipeMilkWarm;
            if (engine.tempHeat >= PasteurizerSimEngine.SP_HEAT - 5f) toHeatColor = pipeMilkHot;
            else if (!engine.pumpHotOn) toHeatColor = pipeMilkColor;
            SetPipe(pipeToHeatSegments, milkRunning, toHeatColor);

            SetPipe(pipeToRetSegments,   milkRunning && engine.tempHeat > 60f, pipeMilkHot);
            SetPipe(pipeFromRetSegments, milkRunning && engine.tempHeat > 60f, pipeMilkHot);
            SetPipe(pipeToCoolingSegments, milkRunning && valid, pipeMilkWarm);
            SetPipe(pipeOutSegments,        milkRunning && valid, pipeMilkCold);
            SetPipe(pipeReturnSegments,     milkRunning && engine.vRet, pipeMilkWarm);

            SetPipe(pipeHotSupplySegments, engine.pumpHotOn, pipeWaterHot);
            SetPipe(pipeHotReturnSegments, engine.pumpHotOn, pipeWaterHot);
            SetPipe(pipeHotBackSegments,   engine.pumpHotOn, pipeWaterHot);

            SetPipe(pipeMakeupSegments,    engine.makeupOn, pipeWaterCold);
            SetPipe(pipePulmonOutSegments, engine.pumpHotOn && engine.pulmonLevel > 0.05f, pipeWaterCold);
            SetPipe(pipeColdSegments,        engine.pumpColdOn, pipeWaterCold);
            SetPipe(pipeColdReturnSegments,  engine.pumpColdOn, pipeWaterCold);

            // Serpentina holding (resalta cuando hay flujo caliente)
            if (plantRetFlowOverlay != null)
            {
                var c = plantRetFlowOverlay.color;
                c.a = (milkRunning && engine.tempHeat > 60f) ? 1f : 0f;
                plantRetFlowOverlay.color = c;
            }

            // Válvulas: rota el handle 45° cuando se "abre"
            SetValveHandle(valveProdHandle, valveProdBody, engine.vProd,  engine.pumpMilkOn);
            SetValveHandle(valveRetHandle,  valveRetBody,  engine.vRet,   engine.pumpMilkOn);
            SetValveHandle(valveDesHandle,  valveDesBody,  engine.vDes,   engine.pumpMilkOn);
            SetValveHandle(valveFillHandle, valveFillBody, engine.fillOn, engine.energyOn);
        }

        private void SetPipe(Image[] segments, bool on, Color flowColor)
        {
            if (segments == null) return;
            var c = on ? flowColor : pipeOffColor;
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] != null) segments[i].color = c;
        }

        private void SetValveHandle(RectTransform handle, Image body, bool open, bool used)
        {
            if (handle != null)
                handle.localEulerAngles = new Vector3(0, 0, (used && open) ? 45f : 0f);
            if (body != null)
                body.color = used
                    ? (open ? new Color(0.30f, 0.85f, 0.40f) : new Color(0.85f, 0.30f, 0.30f))
                    : new Color(0.78f, 0.83f, 0.88f);
        }

        // ---- updaters ----

        private void RefreshReadings()
        {
            if (heatDisplay != null) heatDisplay.text = $"{engine.tempHeat:F1} °C";
            if (heatTemp    != null) heatTemp.text    = $"{engine.tempHeat:F1} C";
            if (holdTemp    != null) holdTemp.text    = $"{engine.tempHold:F1} C";
            if (outTemp     != null) outTemp.text     = $"{engine.tempOut:F1} C";
            if (holdTime    != null) holdTime.text    = $"{engine.holdTimer:F1} s";
            if (psi         != null) psi.text         = $"{engine.steamPsi:F1}";
            if (pulmonLvl   != null) pulmonLvl.text   = $"{engine.pulmonLevel * 100f:F0} %";
        }

        private void RefreshSteps()
        {
            bool s1 = engine.energyOn && engine.running;
            bool s2 = engine.tankInVol >= PasteurizerSimEngine.TANK_MIN_VOL;
            bool s3 = engine.pumpHotOn && engine.tempHeat >= PasteurizerSimEngine.T_PUMP_READY;
            bool s4 = engine.pumpColdOn;
            bool s5 = engine.pumpMilkOn;
            bool s6 = engine.vProd;
            SetStep(0, s1 ? StepState.Done : StepState.Active);
            SetStep(1, !s1 ? StepState.Idle : (s2 ? StepState.Done : StepState.Active));
            SetStep(2, !s2 ? StepState.Idle : (s3 ? StepState.Done : StepState.Active));
            SetStep(3, !s3 ? StepState.Idle : (s4 ? StepState.Done : StepState.Active));
            SetStep(4, !s4 ? StepState.Idle : (s5 ? StepState.Done : StepState.Active));
            SetStep(5, !s5 ? StepState.Idle : (s6 ? StepState.Done : StepState.Active));
        }

        private enum StepState { Idle, Active, Done }
        private void SetStep(int i, StepState st)
        {
            if (i < 0 || i >= stepDots.Length) return;
            if (stepDots[i] != null)
            {
                stepDots[i].color = st == StepState.Done   ? colorOk
                                  : st == StepState.Active ? colorActive
                                  : colorOff;
            }
            if (i < stepLabels.Length && stepLabels[i] != null)
            {
                stepLabels[i].color = st == StepState.Idle ? colorOff : Color.white;
            }
        }

        private void RefreshValves()
        {
            SetValveText(vProdTxt, engine.vProd);
            SetValveText(vRetTxt,  engine.vRet);
            SetValveText(vDesTxt,  engine.vDes);
            if (trapStateTxt != null)
            {
                // Forma corta a proposito: la celda de esta tesela mide unas 100
                // unidades y el valor ya esta a 18 pt en negrita, de modo que
                // "DRENANDO" se partia en dos lineas. El nombre completo del
                // elemento vive en el panel de control.
                trapStateTxt.text = engine.trapOpen ? "DRENA" : "CERR";
                trapStateTxt.color = engine.trapOpen ? colorOk : colorOff;
            }
            if (alarmTxt != null)
            {
                alarmTxt.text = engine.alarm ? "ALARMA" : "NORMAL";
                alarmTxt.color = engine.alarm ? colorAlarm : colorOk;
            }
            if (cipTxt != null) cipTxt.text = engine.GetCipStatus();
        }

        private void SetValveText(TMP_Text txt, bool open)
        {
            if (txt == null) return;
            // Igual que la trampa: en la tesela VÁLVULAS de la barra superior no
            // caben las palabras completas. Ver la nota de RefreshValves.
            txt.text = open ? "ABR" : "CERR";
            txt.color = open ? colorOk : colorOff;
        }

        private void RefreshButtons()
        {
            // Labels en español, dos líneas: nombre completo arriba y estado abajo.
            // Las abreviaturas anteriores ("V. Llenado", "B. Producto") venian de
            // cuando la rejilla tenia dos columnas de 118 unidades. Con una sola
            // columna de 264 caben los nombres enteros.
            //
            // "Calefactor" se deja como esta a proposito. La denominacion correcta
            // es intercambiador, tal como señalo un docente, pero la locucion
            // VO_Sim_04_Calefactor_Espera dice "calefactor": cambiar solo el rotulo
            // dejaria el texto y la voz diciendo cosas distintas. El cambio va
            // junto con la regrabacion, en el bloque de contenido.
            string n3 = numerarLosPasos ? "3. " : "";
            string n4 = numerarLosPasos ? "4. " : "";
            string n5 = numerarLosPasos ? "5. " : "";
            string n6 = numerarLosPasos ? "6. " : "";

            if (energyLbl   != null) energyLbl.text   = $"1. Energía: {(engine.energyOn ? "ENCENDIDA" : "APAGADA")}";
            if (modeLbl     != null) modeLbl.text     = $"Modo de operación\n{(engine.mode == PasteurizerSimEngine.Mode.Production ? "Producción" : "Limpieza CIP")}";
            if (fillLbl     != null) fillLbl.text     = $"{n3}Válvula de llenado\n{(engine.fillOn ? "ABIERTA" : "CERRADA")}";
            // Mientras calienta, el boton dice a que temperatura va. La subida de
            // 20 a 65 grados tarda unos seis segundos, y sin este numero la espera
            // parece que el simulador se colgo.
            if (hotPumpLbl != null)
            {
                string estadoCal;
                if (!engine.pumpHotOn)
                    estadoCal = engine.CanStartHot() ? "APAGADO" : "Falta paso 2";
                else if (engine.tempHeat < PasteurizerSimEngine.T_PUMP_READY)
                    estadoCal = $"CALENTANDO {engine.tempHeat:F0} °C";
                else
                    estadoCal = $"ENCENDIDO {engine.tempHeat:F0} °C";
                hotPumpLbl.text = $"{n4}Calefactor\n{estadoCal}";
            }
            if (coldPumpLbl != null) coldPumpLbl.text = $"{n5}Refrigerador\n{(engine.pumpColdOn ? "ENCENDIDO" : "APAGADO")}";
            // La bomba de producto es el unico boton con cuatro enclavamientos
            // encadenados, y por eso es el que parece averiado: se pulsa y no
            // pasa nada visible. En vez de dejar el motivo solo en la barra del
            // pie, se escribe en el propio boton.
            if (milkPumpLbl != null)
            {
                string estado = engine.pumpMilkOn      ? "ENCENDIDA"
                              : engine.CanStartMilk()  ? "APAGADA"
                              : MotivoBombaProducto();
                milkPumpLbl.text = $"{n6}Bomba de producto\n{estado}";
            }

            SetBtnColor(energyBtn,   engine.energyOn);
            SetBtnColor(fillBtn,     engine.fillOn);
            SetBtnColor(hotPumpBtn,  engine.pumpHotOn);
            SetBtnColor(coldPumpBtn, engine.pumpColdOn);
            SetBtnColor(milkPumpBtn, engine.pumpMilkOn,
                disabled: !engine.CanStartMilk() && !engine.pumpMilkOn);
        }

        private void SetBtnColor(Button btn, bool on, bool disabled = false)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            if (disabled)   img.color = colorBtnDis;
            else if (on)    img.color = colorBtnOn;
            else            img.color = colorBtnOff;
        }

        private void RefreshSliders()
        {
            if (targetVal != null) targetVal.text = engine.targetFillVol.ToString("F0");
            if (flowVal   != null) flowVal.text   = engine.flowRate.ToString("F2");
        }

        private void RefreshTanks()
        {
            if (tankInFill != null)
                tankInFill.fillAmount = Mathf.Clamp01(engine.tankInVol / PasteurizerSimEngine.TANK_IN_MAX);
            if (tankOutFill != null)
                tankOutFill.fillAmount = Mathf.Clamp01(engine.tankOutVol / PasteurizerSimEngine.TANK_OUT_MAX);
            if (tankInVolTxt != null)
                tankInVolTxt.text = $"{engine.tankInVol:F1} L";
            if (tankOutVolTxt != null)
                tankOutVolTxt.text = $"{engine.tankOutVol:F1} L";
            if (pulmonFill != null)
            {
                pulmonFill.fillAmount = Mathf.Clamp01(engine.pulmonLevel);
                pulmonFill.color = engine.pulmonLevel < PasteurizerSimEngine.PULMON_OPTIMAL
                    ? new Color(0.62f, 0.83f, 1f) : new Color(0.77f, 0.40f, 1f);
            }
        }

        private void RefreshBatchMetrics()
        {
            if (mFilled != null) mFilled.text = $"{engine.vFilled:F1} L";
            if (mFinal  != null) mFinal.text  = $"{engine.vFinal:F1} L";
            if (mRecirc != null) mRecirc.text = $"{engine.vRecirc:F1} L";
            if (mEvap   != null) mEvap.text   = $"{engine.vEvap:F3} L";
            if (mLoss   != null) mLoss.text   = $"{engine.vLoss:F1} L";
            if (mYield  != null) mYield.text  = engine.vFilled > 0 ? $"{engine.YieldPct:F1} %" : "-- %";
        }

        private void RefreshHints()
        {
            if (hintTxt != null) hintTxt.text = engine.hint ?? "";
            ResaltarAvisoSiCambio();
        }
    }
}
