using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class GestorEscena3 : MonoBehaviour, IReiniciable
{
    // AJUSTE: Ya no necesitamos la referencia a 'canvasInicio' porque el GestorCuestionario lo apagará por sí solo

    [Header("Pantalla de Video")]
    [Tooltip("El Canvas duplicado que contiene la Raw Image y el Video Player (Canvas_VideoEsc3)")]
    public GameObject canvasVideo; 
    [Tooltip("El componente Video Player encargado de reproducir el tutorial")]
    public VideoPlayer reproductorTutorial;

    [Header("Aviso de video visto")]
    [Tooltip("Se dispara una sola vez cuando el video se da por visto, sea porque " +
             "termino solo o porque el estudiante lo cerro. Sirve para marcar el " +
             "primer paso del guion del reto 3.")]
    public UnityEvent AlDarPorVistoElVideo;

    // Una sola vez por tanda. El video no se puede volver a reproducir: lo arranca
    // el cuestionario al llegar y no hay boton que lo repita, de modo que un aviso
    // repetido no tendria de donde salir. La marca esta por si en el futuro lo hay.
    private bool _yaAvisoDelVideo;

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
            NarradorUnico.Registrar(reproductorTutorial);
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
                // El video del reto 3 es la voz que manda al llegar: calla la
                // locución del carrusel si el estudiante activó una diapositiva
                // justo antes de que se disparara la transición.
                NarradorUnico.TomarLaPalabra(reproductorTutorial);
                reproductorTutorial.Play();
                Debug.Log("<color=green>[GestorEscena3] Reproducción automática del video tutorial iniciada con éxito.</color>");
            }
        }
    }

    /// <summary>
    /// Apaga el vídeo del reto 3 y lo rebobina. Sin rebobinar, la tanda
    /// siguiente lo vería arrancar por donde lo dejó la anterior.
    /// </summary>
    public void ReiniciarAlInicio()
    {
        if (reproductorTutorial != null)
        {
            reproductorTutorial.Stop();
            reproductorTutorial.frame = 0;
        }
        if (canvasVideo != null) canvasVideo.SetActive(false);
        _yaAvisoDelVideo = false;
    }

    /// <summary>
    /// Da el video por visto y avisa una sola vez.
    ///
    /// Se mide por "dejo de estar delante" y no por "lo vio entero" a proposito.
    /// El estudiante puede cerrarlo con el boton CERRAR de ControlesVideoEsc3, y
    /// despues de cerrarlo no hay forma de volver a verlo. Si el aviso exigiera
    /// llegar al final, quien lo cierre en el segundo tres se quedaria con el paso
    /// sin marcar y sin manera de arreglarlo.
    /// </summary>
    public void DarPorVistoElVideo()
    {
        if (_yaAvisoDelVideo) return;
        _yaAvisoDelVideo = true;
        AlDarPorVistoElVideo?.Invoke();
    }

    void OcultarVideo(VideoPlayer vp)
    {
        // Esta función se activa automáticamente al finalizar el minuto y 45 segundos
        if (canvasVideo != null) 
        {
            canvasVideo.SetActive(false);
        }
        Debug.Log("<color=green>[GestorEscena3] Video tutorial finalizado. Simulador liberado para el estudiante.</color>");
        DarPorVistoElVideo();
    }
}