using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using TMPro;
using System.Collections;

public class SplashFloatingController : MonoBehaviour, IReiniciable
{
    [Header("Referencias")]
    public VideoPlayer vidPlayer;
    public CanvasGroup canvasGroup; 
    public TextMeshProUGUI textoTitulo;

    [Header("Configuración de Tiempo")]
    public float fadeSpeed = 1.0f; // <--- ESTA ES LA VARIABLE QUE FALTABA
    public float tiempoExtraAlFinal = 1.0f;

    [Header("Al terminar el splash")]
    [Tooltip("Se dispara cuando el splash ya se ha desvanecido, justo antes de " +
             "ocultarse. Sirve para encadenar lo que viene despues (por ejemplo, " +
             "arrancar el video explicativo de la Escena 3).")]
    public UnityEvent OnSplashTerminado;

    // Todo esto vivia en Start, y Start NO se vuelve a ejecutar cuando un objeto
    // se reactiva. Como el splash termina con SetActive(false) sobre si mismo, al
    // reiniciar entre tandas la foto de la escena lo devolvia encendido pero con
    // el lienzo a alfa 0, el video parado y sin volver a disparar
    // OnSplashTerminado, que es lo que encadena el video del reto 3. De ahi que
    // tras el reinicio no apareciera ningun splash ni el video.
    //
    // En OnEnable se ejecuta en cada encendido, que es lo que hace falta.
    void OnEnable()
    {
        Arrancar();
    }

    void OnDisable()
    {
        if (vidPlayer != null) vidPlayer.loopPointReached -= IniciarCierre;
    }

    private void Arrancar()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1;
        if (textoTitulo != null) textoTitulo.canvasRenderer.SetAlpha(0);

        // Sin reproductor no hay splash que reproducir. Antes se accedia sin
        // comprobar y lanzaba NullReferenceException, abortando el arranque entero.
        if (vidPlayer == null)
        {
            Debug.LogWarning($"[SplashFloatingController] '{name}' no tiene vidPlayer " +
                             "asignado: no se reproduce splash ni se auto-oculta.", this);
            return;
        }

        // Quitar antes de poner: si no, cada reactivacion sumaria una suscripcion
        // mas y el cierre se dispararia varias veces.
        vidPlayer.loopPointReached -= IniciarCierre;
        vidPlayer.loopPointReached += IniciarCierre;

        NarradorUnico.Registrar(vidPlayer);
        NarradorUnico.TomarLaPalabra(vidPlayer);

        vidPlayer.frame = 0;
        vidPlayer.Play();
        StartCoroutine(AparecerTexto());
    }

    /// <summary>
    /// Deja el splash listo para volver a verse en la tanda siguiente.
    ///
    /// Si su objeto esta encendido (el del reto 1), arranca ya. Si esta apagado
    /// (los de los retos 2 y 3), solo se rebobina: ya lo arrancara OnEnable
    /// cuando la experiencia llegue a ese reto.
    /// </summary>
    public void ReiniciarAlInicio()
    {
        StopAllCoroutines();

        if (canvasGroup != null) canvasGroup.alpha = 1;
        if (textoTitulo != null) textoTitulo.canvasRenderer.SetAlpha(0);

        if (vidPlayer != null)
        {
            vidPlayer.Stop();
            vidPlayer.frame = 0;
        }

        if (gameObject.activeInHierarchy) Arrancar();
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

        // Se avisa ANTES de desactivar: al hacer SetActive(false) se detienen las
        // corrutinas de este objeto, asi que no habria oportunidad de avisar despues.
        OnSplashTerminado?.Invoke();

        // Finalmente ocultamos el objeto
        this.gameObject.SetActive(false);
    }
}