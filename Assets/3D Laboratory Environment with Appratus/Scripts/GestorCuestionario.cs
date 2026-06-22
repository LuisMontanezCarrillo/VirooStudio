using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GestorCuestionario : MonoBehaviour
{
    [Header("Referencias de la Interfaz (UI)")]
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI textoFeedback;
    public Button botonA;
    public Button botonB;
    public Button botonC;

    [Header("Configuración de Feedback")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;

    [Header("Efectos de Sonido")]
    public AudioSource reproductorAudio;
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;

    [Header("Transición a Escena 3")]
    [Tooltip("Arrastra aquí el objeto vacío hacia donde se teletransportará el jugador para operar la máquina")]
    public Transform puntoDestinoEscena3;
    
    private Color colorOriginalBotones;
    private int preguntaActual = 0;
    private bool esperandoSiguiente = false;
    
    private bool enIntroduccion = true;

    private class DatosPregunta
    {
        public string enunciado;
        public string opcionA;
        public string opcionB;
        public string opcionC;
        public int respuestaCorrecta; 

        public DatosPregunta(string e, string a, string b, string c, int correcto)
        {
            enunciado = e; opcionA = a; opcionB = b; opcionC = c; respuestaCorrecta = correcto;
        }
    }

    private DatosPregunta[] bancoPreguntas;

    void Start()
    {
        if (botonA != null) colorOriginalBotones = botonA.image.color;
        
        if (botonA != null) { botonA.onClick.RemoveAllListeners(); botonA.onClick.AddListener(() => ValidarRespuesta(0, botonA)); }
        if (botonB != null) { botonB.onClick.RemoveAllListeners(); botonB.onClick.AddListener(() => ValidarRespuesta(1, botonB)); }
        if (botonC != null) { botonC.onClick.RemoveAllListeners(); botonC.onClick.AddListener(() => ValidarRespuesta(2, botonC)); }

        LlenarBancoPreguntas();
        ActualizarPantalla();
    }

    void LlenarBancoPreguntas()
    {
        bancoPreguntas = new DatosPregunta[5];

        bancoPreguntas[0] = new DatosPregunta(
            "¿Cuál es el objetivo principal de la pasteurización de la leche según la normatividad sanitaria?",
            "A) Eliminar todos los microorganismos presentes para que la leche sea estéril.",
            "B) Eliminar patógenos para garantizar inocuidad, sin alterar significativamente sus propiedades.",
            "C) Aumentar el contenido de grasa de la leche para mejorar su sabor.",
            1 
        );

        bancoPreguntas[1] = new DatosPregunta(
            "A nivel general, ¿cuáles son las tres etapas fundamentales que conforman el ciclo térmico en un sistema de pasteurización continuo?",
            "A) Mezclado, Ebullición y Filtrado.",
            "B) Calentamiento, Retención (mantener temperatura) y Enfriamiento rápido.",
            "C) Fermentación, Evaporación y Condensación.",
            1 
        );

        bancoPreguntas[2] = new DatosPregunta(
            "De acuerdo con la descripción del equipo, ¿qué funciones integran las múltiples secciones del Intercambiador de Placas (HE-01)?",
            "A) Almacenar la leche cruda, mantener la columna hidrostática y desacoplar presiones.",
            "B) Regeneración, calentamiento con agua caliente y enfriamiento con agua fría.",
            "C) Garantizar el tiempo mínimo a temperatura de pasteurización (≥15 s a ≥72 °C).",
            1 
        );

        bancoPreguntas[3] = new DatosPregunta(
            "¿Qué componente tiene la función específica de garantizar el tiempo mínimo a temperatura de pasteurización (≥15 s a ≥72 °C)?",
            "A) El Tubo de Retención (HT-01).",
            "B) La Bomba Booster (P-02).",
            "C) El Filtro Y Sanitario (F-01).",
            0 
        );

        bancoPreguntas[4] = new DatosPregunta(
            "La Válvula de Diversión es clave para el HACCP. ¿Qué ocurre si la temperatura a la salida del tubo de retención cae bajo el setpoint?",
            "A) Empuja la leche contra el delta-P para evitar contaminación por descompresión.",
            "B) Desvía el flujo automáticamente de vuelta al tanque de balance.",
            "C) Activa las válvulas neumáticas superiores para aislar las secciones.",
            1 
        );
    }

    void ActualizarPantalla()
    {
        textoFeedback.text = "";
        ResetearColorBotones();

        if (enIntroduccion)
        {
            textoPregunta.text = "<b>¡Bienvenido al Reto 2!</b>\n\nEste reto consiste en un cuestionario interactivo sobre los fundamentos y beneficios de la pasteurización, así como los componentes del equipo pasteurizador HTST.\n\n<i>Recomendación:</i> Antes de iniciar el cuestionario, se recomienda explorar detalladamente el pasteurizador y el carrusel interactivo de la sala.";
            
            if (botonA != null) { botonA.image.enabled = false; botonA.interactable = false; botonA.GetComponentInChildren<TextMeshProUGUI>().text = ""; }
            if (botonB != null) { botonB.image.enabled = false; botonB.interactable = false; botonB.GetComponentInChildren<TextMeshProUGUI>().text = ""; }
            if (botonC != null) { botonC.image.enabled = true; botonC.interactable = true; botonC.GetComponentInChildren<TextMeshProUGUI>().text = "Comenzar Cuestionario"; }
            
            esperandoSiguiente = false;
        }
        else if (preguntaActual < bancoPreguntas.Length)
        {
            if (botonA != null) { botonA.image.enabled = true; botonA.interactable = true; }
            if (botonB != null) { botonB.image.enabled = true; botonB.interactable = true; }
            if (botonC != null) { botonC.image.enabled = true; botonC.interactable = true; }

            DatosPregunta p = bancoPreguntas[preguntaActual];
            textoPregunta.text = p.enunciado;
            botonA.GetComponentInChildren<TextMeshProUGUI>().text = p.opcionA;
            botonB.GetComponentInChildren<TextMeshProUGUI>().text = p.opcionB;
            botonC.GetComponentInChildren<TextMeshProUGUI>().text = p.opcionC;
            
            esperandoSiguiente = false;
        }
        else
        {
            StartCoroutine(FinalizarCuestionario());
        }
    }

    void ValidarRespuesta(int opcionSeleccionada, Button botonPresionado)
    {
        if (esperandoSiguiente) return; 

        if (enIntroduccion)
        {
            if (opcionSeleccionada == 2) 
            {
                enIntroduccion = false;
                ActualizarPantalla();
            }
            return;
        }

        if (opcionSeleccionada == bancoPreguntas[preguntaActual].respuestaCorrecta)
        {
            botonPresionado.image.color = colorCorrecto;
            textoFeedback.text = "<color=green>¡Excelente! Respuesta correcta.</color>";
            
            if (reproductorAudio != null && sonidoCorrecto != null) reproductorAudio.PlayOneShot(sonidoCorrecto);

            esperandoSiguiente = true;
            StartCoroutine(SiguientePreguntaCo());
        }
        else
        {
            botonPresionado.image.color = colorIncorrecto;
            textoFeedback.text = "<color=red>Respuesta incorrecta. Analiza el equipo e intenta de nuevo.</color>";
            
            if (reproductorAudio != null && sonidoIncorrecto != null) reproductorAudio.PlayOneShot(sonidoIncorrecto);
        }
    }

    IEnumerator SiguientePreguntaCo()
    {
        yield return new WaitForSeconds(2f);
        preguntaActual++;
        ActualizarPantalla();
    }

    IEnumerator FinalizarCuestionario()
    {
        // 1. Mensaje de cierre
        textoPregunta.text = "<b>¡Cuestionario Superado con Éxito!</b>\n\nHas completado la Escena 2: Reconocimiento del proceso de pasteurización.\n\nEstás preparado para iniciar la Escena 3: Simulación del proceso de pasteurización.";
        textoFeedback.text = ""; 
        
        if(botonA != null) botonA.gameObject.SetActive(false);
        if(botonB != null) botonB.gameObject.SetActive(false);
        if(botonC != null) botonC.gameObject.SetActive(false);

        // 2. Tiempo de lectura
        yield return new WaitForSeconds(8.0f);
        
        // 3. Desaparecer el menú visualmente
        Canvas miCanvas = GetComponent<Canvas>();
        if (miCanvas != null) miCanvas.enabled = false;

        // --- INICIA LA MAGIA DE LA TRANSICIÓN (FADE OUT + TELEPORT + FADE IN) ---
        
        // A. Crear el panel negro frente a la cámara
        GameObject canvasFadeObj = new GameObject("Canvas_FadeVirtual");
        Canvas canvasFade = canvasFadeObj.AddComponent<Canvas>();
        canvasFade.renderMode = RenderMode.WorldSpace;
        canvasFade.sortingOrder = 999; 

        Transform camaraVR = Camera.main.transform;
        canvasFadeObj.transform.SetParent(camaraVR, false);
        canvasFadeObj.transform.localPosition = new Vector3(0, 0, 0.5f); 
        canvasFadeObj.transform.localRotation = Quaternion.identity;
        canvasFadeObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        GameObject imgObj = new GameObject("Cuadro_Negro");
        imgObj.transform.SetParent(canvasFadeObj.transform, false);
        Image imagenNegra = imgObj.AddComponent<Image>();
        imagenNegra.color = new Color(0, 0, 0, 0); 
        imagenNegra.rectTransform.sizeDelta = new Vector2(5000, 5000); 
        imagenNegra.raycastTarget = false; 

        float duracionFade = 2f;
        float tiempo = 0;

        // B. FADE OUT (Fundido a negro progresivo)
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, tiempo / duracionFade));
            yield return null;
        }

        // C. ¡EL TELETRANSPORTE! (En la oscuridad total)
        if (puntoDestinoEscena3 != null)
        {
            Transform rootJugador = camaraVR.root;

            // Ajustamos la rotación para que mire al frente del panel
            float diferenciaRotacion = puntoDestinoEscena3.eulerAngles.y - camaraVR.eulerAngles.y;
            rootJugador.Rotate(0, diferenciaRotacion, 0);

            // Ajustamos la posición respetando la altura del usuario
            Vector3 diferenciaPosicion = puntoDestinoEscena3.position - camaraVR.position;
            diferenciaPosicion.y = 0; 
            rootJugador.position += diferenciaPosicion;
        }

        // Un breve respiro
        yield return new WaitForSeconds(0.5f);

        // D. FADE IN (Aclarar progresivamente la vista en la nueva zona)
        tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, tiempo / duracionFade));
            yield return null;
        }

        // E. Limpieza
        Destroy(canvasFadeObj);
    }

    void ResetearColorBotones()
    {
        if (botonA != null)
        {
            botonA.image.color = colorOriginalBotones;
            botonB.image.color = colorOriginalBotones;
            botonC.image.color = colorOriginalBotones;
        }
    }
}