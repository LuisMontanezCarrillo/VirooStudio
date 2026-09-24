using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fundido a negro delante de los ojos del participante.
///
/// QUE RESUELVE
/// ------------
/// El mismo fundido estaba escrito dos veces, copiado palabra por palabra, en
/// FadeOutVR y en GestorCuestionario: los dos creaban un Canvas en espacio de
/// mundo colgado de la camara, con un cuadro negro de 5000 x 5000 a escala
/// 0,001, y lo destruian al terminar. Ademas de estar duplicado, ese esquema
/// tiene un fallo latente: si la transicion se interrumpe a medio fundido (y el
/// reinicio entre tandas la interrumpe), el Canvas se queda huerfano y el
/// estudiante se queda mirando un negro del que nadie va a sacarlo.
///
/// Aqui el velo es uno solo y persiste. Cualquiera puede pedir oscurecer o
/// aclarar, y una peticion nueva cancela la anterior en vez de sumarse a ella.
/// Transparente() es la valvula de seguridad: devuelve la vista pase lo que
/// pase.
/// </summary>
[AddComponentMenu("ViroLab/Fundido de pantalla")]
public class FundidoDePantalla : MonoBehaviour
{
    public const float DuracionPorDefecto = 2f;

    private static FundidoDePantalla _instancia;

    private Canvas _lienzo;
    private Image _velo;
    private Coroutine _enCurso;

    /// Con la recarga de dominio desactivada el estado estatico sobrevive entre
    /// sesiones de Play, y quedaria apuntando a un objeto ya destruido.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void OlvidarEntreSesiones() { _instancia = null; }

    /// <summary>El velo de la experiencia. Se crea la primera vez que se pide.</summary>
    public static FundidoDePantalla Instancia
    {
        get
        {
            if (_instancia == null)
            {
                _instancia = FindObjectOfType<FundidoDePantalla>();
                if (_instancia == null)
                    _instancia = new GameObject("Fundido de pantalla").AddComponent<FundidoDePantalla>();
            }
            return _instancia;
        }
    }

    private void Awake()
    {
        if (_instancia != null && _instancia != this) { Destroy(this); return; }
        _instancia = this;
    }

    // ================================================================
    //  Peticiones
    // ================================================================

    /// <summary>Oscurece hasta negro. Para usar con yield return desde una corrutina.</summary>
    public IEnumerator Oscurecer(float duracion = DuracionPorDefecto)
    {
        yield return Interpolar(1f, duracion);
    }

    /// <summary>Aclara hasta devolver la vista.</summary>
    public IEnumerator Aclarar(float duracion = DuracionPorDefecto)
    {
        yield return Interpolar(0f, duracion);
    }

    /// <summary>
    /// Version para cablear en un UnityEvent del Inspector, que no sabe esperar
    /// corrutinas. Oscurece con la duracion por defecto y devuelve el control.
    /// </summary>
    public void OscurecerAhora() { StartCoroutine(Oscurecer()); }

    /// <summary>Lo mismo para aclarar. Ver OscurecerAhora.</summary>
    public void AclararAhora() { StartCoroutine(Aclarar()); }

    /// <summary>Negro inmediato, sin transicion.</summary>
    public void Negro()
    {
        Preparar();
        DetenerLoQueHubiera();
        _velo.color = new Color(0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// Devuelve la vista de inmediato y cancela cualquier fundido en marcha.
    /// Se llama al reiniciar la experiencia y ante cualquier duda: mas vale un
    /// corte brusco que dejar a alguien a oscuras dentro de un visor.
    /// </summary>
    public void Transparente()
    {
        DetenerLoQueHubiera();
        if (_velo == null) return;
        _velo.color = new Color(0f, 0f, 0f, 0f);
        if (_lienzo != null) _lienzo.gameObject.SetActive(false);
    }

    // ================================================================
    //  Interioridades
    // ================================================================

    private void DetenerLoQueHubiera()
    {
        if (_enCurso != null) { StopCoroutine(_enCurso); _enCurso = null; }
    }

    private IEnumerator Interpolar(float alfaFinal, float duracion)
    {
        Preparar();
        DetenerLoQueHubiera();
        _enCurso = StartCoroutine(Rutina(alfaFinal, duracion));
        yield return _enCurso;
    }

    private IEnumerator Rutina(float alfaFinal, float duracion)
    {
        float alfaInicial = _velo.color.a;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(alfaInicial, alfaFinal, duracion <= 0f ? 1f : t / duracion);
            _velo.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        _velo.color = new Color(0f, 0f, 0f, alfaFinal);
        _enCurso = null;

        // Ya transparente, el velo deja de dibujarse. Con el Canvas encendido
        // seguiria costando una pasada de interfaz por fotograma sin pintar nada.
        if (alfaFinal <= 0f && _lienzo != null) _lienzo.gameObject.SetActive(false);
    }

    private void Preparar()
    {
        if (_lienzo == null)
        {
            var obj = new GameObject("Velo_Fundido");
            _lienzo = obj.AddComponent<Canvas>();
            _lienzo.renderMode = RenderMode.WorldSpace;
            _lienzo.sortingOrder = 999;

            var cuadro = new GameObject("Cuadro_Negro");
            cuadro.transform.SetParent(obj.transform, false);
            _velo = cuadro.AddComponent<Image>();
            _velo.color = new Color(0f, 0f, 0f, 0f);
            _velo.rectTransform.sizeDelta = new Vector2(5000f, 5000f);

            // Sin esto el velo se come los clics del rayo del mando mientras dura
            // el fundido.
            _velo.raycastTarget = false;
        }

        // La camara activa puede cambiar a lo largo de la sesion, asi que el velo
        // se vuelve a colgar de la que haya en cada peticion en vez de fiarse de
        // la que hubiera al crearlo.
        var camara = Camera.main != null ? Camera.main.transform : null;
        if (camara != null && _lienzo.transform.parent != camara)
            _lienzo.transform.SetParent(camara, false);

        _lienzo.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        _lienzo.transform.localRotation = Quaternion.identity;
        _lienzo.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        _lienzo.gameObject.SetActive(true);
    }
}
