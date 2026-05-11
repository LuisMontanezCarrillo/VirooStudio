using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class SplashFloatingController : MonoBehaviour
{
    [Header("Referencias")]
    public VideoPlayer vidPlayer;
    public CanvasGroup canvasGroup; 
    public TextMeshProUGUI textoTitulo;

    [Header("Configuración de Tiempo")]
    public float fadeSpeed = 1.0f; // <--- ESTA ES LA VARIABLE QUE FALTABA
    public float tiempoExtraAlFinal = 1.0f; 

    void Start()
    {
        // Configuración inicial de visibilidad
        if (canvasGroup != null) canvasGroup.alpha = 1; 
        if (textoTitulo != null) textoTitulo.canvasRenderer.SetAlpha(0);
        
        // Suscribirse al evento de finalización del video
        vidPlayer.loopPointReached += IniciarCierre;
        
        vidPlayer.Play();
        StartCoroutine(AparecerTexto());
    }

    IEnumerator AparecerTexto()
    {
        yield return new WaitForSeconds(1.5f); 
        if (textoTitulo != null) textoTitulo.CrossFadeAlpha(1, fadeSpeed, false);
    }

    void IniciarCierre(VideoPlayer vp)
    {
        StartCoroutine(EsperaYDesvanecer());
    }

    IEnumerator EsperaYDesvanecer()
    {
        // Aumentamos el tiempo antes de que se oculte el Canvas
        yield return new WaitForSeconds(tiempoExtraAlFinal);

        // Desvanecimiento suave (aquí se usa fadeSpeed)
        while (canvasGroup != null && canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Finalmente ocultamos el objeto
        this.gameObject.SetActive(false);
    }
}