using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
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

    [Header("Efectos de Sonido (Interfaz)")]
    public AudioSource reproductorAudio;
    public AudioClip audioIntroReto2; 
    public AudioClip audioCierreReto2; 
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;

    [Header("Transición Local a Escena 3")]
    [Tooltip("Arrastra aquí el objeto vacío hacia donde se teletransportará el jugador para operar la máquina")]
    public Transform puntoDestinoEscena3;
    
    // NOTA SENIOR: Hemos eliminado la variable 'canvasEscena3' del script.
    // Manejar interfaces globales por código local rompe la red en VIROO.

    [Header("Eventos de Red (VIROO)")]
    [Tooltip("Conecta aquí el Viroo Action/Network Event que encenderá el Canvas 3 e iniciará el video tutorial para TODOS los usuarios")]
    public UnityEvent OnTransitionComplete; 
    
    private Color colorOriginalBotones;
    private int preguntaActual = 0;
    private bool esperandoSiguiente = false;
    
    private bool enIntroduccion = true;
    private bool introReproducida = false;
    private bool finalizando = false; 

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
        // Lógica de llenado de preguntas conservada intacta...
        bancoPreguntas = new DatosPregunta[5];
        bancoPreguntas[0] = new DatosPregunta("¿Cuál es el objetivo principal de la pasteurización de la leche según la normatividad sanitaria?", "A) Eliminar todos los microorganismos presentes para que la leche sea estéril.", "B) Eliminar los microorganismos patógenos para garantizar la inocuidad, sin alterar significativamente sus propiedades.", "C) Aumentar el contenido de grasa de la leche para mejorar su sabor.", 1);
        bancoPreguntas[1] = new DatosPregunta("A nivel general, ¿cuáles son las tres etapas fundamentales que conforman el ciclo térmico en un sistema de pasteurización continuo?", "A) Mezclado, Ebullición y Filtrado.", "B) Calentamiento, Retención (mantenimiento de la temperatura) y Enfriamiento rápido.", "C) Fermentación, Evaporación y Condensación.", 1);
        bancoPreguntas[2] = new DatosPregunta("De acuerdo con la descripción del equipo, ¿qué funciones integran las múltiples secciones del Intercambiador de Placas?", "A) Almacenar la leche cruda, mantener la columna hidrostática y desacoplar variaciones de presión.", "B) Regeneración, calentamiento con agua caliente de la caldera y enfriamiento con agua fría del chiller.", "C) Garantizar el tiempo mínimo a temperatura de pasteurización (≥15 s a ≥72 °C).", 1);
        bancoPreguntas[3] = new DatosPregunta("¿Qué componente tiene la función específica de garantizar el tiempo mínimo a temperatura de pasteurización (≥15 s a ≥72 °C)?", "A) El Tubo de Retención.", "B) La Bomba Booster, para asegurar sobre-presión.", "C) El Filtro y Sanitario, antes de la bomba de alimentación.", 0);
        bancoPreguntas[4] = new DatosPregunta("La válvula de desviación es un elemento de seguridad crítico. ¿Qué ocurre si la temperatura a la salida del tubo de retención baja de 72 °C?", "A) Empuja la leche contra el delta-P para evitar contaminación por descompresión.", "B) Desvía el flujo automáticamente de vuelta al tanque de balance.", "C) Activa el banco de válvulas neumáticas superiores para aislar las secciones.", 1);
    }

    void ActualizarPantalla()
    {
        textoFeedback.text = "";
        ResetearColorBotones();

        if (enIntroduccion)
        {
            textoPregunta.text = "<b>¡Bienvenido al Reto 2!</b>\n\nEste reto consiste en responder un cuestionario interactivo sobre los fundamentos y beneficios de la pasteurización, así como los componentes del equipo pasteurizador HTST.\n\n<i>Antes de iniciar el cuestionario, se recomienda explorar detalladamente el pasteurizador y el carrusel interactivo ubicado en la planta piloto.</i>";
            
            if (botonA != null) { botonA.image.enabled = false; botonA.interactable = false; botonA.GetComponentInChildren<TextMeshProUGUI>().text = ""; }
            if (botonB != null) { botonB.image.enabled = false; botonB.interactable = false; botonB.GetComponentInChildren<TextMeshProUGUI>().text = ""; }
            if (botonC != null) { botonC.image.enabled = true; botonC.interactable = true; botonC.GetComponentInChildren<TextMeshProUGUI>().text = "Comenzar Cuestionario"; }
            
            if (!introReproducida && reproductorAudio != null && audioIntroReto2 != null)
            {
                reproductorAudio.clip = audioIntroReto2;
                reproductorAudio.Play();
                introReproducida = true;
            }

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
        else if (!finalizando)
        {
            // Sin esta guarda, una segunda llamada lanzaria un segundo fade y un
            // segundo arranque del video.
            finalizando = true;
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
                if (reproductorAudio != null) reproductorAudio.Stop();
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
        textoPregunta.text = "<b>¡Cuestionario Superado con Éxito!</b>\n\nHas completado la Escena 2: Reconocimiento del proceso de pasteurización.\n\nEstás preparado para iniciar la Escena 3: Simulación del proceso de pasteurización.";
        textoFeedback.text = ""; 
        
        if(botonA != null) botonA.gameObject.SetActive(false);
        if(botonB != null) botonB.gameObject.SetActive(false);
        if(botonC != null) botonC.gameObject.SetActive(false);

        float tInicioVO = 0f;
        float duracionVO = 0f;
        if (reproductorAudio != null && audioCierreReto2 != null)
        {
            reproductorAudio.clip = audioCierreReto2;
            reproductorAudio.Play();
            tInicioVO = Time.time;
            duracionVO = audioCierreReto2.length;
        }

        // La espera debe salir de la duracion real del clip, no de un numero fijo:
        // con 5 s fijos y un audio de 18 s el video de la Escena 3 arrancaba encima
        // de la voz en off. Se arranca el fade a falta de 'duracionFade' segundos
        // para que el negro se complete justo cuando termina la locucion.
        const float margenLectura = 1.0f;   // minimo para alcanzar a leer el mensaje
        float duracionFade = 2f;
        yield return new WaitForSeconds(Mathf.Max(margenLectura, duracionVO - duracionFade));
        
        Canvas miCanvas = GetComponent<Canvas>();
        if (miCanvas != null) miCanvas.enabled = false;

        // --- TRANSICIÓN INMERSIVA CINEMATOGRÁFICA (Local para quien viaja) ---
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

        float tiempo = 0;

        // FADE OUT
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, tiempo / duracionFade));
            yield return null;
        }

        // TELETRANSPORTE AL ENTORNO DE SIMULACIÓN
        if (puntoDestinoEscena3 != null)
        {
            Transform rootJugador = camaraVR.root;
            float diferenciaRotacion = puntoDestinoEscena3.eulerAngles.y - camaraVR.eulerAngles.y;
            rootJugador.Rotate(0, diferenciaRotacion, 0);
            Vector3 diferenciaPosicion = puntoDestinoEscena3.position - camaraVR.position;
            diferenciaPosicion.y = 0; 
            rootJugador.position += diferenciaPosicion;
        }

        // Garantia dura: no encender el video hasta que la locucion haya terminado
        // de verdad. Ademas hay que liberar el AudioSource, porque el VideoPlayer de
        // la Escena 3 lo tiene asignado como salida de audio y se lo arrebataria.
        if (reproductorAudio != null && duracionVO > 0f)
        {
            while (Time.time < tInicioVO + duracionVO) yield return null;
            reproductorAudio.Stop();
            reproductorAudio.clip = null;
        }

        // --- AVISO A LA RED ---
        // Notificamos a VIROO que el usuario completó la transición.
        // El componente de red conectado aquí se encargará de encender el Canvas 3 y el Video.
        OnTransitionComplete?.Invoke();

        yield return new WaitForSeconds(0.5f);

        // FADE IN
        tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, tiempo / duracionFade));
            yield return null;
        }

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