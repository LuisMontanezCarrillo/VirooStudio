using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

// Controles del video explicativo de la Escena 3.
// Solo anade play/pausa y cerrar: el arranque automatico y el auto-ocultado al
// terminar siguen a cargo de GestorEscena3, que no se toca.
// Los controles son locales por estudiante, igual que las fichas del explorador
// de piezas: no se sincronizan por red.
public class ControlesVideoEsc3 : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Video Player del tutorial (el mismo que usa GestorEscena3)")]
    public VideoPlayer reproductor;
    [Tooltip("El Canvas que se apaga al cerrar (Canvas_VideoEsc3)")]
    public GameObject canvasVideo;

    [Tooltip("El GestorEscena3, para avisarle de que el video se dio por visto. " +
             "Si se deja vacio se busca solo al arrancar; no hay que cablearlo.")]
    public GestorEscena3 gestor;

    [Header("Botones")]
    public Button botonPlayPausa;
    public TMP_Text etiquetaPlayPausa;
    public Button botonCerrar;

    [Header("Textos")]
    public string textoPausar = "PAUSA";
    public string textoReanudar = "REANUDAR";

    private void Awake()
    {
        if (botonPlayPausa != null) botonPlayPausa.onClick.AddListener(AlternarPlayPausa);
        if (botonCerrar != null) botonCerrar.onClick.AddListener(Cerrar);

        // Se busca aqui y no se exige cablearlo: este objeto vive dentro del canvas
        // del video y el gestor esta fuera, de modo que es facil olvidarlo y el
        // olvido no daria ningun error, solo un paso del guion que no se marca.
        if (gestor == null) gestor = FindFirstObjectByType<GestorEscena3>();
    }

    private void OnEnable()
    {
        // GestorEscena3 enciende este canvas justo antes de llamar a Play(), asi que
        // en este instante isPlaying todavia es false aunque el video vaya a arrancar.
        // Por eso la etiqueta se fija a "pausar" en vez de consultar el reproductor.
        if (etiquetaPlayPausa != null) etiquetaPlayPausa.text = textoPausar;
    }

    public void AlternarPlayPausa()
    {
        if (reproductor == null) return;

        if (reproductor.isPlaying)
        {
            reproductor.Pause();
            if (etiquetaPlayPausa != null) etiquetaPlayPausa.text = textoReanudar;
        }
        else
        {
            // Al reanudar, el video vuelve a tomar la palabra.
            NarradorUnico.TomarLaPalabra(reproductor);
            reproductor.Play();
            if (etiquetaPlayPausa != null) etiquetaPlayPausa.text = textoPausar;
        }
    }

    public void Cerrar()
    {
        if (reproductor != null) reproductor.Stop();
        if (canvasVideo != null) canvasVideo.SetActive(false);
        Debug.Log("<color=green>[ControlesVideoEsc3] Video cerrado por el estudiante.</color>");

        // Cerrar a mano cuenta igual que verlo hasta el final. Si no contara, el
        // estudiante que cierra el video se quedaria con el primer paso del reto 3
        // sin marcar, y no hay boton que vuelva a reproducirlo.
        if (gestor != null) gestor.DarPorVistoElVideo();
    }
}
