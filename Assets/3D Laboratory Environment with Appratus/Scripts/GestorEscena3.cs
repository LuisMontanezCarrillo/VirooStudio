using UnityEngine;
using UnityEngine.Video;

public class GestorEscena3 : MonoBehaviour
{
    // AJUSTE: Ya no necesitamos la referencia a 'canvasInicio' porque el GestorCuestionario lo apagará por sí solo

    [Header("Pantalla de Video")]
    [Tooltip("El Canvas duplicado que contiene la Raw Image y el Video Player (Canvas_VideoEsc3)")]
    public GameObject canvasVideo; 
    [Tooltip("El componente Video Player encargado de reproducir el tutorial")]
    public VideoPlayer reproductorTutorial;

    void Start()
    {
        // Nos aseguramos de que el video comience completamente oculto en memoria
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(false); 
        }

        // Suscribimos el evento inteligente para cuando el video termine su reproducción de 1:45
        if (reproductorTutorial != null)
        {
            reproductorTutorial.loopPointReached += OcultarVideo;
        }
    }

    // AJUSTE: Esta función ahora se llama automáticamente por el GestorCuestionario tras el Fade In
    public void IniciarTutorialAutomatico()
    {
        Debug.Log("<color=yellow>[GestorEscena3] Señal automática recibida. Activando video tutorial.</color>");

        // 1. Encendemos el Canvas del Video y le damos a Play
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(true);
            
            if (reproductorTutorial != null)
            {
                reproductorTutorial.Play();
                Debug.Log("<color=green>[GestorEscena3] Reproducción automática del video tutorial iniciada con éxito.</color>");
            }
        }
    }

    void OcultarVideo(VideoPlayer vp)
    {
        // Esta función se activa automáticamente al finalizar el minuto y 45 segundos
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(false);
        }
        Debug.Log("<color=green>[GestorEscena3] Video tutorial finalizado. Simulador liberado para el estudiante.</color>");
    }
}