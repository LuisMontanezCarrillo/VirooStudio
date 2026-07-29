using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeOutVR : MonoBehaviour
{
    public float tiempoDeEspera = 4f;
    public float duracionFade = 2f;

    [Tooltip("Arrastra aquí el objeto hacia donde quieres teletransportar al jugador")]
    public Transform puntoDeDestino;

    [Tooltip("El Canvas de la Fase 1 que vamos a apagar")]
    public GameObject canvasFase1;

    [Tooltip("El Canvas de la Fase 2 que vamos a encender")]
    public GameObject canvasFase2;

    [Tooltip("Arrastra aquí el objeto Bisagra_Puerta para cerrarla al teletransportar")]
    public ControladorPuerta puertaPlanta;

    [Header("Ambiente Sonoro")]
    [Tooltip("Arrastra aquí el objeto RoomTone_PlantaPiloto para iniciar el sonido ambiental al ingresar a la planta")]
    public AudioSource audioAmbientePlanta;

    public void IniciarFadeOut()
    {
        StartCoroutine(RutinaFadeYTeletransporte());
    }

    IEnumerator RutinaFadeYTeletransporte()
    {
        // 1. Espera inicial (mientras la puerta física se abre lentamente)
        yield return new WaitForSeconds(tiempoDeEspera);

        // 2. Crear panel negro dinámico frente a los ojos del jugador
        GameObject canvasObj = new GameObject("Canvas_FadeMagico");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 999; 

        Transform camaraVR = Camera.main.transform;
        canvasObj.transform.SetParent(camaraVR);
        canvasObj.transform.localPosition = new Vector3(0, 0, 0.5f); 
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        GameObject imgObj = new GameObject("Cuadro_Negro");
        imgObj.transform.SetParent(canvasObj.transform, false);
        Image imagenNegra = imgObj.AddComponent<Image>();
        imagenNegra.color = new Color(0, 0, 0, 0); 
        imagenNegra.rectTransform.sizeDelta = new Vector2(5000, 5000); 
        imagenNegra.raycastTarget = false; 

        // 3. OSCURECER (Fade Out)
        float tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, tiempo / duracionFade));
            yield return null;
        }

        // 4. ¡EL TELETRANSPORTE INTELIGENTE! (En total oscuridad)
        if (puntoDeDestino != null)
        {
            Transform rootJugador = camaraVR.root;

            // A. ROTACIÓN: Orientamos al jugador hacia la dirección del destino
            float diferenciaRotacion = puntoDeDestino.eulerAngles.y - camaraVR.eulerAngles.y;
            rootJugador.Rotate(0, diferenciaRotacion, 0);

            // B. CÁLCULO DE POSICIÓN: Medimos la distancia exacta a la baldosa de destino
            Vector3 diferenciaPosicion = puntoDeDestino.position - camaraVR.position;
            diferenciaPosicion.y = 0; // Respetamos la altura física del usuario

            // C. TRASLACIÓN: Movemos el cuerpo completo del jugador
            rootJugador.position += diferenciaPosicion;

            // D. CAMBIO DE INTERFAZ: Apagamos el Canvas viejo y encendemos el nuevo de la Escena 2
            if (canvasFase1 != null) canvasFase1.SetActive(false);
            if (canvasFase2 != null) canvasFase2.SetActive(true);

            // E. CERRAR PUERTA NORMATIVA: La regresamos a su posición cerrada original
            if (puertaPlanta != null) puertaPlanta.CerrarInstante();
        }

        // F. AMBIENTE SONORO: Reproducimos el Room Tone en la oscuridad, justo antes de aclarar la vista
        if (audioAmbientePlanta != null && !audioAmbientePlanta.isPlaying)
        {
            audioAmbientePlanta.Play();
        }

        yield return new WaitForSeconds(0.5f); // Un breve respiro de calma en la oscuridad

        // 5. ACLARAR (Fade In - El estudiante abre los ojos en la planta piloto)
        tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, tiempo / duracionFade));
            yield return null;
        }

        // 6. Limpieza automática del objeto temporal
        Destroy(canvasObj);
    }
}