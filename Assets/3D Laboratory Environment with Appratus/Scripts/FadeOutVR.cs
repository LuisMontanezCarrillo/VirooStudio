using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using ViroLab.Multiusuario;

/// <summary>
/// Cierre del reto 1: espera, funde a negro y pasa al reto siguiente.
///
/// El fundido ya no se construye aquí. Estaba copiado palabra por palabra en
/// este archivo y en GestorCuestionario, y si la transición se interrumpía a
/// medias el lienzo negro quedaba huérfano colgado de la cámara. Ahora lo lleva
/// FundidoDePantalla, que es uno solo para toda la experiencia.
/// </summary>
public class FadeOutVR : MonoBehaviour
{
    [Header("Ritmo")]
    public float tiempoDeEspera = 4f;
    public float duracionFade = 2f;

    [Header("Avance al reto siguiente")]
    [Tooltip("Asigna aquí el AvanceEnGrupo del reto 1. Con él asignado, el salto " +
             "lo hace VIROO moviendo a los dos participantes a la vez, que es lo " +
             "único compatible con la protección frente a choques. Mientras esté " +
             "vacío se mantiene el camino antiguo, que mueve solo a quien termina.")]
    public AvanceEnGrupo avanceEnGrupo;

    [Tooltip("SOLO para el camino antiguo, mientras no esté cableado el avance en " +
             "grupo. Destino del teletransporte individual.")]
    public Transform puntoDeDestino;

    [Header("Eventos de Red (VIROO)")]
    [Tooltip("Coloca aquí los Canvas, Puertas y Audios usando los componentes de red de VIROO")]
    public UnityEvent OnTeletransporteEjecutado;

    public void IniciarFadeOut()
    {
        StartCoroutine(RutinaFadeYTeletransporte());
    }

    IEnumerator RutinaFadeYTeletransporte()
    {
        yield return new WaitForSeconds(tiempoDeEspera);

        // --- CAMINO BUENO: el grupo avanza junto ---
        // El fundido y el encendido de lo que toque cuelgan de los eventos de
        // AvanceEnGrupo, cableados en el Inspector, porque el salto no ocurre
        // hasta que han terminado los dos participantes.
        if (avanceEnGrupo != null)
        {
            avanceEnGrupo.AnunciarQueTermine();
            yield break;
        }

        // --- CAMINO ANTIGUO ---
        // Mueve solo a quien termina, así que los dos visores acaban con mapeos
        // distintos entre la sala y el mundo virtual y el aviso de proximidad
        // deja de medir sobre posiciones reales. Se conserva únicamente para que
        // la experiencia siga funcionando mientras AvanceEnGrupo no está cableado.
        Debug.LogWarning("[FadeOutVR] Avance individual: no hay AvanceEnGrupo asignado. " +
                         "Con dos participantes en sala esto rompe la protección frente a " +
                         "choques.", this);

        var fundido = FundidoDePantalla.Instancia;
        yield return fundido.Oscurecer(duracionFade);

        if (puntoDeDestino != null)
        {
            Transform camaraVR = Camera.main.transform;
            Transform rootJugador = camaraVR.root;

            float diferenciaRotacion = puntoDeDestino.eulerAngles.y - camaraVR.eulerAngles.y;
            rootJugador.Rotate(0, diferenciaRotacion, 0);

            Vector3 diferenciaPosicion = puntoDeDestino.position - camaraVR.position;
            diferenciaPosicion.y = 0;

            rootJugador.position += diferenciaPosicion;
        }

        // Los Canvas, la puerta y el audio se delegan al sistema de red de VIROO.
        OnTeletransporteEjecutado?.Invoke();

        yield return new WaitForSeconds(0.5f);

        yield return fundido.Aclarar(duracionFade);
    }
}
