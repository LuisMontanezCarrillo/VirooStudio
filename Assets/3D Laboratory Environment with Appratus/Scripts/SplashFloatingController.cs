using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class SplashFloatingController : MonoBehaviour
{
    [Header("Referencias")]
    public VideoPlayer vidPlayer;
    public CanvasGroup canvasGroup; 
    public TextMeshProUGUI textoTitulo;

    [Header("Configuraci�n de Tiempo")]
    public float fadeSpeed = 1.0f; // <--- ESTA ES LA VARIABLE QUE FALTABA
    public float tiempoExtraAlFinal = 1.0f;

    [Header("Al terminar el splash")]
    [Tooltip("Se dispara cuando el splash ya se ha desvanecido, justo antes de " +
             "ocultarse. Sirve para encadenar lo que viene despues (por ejemplo, " +
             "arrancar el video explicativo de la Escena 3).")]
    public UnityEvent OnSplashTerminado;

    void Start()
    {
        // Configuraci�n inicial de visibilidad
        if (canvasGroup != null) canvasGroup.alpha = 1;
        if (textoTitulo != null) textoTitulo.canvasRenderer.SetAlpha(0);

        // Sin reproductor no hay splash que reproducir. Antes se accedia sin
        // comprobar y lanzaba NullReferenceException, abortando el Start entero.
        if (vidPlayer == null)
        {
            Debug.LogWarning($"[SplashFloatingController] '{name}' no tiene vidPlayer " +
                             "asignado: no se reproduce splash ni se auto-oculta.", this);
            return;
        }

        // Suscribirse al evento de finalizaci�n del video
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

        // Desvanecimiento suave (aqu� se usa fadeSpeed)
        while (canvasGroup != null && canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // Se avisa ANTES de desactivar: al hacer SetActive(false) se detienen las
        // corrutinas de este objeto, asi que no habria oportunidad de avisar despues.
        OnSplashTerminado?.Invoke();

        // Finalmente ocultamos el objeto
        this.gameObject.SetActive(false);
    }
}