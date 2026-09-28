using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Arbitro de la palabra: garantiza que en la experiencia nunca suenen dos
/// narraciones a la vez, y que una locucion corta no se coma a la anterior.
///
/// EL PRIMER PROBLEMA: DOS VOCES A LA VEZ
/// --------------------------------------
/// La voz del carrusel, la del cuestionario, la del video de la Escena 3, la
/// del splash y la guia por voz del simulador viven en componentes distintos
/// que no se conocen entre si. Si un estudiante activaba una diapositiva del
/// carrusel justo antes de la transicion al reto 3, esa locucion seguia sonando
/// por encima del video que arranca al llegar.
///
/// Para eso esta TomarLaPalabra: manda el ultimo que habla. Lo usan las
/// transiciones y todo lo que el estudiante dispara a proposito.
///
/// EL SEGUNDO PROBLEMA: FRASES CORTADAS
/// ------------------------------------
/// La guia del simulador no habla porque nadie la llame, sino porque el motor
/// cambia de estado, y los estados encadenan. En cuanto arranca la bomba de
/// producto, el liquido alcanza la temperatura de retencion uno o dos segundos
/// despues: la locucion de la bomba dura 18,5 segundos y la siguiente la
/// cortaba a los dos. Peor aun, las correcciones se evaluan en el mismo Update
/// que la secuencia, asi que una correccion podia cortar una frase nacida en el
/// mismo fotograma.
///
/// Para eso esta Pedir: en vez de imponerse, se pide turno con una prioridad.
/// Lo que informa espera; lo que corrige se adelanta; lo que urge interrumpe.
///
/// Las correcciones ademas caducan. Una correccion que llega treinta segundos
/// tarde, cuando el estudiante ya arreglo lo que hacia mal, confunde mas que
/// ayuda: si no alcanza a sonar a tiempo, se descarta.
///
/// Los videos se pausan en vez de detenerse, para que el control de PAUSA y
/// REANUDAR de la Escena 3 siga teniendo sentido.
/// </summary>
public static class NarradorUnico
{
    /// <summary>Con que insistencia se pide la palabra.</summary>
    public enum Prioridad
    {
        /// Relleno. Si hay alguien hablando se descarta en vez de esperar.
        Ambiente = 0,
        /// Lo normal: informa del paso en curso. Espera su turno.
        Secuencia = 1,
        /// Avisa de un error del estudiante. Se adelanta a la secuencia.
        Correccion = 2,
        /// Corta lo que haya y vacia la cola. Alarmas y arranques de reto.
        Urgente = 3,
    }

    /// <summary>Cuantas locuciones como maximo esperan turno.</summary>
    public const int MaximoEnCola = 3;

    /// <summary>Margen tras arrancar antes de dar por terminada una locucion.</summary>
    private const float MargenDeArranque = 0.15f;

    private struct Turno
    {
        public AudioSource fuente;
        public AudioClip clip;
        public Prioridad prioridad;
        public float pedidoEn;
        public float caducidad;   // 0 = no caduca
        public bool cede;         // se aparta ante cualquier peticion con peso
    }

    private static readonly List<AudioSource> _voces = new List<AudioSource>();
    private static readonly List<VideoPlayer> _videos = new List<VideoPlayer>();
    private static readonly List<Turno> _cola = new List<Turno>();

    private static AudioSource _hablando;
    private static float _arrancoEn;
    private static Turno _turnoActual;

    /// Con la recarga de dominio desactivada, el estado estatico sobrevive entre
    /// sesiones de Play y quedarian referencias muertas de la sesion anterior.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        _voces.Clear();
        _videos.Clear();
        _cola.Clear();
        _hablando = null;
        _turnoActual = default;
        _turnero = null;
        _vigiladas.Clear();
        _sonaban.Clear();
        _ultimaBusqueda = -999f;
    }

    // ================================================================
    //  Registro
    // ================================================================

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

    // ================================================================
    //  Interrupcion: manda el ultimo que habla
    // ================================================================

    /// <summary>
    /// Silencia toda narracion en curso salvo la de quien toma la palabra.
    /// Se llama JUSTO ANTES de arrancar la narracion propia.
    ///
    /// Vacia tambien la cola: lo que esperaba turno pertenecia a lo que se acaba
    /// de interrumpir, y dejarlo sonaria fuera de sitio en el reto siguiente.
    /// </summary>
    public static void TomarLaPalabra(Object quien)
    {
        _cola.Clear();
        SilenciarAlResto(quien);
    }

    /// Calla a los demas SIN tocar la cola. Es lo que usa el turnero al pasar al
    /// turno siguiente: alli la cola es justo lo que no se debe perder.
    private static void SilenciarAlResto(Object quien)
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

        _hablando = quien as AudioSource;
        _arrancoEn = Time.unscaledTime;
        _turnoActual = default;
    }

    /// <summary>Silencio total. Util en las transiciones entre retos.</summary>
    public static void Callar()
    {
        TomarLaPalabra(null);
        _hablando = null;
    }

    // ================================================================
    //  Turnos
    // ================================================================

    /// <summary>
    /// Pide la palabra para un clip. Segun la prioridad y lo que este sonando,
    /// suena ya, espera turno o se descarta.
    /// </summary>
    /// <param name="caducidad">
    /// Segundos que el turno sigue teniendo sentido. Pasados, se descarta sin
    /// sonar. Cero significa que no caduca.
    /// </param>
    /// <param name="cedeLaPalabra">
    /// La locucion toma la palabra, pero la suelta ante la primera peticion con
    /// peso en vez de hacerla esperar. Para lo que es contexto y no instruccion.
    /// </param>
    public static void Pedir(AudioSource fuente, AudioClip clip, Prioridad prioridad,
                             float caducidad = 0f, bool cedeLaPalabra = false)
    {
        if (fuente == null || clip == null) return;

        Registrar(fuente);
        AsegurarTurnero();

        var turno = new Turno
        {
            fuente = fuente,
            clip = clip,
            prioridad = prioridad,
            pedidoEn = Time.unscaledTime,
            caducidad = caducidad,
            cede = cedeLaPalabra,
        };

        if (prioridad == Prioridad.Urgente)
        {
            _cola.Clear();
            Arrancar(turno);
            return;
        }

        if (!AlguienHablando())
        {
            Arrancar(turno);
            return;
        }

        // Quien cede la palabra se aparta. Lo usa la bienvenida del simulador,
        // que dura 30 s: es contexto, no instruccion, y en cuanto el estudiante
        // empieza a operar deja de venir a cuento.
        if (_turnoActual.cede && prioridad >= Prioridad.Secuencia)
        {
            Arrancar(turno);
            return;
        }

        // Relleno con la palabra ocupada: se pierde en vez de esperar.
        if (prioridad == Prioridad.Ambiente) return;

        if (prioridad == Prioridad.Correccion)
        {
            for (int i = 0; i < _cola.Count; i++)
                if (_cola[i].clip == clip) return;   // ya espera turno

            // Por delante de lo que solo informa, detras de las correcciones que
            // ya esperaban.
            int destino = 0;
            while (destino < _cola.Count && _cola[destino].prioridad >= Prioridad.Correccion) destino++;
            _cola.Insert(destino, turno);
        }
        else
        {
            // MANDA LA MAS RECIENTE.
            //
            // La guia del simulador tiene 159 s de locucion para una corrida que
            // un estudiante despachado resuelve en 113 s: va a ir por detras
            // pase lo que pase. Puesta a elegir, una guia de proceso retrasada es
            // mas util hablando de donde esta el estudiante AHORA que de donde
            // estuvo. Medido sobre una corrida tipica, guardar la cola por orden
            // de llegada dejaba locuciones sonando hasta 35 s tarde; quedarse con
            // la ultima lo baja a 5 s.
            for (int i = _cola.Count - 1; i >= 0; i--)
                if (_cola[i].prioridad == Prioridad.Secuencia) _cola.RemoveAt(i);

            _cola.Add(turno);
        }

        // Tope de seguridad. Con la regla de arriba la cola no deberia crecer,
        // pero un estudiante que se atasca puede acumular correcciones.
        while (_cola.Count > MaximoEnCola)
        {
            int peor = 0;
            for (int i = 1; i < _cola.Count; i++)
                if (_cola[i].prioridad < _cola[peor].prioridad) peor = i;
            _cola.RemoveAt(peor);
        }
    }

    private static bool AlguienHablando()
    {
        if (_hablando == null) return false;
        if (Time.unscaledTime - _arrancoEn < MargenDeArranque) return true;
        return _hablando.isPlaying;
    }

    private static void Arrancar(Turno t)
    {
        SilenciarAlResto(t.fuente);
        t.fuente.Stop();
        t.fuente.clip = t.clip;
        t.fuente.Play();
        _hablando = t.fuente;
        _arrancoEn = Time.unscaledTime;
        _turnoActual = t;
    }

    /// <summary>La llama el turnero una vez por fotograma.</summary>
    private static void Avanzar()
    {
        if (_cola.Count == 0) return;
        if (AlguienHablando()) return;

        while (_cola.Count > 0)
        {
            var t = _cola[0];
            _cola.RemoveAt(0);

            if (t.fuente == null || t.clip == null) continue;
            if (t.caducidad > 0f && Time.unscaledTime - t.pedidoEn > t.caducidad) continue;

            Arrancar(t);
            return;
        }
    }

    // ================================================================
    //  El turnero
    // ================================================================

    private static Turnero _turnero;

    /// El turnero hace dos cosas: avanzar la cola y vigilar a las voces que no
    /// piden turno. Lo segundo tiene que estar en marcha desde el principio,
    /// porque en el reto 1 nadie pide turno: las narraciones se disparan con un
    /// AudioSource.Play cableado en el Inspector. Si se esperara a la primera
    /// peticion, el turnero no existiria durante todo el reto 1 y las voces se
    /// seguirian pisando.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ArrancarElTurnero() { AsegurarTurnero(); }

    /// La cola necesita que alguien la mire cada fotograma, y una clase estatica
    /// no tiene Update. Se crea un objeto oculto la primera vez que hace falta.
    private static void AsegurarTurnero()
    {
        if (_turnero != null) return;

        var obj = new GameObject("NarradorUnico (turnos)");
        obj.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(obj);
        _turnero = obj.AddComponent<Turnero>();
    }

    private class Turnero : MonoBehaviour
    {
        private void Update() { Avanzar(); Vigilar(); }
    }

    // ================================================================
    //  Vigilancia de las voces que no piden turno
    // ================================================================

    /// Duracion minima y maxima, en segundos, para considerar que un clip es una
    /// narracion. Por debajo son efectos (clics, golpes); por encima, musica y
    /// ambientes largos.
    private const float NarracionMin = 4f;
    private const float NarracionMax = 40f;

    private const float CadaCuantoSeBusca = 2f;

    private static readonly List<AudioSource> _vigiladas = new List<AudioSource>();
    private static readonly List<bool> _sonaban = new List<bool>();
    private static float _ultimaBusqueda = -999f;

    /// <summary>
    /// Hace cumplir la regla incluso a quien no pide turno.
    ///
    /// Las narraciones del reto 1 (casillero, ropa, botas) se disparan con un
    /// AudioSource.Play cableado en un UnityEvent del Inspector, sin pasar por
    /// aqui. Por eso se solapaban por mucho que el resto de la experiencia
    /// respetara el arbitro: nadie les habia dicho que existiera.
    ///
    /// En vez de pedir que se cablee cada una, se vigilan todas: cuando una
    /// empieza a sonar por su cuenta, se le da la palabra y se calla al resto.
    /// Solo se consideran narraciones los clips que no son bucles y duran entre
    /// cuatro y cuarenta segundos, de modo que los clics y la musica de fondo
    /// quedan fuera.
    /// </summary>
    private static void Vigilar()
    {
        if (Time.unscaledTime - _ultimaBusqueda >= CadaCuantoSeBusca)
        {
            _ultimaBusqueda = Time.unscaledTime;
            RefrescarVigiladas();
        }

        for (int i = _vigiladas.Count - 1; i >= 0; i--)
        {
            var v = _vigiladas[i];
            if (v == null) { _vigiladas.RemoveAt(i); _sonaban.RemoveAt(i); continue; }

            bool ahora = v.isPlaying;
            bool antes = _sonaban[i];
            _sonaban[i] = ahora;

            if (!ahora || antes) continue;                  // solo el flanco de subida
            if (ReferenceEquals(v, _hablando)) continue;    // la arranco el propio arbitro
            if (!EsNarracion(v)) continue;

            TomarLaPalabra(v);
        }
    }

    private static bool EsNarracion(AudioSource v)
    {
        if (v.loop || v.clip == null) return false;
        float d = v.clip.length;
        return d >= NarracionMin && d <= NarracionMax;
    }

    private static void RefrescarVigiladas()
    {
        var todas = Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (todas == null) return;

        foreach (var a in todas)
        {
            if (a == null || _vigiladas.Contains(a)) continue;
            _vigiladas.Add(a);
            _sonaban.Add(a.isPlaying);   // lo que ya sonaba no cuenta como flanco
        }
    }
}
