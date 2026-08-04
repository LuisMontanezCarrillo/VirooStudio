using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class GestorEscena3 : MonoBehaviour
{
    // AJUSTE: Ya no necesitamos la referencia a 'canvasInicio' porque el GestorCuestionario lo apagar� por s� solo

    [Header("Pantalla de Video")]
    [Tooltip("El Canvas duplicado que contiene la Raw Image y el Video Player (Canvas_VideoEsc3)")]
    public GameObject canvasVideo; 
    [Tooltip("El componente Video Player encargado de reproducir el tutorial")]
    public VideoPlayer reproductorTutorial;

    [Header("Al terminar el video")]
    [Tooltip("Se dispara cuando el video explicativo termina. Aqui se engancha la " +
             "aparicion del tablero del simulador, que no debe verse antes.")]
    public UnityEvent OnTutorialFinalizado;

    void Start()
    {
        // Nos aseguramos de que el video comience completamente oculto en memoria
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(false); 
        }

        // Suscribimos el evento inteligente para cuando el video termine su reproducci�n de 1:45
        if (reproductorTutorial != null)
        {
            reproductorTutorial.loopPointReached += OcultarVideo;
        }
    }

    // AJUSTE: Esta funci�n ahora se llama autom�ticamente por el GestorCuestionario tras el Fade In
    public void IniciarTutorialAutomatico()
    {
        // Idempotente: si ya se esta reproduciendo, una segunda llamada lo reiniciaria
        // desde cero para quien ya lo estaba viendo.
        if (reproductorTutorial != null && reproductorTutorial.isPlaying) return;

        Debug.Log("<color=yellow>[GestorEscena3] Senal automatica recibida. Activando video tutorial.</color>");

        // 1. Encendemos el Canvas del Video y le damos a Play
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(true);
            
            if (reproductorTutorial != null)
            {
                reproductorTutorial.Play();
                Debug.Log("<color=green>[GestorEscena3] Reproducci�n autom�tica del video tutorial iniciada con �xito.</color>");
            }
        }
    }

    void OnDestroy()
    {
        if (reproductorTutorial != null)
            reproductorTutorial.loopPointReached -= OcultarVideo;
    }

    void OcultarVideo(VideoPlayer vp)
    {
        // Esta funci�n se activa autom�ticamente al finalizar el minuto y 45 segundos
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(false);
        }
        Debug.Log("<color=green>[GestorEscena3] Video tutorial finalizado. Simulador liberado para el estudiante.</color>");

        // Ahora si: el estudiante ya sabe como funciona el proceso y puede operar.
        OnTutorialFinalizado?.Invoke();
    }
}