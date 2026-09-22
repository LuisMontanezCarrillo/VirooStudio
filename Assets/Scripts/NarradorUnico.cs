using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Arbitro de la palabra: garantiza que en la experiencia nunca suenen dos
/// narraciones a la vez.
///
/// El problema que resuelve: la voz del carrusel, la del cuestionario, la del
/// video de la Escena 3, la del splash y la guia por voz del simulador viven en
/// componentes distintos que no se conocen entre si. Si un estudiante activaba
/// una diapositiva del carrusel justo antes de la transicion al reto 3, esa
/// locucion seguia sonando por encima del video que arranca al llegar, y las
/// dos voces se cruzaban.
///
/// Regla, fijada por el autor del simulador: manda el ultimo que habla. Quien
/// empieza a narrar silencia a todos los demas.
///
/// Los videos se pausan en vez de detenerse, para que el control de PAUSA y
/// REANUDAR de la Escena 3 siga teniendo sentido.
/// </summary>
public static class NarradorUnico
{
    private static readonly List<AudioSource> _voces = new List<AudioSource>();
    private static readonly List<VideoPlayer> _videos = new List<VideoPlayer>();

    /// Con la recarga de dominio desactivada, el estado estatico sobrevive entre
    /// sesiones de Play y quedarian referencias muertas de la sesion anterior.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        _voces.Clear();
        _videos.Clear();
    }

    public static void Registrar(AudioSource fuente)
    {
        if (fuente != null && !_voces.Contains(fuente)) _voces.Add(fuente);
    }

    public static void Registrar(VideoPlayer video)
    {
        if (video != null && !_videos.Contains(video)) _videos.Add(video);
    }

    public static void Olvidar(AudioSource fuente) { _voces.Remove(fuente); }
    public static void Olvidar(VideoPlayer video) { _videos.Remove(video); }

    /// <summary>
    /// Silencia toda narracion en curso salvo la de quien toma la palabra.
    /// Se llama JUSTO ANTES de arrancar la narracion propia.
    /// </summary>
    public static void TomarLaPalabra(Object quien)
    {
        for (int i = _voces.Count - 1; i >= 0; i--)
        {
            var v = _voces[i];
            if (v == null) { _voces.RemoveAt(i); continue; }
            if (ReferenceEquals(v, quien)) continue;
            if (v.isPlaying) v.Stop();
        }

        for (int i = _videos.Count - 1; i >= 0; i--)
        {
            var v = _videos[i];
            if (v == null) { _videos.RemoveAt(i); continue; }
            if (ReferenceEquals(v, quien)) continue;
            if (v.isPlaying) v.Pause();
        }
    }

    /// <summary>Silencio total. Util en las transiciones entre retos.</summary>
    public static void Callar()
    {
        TomarLaPalabra(null);
    }
}
