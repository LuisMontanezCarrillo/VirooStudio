using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;
using ViroLab.Pasteurizador.Simulator;

/// <summary>
/// Reparte todo el sonido en tres buses y agacha la planta mientras alguien
/// habla.
///
/// POR QUE EXISTE
/// --------------
/// En las pruebas del 29 de septiembre de 2026 el sonido fue el item peor
/// valorado del instrumento: 4,55 sobre 7 en "los sonidos me ayudaron a saber en
/// que estado estaba el proceso". Los comentarios dicen donde duele:
///
///   "Mucho ruido de las maquinas, y poco sonido de la explicacion"
///   "Bajo volumen"
///   "No se escucho muy bien"
///
/// El repositorio lo explica entero. El proyecto no tenia NINGUN AudioMixer: no
/// habia ningun archivo .mixer y los doce AudioSource de la escena salian con
/// OutputAudioMixerGroup vacio. Todo sonaba al mismo nivel, sin buses y sin
/// forma de bajar una cosa sin bajar las demas.
///
/// Y hay un segundo defecto, mas concreto. NarradorUnico ya arbitra quien habla:
/// cuando una narracion toma la palabra, PARA las demas narraciones y PAUSA los
/// videos. Pero no toca la planta, porque la planta no son voces. Resultado: la
/// narracion no competia con otra narracion, competia con hasta cinco bucles de
/// maquina sonando a la vez (proceso, bomba de producto, bomba de agua, caldera
/// y vapor), todos en 2D y todos al mismo volumen en cualquier punto de la sala.
///
/// Este componente cierra las dos cosas: enruta y agacha.
///
/// POR QUE ENRUTA SOLO, EN VEZ DE PEDIR QUE SE ASIGNE CADA FUENTE A MANO
/// --------------------------------------------------------------------
/// Siete de las fuentes de la planta NO EXISTEN en la escena: las crea
/// PasteurizerFxController en tiempo de ejecucion. No hay ningun campo del
/// Inspector donde asignarlas. Cualquier solucion que dependa de abrir la escena
/// y asignar grupos uno por uno deja esas siete fuera, que son justamente las
/// que hacen el ruido del que se quejaron los estudiantes.
///
/// POR QUE FUNCIONA AUNQUE NO HAYA MEZCLADOR
/// -----------------------------------------
/// La leccion del aro invisible: un mecanismo que depende de que alguien se
/// acuerde de cablear su parte falla en silencio, y se descubre en la sala.
///
/// Asi que hay dos modos. Con mezclador asignado se trabaja en decibelios sobre
/// los buses, que es lo correcto. Sin mezclador, el agache se aplica directamente
/// sobre el volumen de las fuentes de planta. Se oye peor, pero se oye: la mejora
/// que mas pesa en el instrumento no queda pendiente de un campo vacio. El
/// componente dice en consola, al arrancar, en cual de los dos modos esta.
///
/// EL ORDEN DE EJECUCION IMPORTA
/// -----------------------------
/// PasteurizerFxController reescribe el volumen de sus fuentes en CADA Update,
/// para que sus barritas del Inspector se puedan mover durante Play. Si el
/// agache se aplicara antes, lo pisaria. De ahi el DefaultExecutionOrder(100):
/// este componente corre despues que el resto y tiene la ultima palabra.
/// </summary>
[AddComponentMenu("ViroLab/Audio/Mezclador de audio")]
[DefaultExecutionOrder(100)]
public class MezcladorDeAudio : MonoBehaviour, IReiniciable
{
    /// A que bus va cada fuente.
    public enum Destino { Voz, Planta, Interfaz }

    // ================================================================
    //  El mezclador
    // ================================================================

    [Header("El mezclador")]
    [Tooltip("Deje vacio para trabajar sin mezclador: el agache se aplicara sobre " +
             "el volumen de cada fuente de planta. Funciona, pero sin buses no hay " +
             "control fino ni efectos.")]
    public AudioMixer mezclador;

    public AudioMixerGroup grupoVoz;
    public AudioMixerGroup grupoPlanta;
    public AudioMixerGroup grupoInterfaz;

    [Header("Nombres de los parametros expuestos")]
    [Tooltip("Tal y como aparecen en la pestana Exposed Parameters del mezclador. " +
             "Si uno no coincide, se avisa en consola nombrandolo: no se queda callado.")]
    public string paramGeneral  = "VolumenGeneral";
    public string paramVoz      = "VolumenVoz";
    public string paramPlanta   = "VolumenPlanta";
    public string paramInterfaz = "VolumenInterfaz";

    // ================================================================
    //  Niveles
    // ================================================================

    [Header("Niveles, en decibelios. Se pueden mover EN VIVO durante Play")]
    [Range(-40f, 10f)] public float nivelGeneral  = 0f;

    [Tooltip("Por encima de cero porque 'bajo volumen' y 'no se escucho muy bien' " +
             "fueron dos de los cuatro comentarios sobre sonido.")]
    [Range(-40f, 10f)] public float nivelVoz      = 2f;

    [Tooltip("Por debajo de cero porque hasta cinco bucles de maquina pueden " +
             "sonar a la vez y se suman.")]
    [Range(-40f, 10f)] public float nivelPlanta   = -6f;

    [Range(-40f, 10f)] public float nivelInterfaz = -3f;

    // ================================================================
    //  El agache
    // ================================================================

    [Header("Agache de la planta mientras alguien habla")]
    [Tooltip("Cuantos decibelios baja la planta. Nueve es una bajada clara sin " +
             "llegar a apagar la maquina, que tambien informa.")]
    [Range(0f, 30f)] public float agache = 9f;

    [Tooltip("Rapido al bajar, para no comerse la primera palabra.")]
    public float segundosDeBajada = 0.15f;

    [Tooltip("Lento al subir, para que la planta no vuelva de golpe entre frase y frase.")]
    public float segundosDeSubida = 0.70f;

    // ================================================================
    //  Reparto
    // ================================================================

    [Header("Reparto de las fuentes")]
    [Tooltip("Segundos entre rastreos. Hace falta repetir porque siete fuentes de " +
             "planta se crean en ejecucion y porque en una sala VIROO los " +
             "participantes entran cuando entran.")]
    public float segundosEntreRastreos = 2f;

    [Tooltip("Fuentes que van al bus de voz pase lo que pase. Solo para corregir " +
             "una clasificacion automatica equivocada; lo normal es dejarlo vacio.")]
    public AudioSource[] siempreVoz;
    public AudioSource[] siemprePlanta;
    public AudioSource[] siempreInterfaz;

    [Tooltip("Reasignar tambien las fuentes que ya tienen un grupo puesto a mano. " +
             "Apagado respeta lo que haya en el Inspector.")]
    public bool reasignarSiempre = false;

    [Tooltip("Escribir en consola cada fuente repartida y el bus que le toca. " +
             "Dejelo encendido para la sesion de comprobacion en la sala.")]
    public bool log = true;

    // ================================================================
    //  Estado
    // ================================================================

    /// Misma horquilla que NarradorUnico usa para decidir que es una narracion.
    /// Se repite el criterio a proposito: si alli cambia, aqui tiene que cambiar
    /// igual, y tenerlo escrito al lado lo hace visible.
    private const float NarracionMin = 4f;
    private const float NarracionMax = 40f;

    private readonly HashSet<AudioSource> _repartidas = new HashSet<AudioSource>();
    private readonly HashSet<AudioSource> _vocesConocidas = new HashSet<AudioSource>();
    private readonly List<AudioSource> _planta = new List<AudioSource>();
    private readonly Dictionary<AudioSource, float> _volumenSinAgachar =
        new Dictionary<AudioSource, float>();

    private float _proximoRastreo;
    private float _agacheActual;      // en dB, de 0 a 'agache'
    private bool _hayMezclador;
    private bool _avisoDeParametro;

    // ================================================================
    //  Ciclo
    // ================================================================

    private void Awake()
    {
        _hayMezclador = mezclador != null
                        && grupoVoz != null && grupoPlanta != null && grupoInterfaz != null;

        if (!log) return;

        if (_hayMezclador)
            Debug.Log("[MezcladorDeAudio] Con mezclador: los niveles y el agache van " +
                      "en decibelios sobre los buses.");
        else
            Debug.LogWarning("[MezcladorDeAudio] SIN mezclador (falta el AudioMixer o " +
                             "alguno de los tres grupos). El agache se aplicara sobre el " +
                             "volumen de cada fuente de planta. Funciona, pero los niveles " +
                             "por bus no tienen efecto.");
    }

    private void Update()
    {
        if (Time.unscaledTime >= _proximoRastreo)
        {
            _proximoRastreo = Time.unscaledTime + Mathf.Max(0.25f, segundosEntreRastreos);
            Rastrear();
        }

        MoverElAgache();
        Aplicar();
    }

    /// <summary>
    /// Entre tandas. Los visores de la pareja siguiente traen fuentes nuevas, y
    /// el agache tiene que empezar en cero aunque la tanda anterior terminara a
    /// mitad de una frase.
    /// </summary>
    public void ReiniciarAlInicio()
    {
        _repartidas.Clear();
        _planta.Clear();
        _volumenSinAgachar.Clear();
        _agacheActual = 0f;
        _proximoRastreo = 0f;
        enabled = true;
    }

    // ================================================================
    //  Reparto
    // ================================================================

    private void Rastrear()
    {
        RefrescarLasVoces();

        var todas = Object.FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (todas == null) return;

        foreach (var a in todas)
        {
            if (a == null || _repartidas.Contains(a)) continue;
            _repartidas.Add(a);

            var destino = Clasificar(a);
            if (destino == Destino.Planta && !_planta.Contains(a)) _planta.Add(a);

            if (!_hayMezclador)
            {
                if (log)
                    Debug.Log($"[MezcladorDeAudio] '{Ruta(a)}' -> {destino} " +
                              $"(sin mezclador: solo cuenta para el agache)");
                continue;
            }

            if (a.outputAudioMixerGroup != null && !reasignarSiempre)
            {
                if (log)
                    Debug.Log($"[MezcladorDeAudio] '{Ruta(a)}' ya tenia grupo " +
                              $"'{a.outputAudioMixerGroup.name}'. Se respeta.");
                continue;
            }

            a.outputAudioMixerGroup = destino == Destino.Voz    ? grupoVoz
                                    : destino == Destino.Planta ? grupoPlanta
                                                                : grupoInterfaz;

            if (log) Debug.Log($"[MezcladorDeAudio] '{Ruta(a)}' -> {destino}");
        }
    }

    /// <summary>
    /// A que bus pertenece una fuente.
    ///
    /// Primero manda lo que se haya puesto a mano. Despues van las reglas por
    /// nombre, que son las fiables: los nombres de este proyecto son regulares
    /// (FX_ para la planta, SND_ y Canvas_ para la interfaz). Solo al final se
    /// mira la duracion del clip, que es el criterio de NarradorUnico y el unico
    /// que queda para una narracion suelta.
    ///
    /// Lo que no encaja en nada va a interfaz Y SE AVISA. Mandarlo al bus
    /// equivocado en silencio es como no hacer nada: se descubre en la sala.
    /// </summary>
    public Destino Clasificar(AudioSource a)
    {
        if (Contiene(siempreVoz, a))      return Destino.Voz;
        if (Contiene(siemprePlanta, a))   return Destino.Planta;
        if (Contiene(siempreInterfaz, a)) return Destino.Interfaz;

        // Las fuentes que alguien usa PARA HABLAR, preguntando a quien las usa.
        // Ni el clip ni el nombre sirven con ellas: el clip se pone en ejecucion
        // y el nombre es el del mueble ("Smart led TV", "Simulador"), no el de
        // nadie hablando.
        if (_vocesConocidas.Contains(a)) return Destino.Voz;

        string n = a.name;

        // Las siete que crea PasteurizerFxController en ejecucion, mas los
        // ambientes de sala. Son todo el ruido de maquina de la experiencia.
        if (n.StartsWith("FX_") || Tiene(n, "RoomTone") || Tiene(n, "Ambiente"))
            return Destino.Planta;

        // Clics, errores y avisos de los lienzos.
        if (n.StartsWith("SND_") || n.StartsWith("Canvas_"))
            return Destino.Interfaz;

        if (Tiene(n, "Voz") || Tiene(n, "Narra") || Tiene(n, "VO"))
            return Destino.Voz;

        // Un bucle largo sin nombre reconocible es ambiente, no una voz.
        if (a.loop) return Destino.Planta;

        // Mismo criterio que NarradorUnico: entre cuatro y cuarenta segundos y
        // sin bucle es una narracion. Por debajo son golpes; por encima, musica.
        if (a.clip != null)
        {
            float d = a.clip.length;
            if (d >= NarracionMin && d <= NarracionMax) return Destino.Voz;
            if (d < NarracionMin) return Destino.Interfaz;
            return Destino.Planta;
        }

        // Sin clip asignado no hay nada que mirar: el clip se pone en ejecucion.
        // Estas son las que de verdad hay que revisar a mano.
        if (log)
            Debug.LogWarning($"[MezcladorDeAudio] '{Ruta(a)}' no tiene clip y su nombre " +
                             "no dice a que bus va. Se manda a Interfaz. Si es una voz o " +
                             "una maquina, anadala a la lista que corresponda.");
        return Destino.Interfaz;
    }

    /// <summary>
    /// Apunta que AudioSource son voz, preguntando a QUIEN LOS USA en vez de
    /// mirar el objeto.
    ///
    /// Las dos primeras avisos que salieron en la sala eran de este tipo:
    /// 'Smart led TV' y 'Simulador'. Las dos tienen el mismo problema de fondo,
    /// y no se arregla con reglas de nombre. Un AudioSource sin clip asignado y
    /// con el nombre del mueble no dice nada por si mismo; lo que lo define es
    /// el componente que lo tiene cogido para hablar por el.
    ///
    /// Se rehace en cada rastreo: un VideoPlayer puede cambiar de fuente en
    /// ejecucion, y estos objetos viven dentro de prefabs que no siempre estan
    /// activos desde el principio.
    /// </summary>
    private void RefrescarLasVoces()
    {
        _vocesConocidas.Clear();

        // 1. La salida de los videos. No es una suposicion: NarradorUnico ya
        //    trata los videos como voz desde antes de que este componente
        //    existiera, porque cuando alguien toma la palabra PAUSA los videos
        //    igual que PARA las narraciones.
        var videos = Object.FindObjectsByType<VideoPlayer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (videos != null)
            foreach (var v in videos)
            {
                if (v == null) continue;

                // En modo Direct el video no pasa por ningun AudioSource y no
                // hay nada que enrutar: ese volumen lo lleva el propio video.
                if (v.audioOutputMode != VideoAudioOutputMode.AudioSource) continue;

                ushort pistas = v.controlledAudioTrackCount;
                for (ushort i = 0; i < pistas; i++)
                {
                    var a = v.GetTargetAudioSource(i);
                    if (a != null) _vocesConocidas.Add(a);
                }
            }

        // 2. La guia por voz del tablero. Su fuente vive en el mismo objeto que
        //    PasteurizerFxController, que es la planta, asi que no vale mirar
        //    que componentes hay al lado: hay que mirar el campo.
        var guias = Object.FindObjectsByType<PasteurizerVoiceGuide>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (guias != null)
            foreach (var g in guias)
                if (g != null && g.source != null) _vocesConocidas.Add(g.source);
    }

    // ================================================================
    //  Agache
    // ================================================================

    private void MoverElAgache()
    {
        bool hablando = NarradorUnico.HayAlguienHablando();
        float objetivo = hablando ? Mathf.Max(0f, agache) : 0f;

        float segundos = hablando ? segundosDeBajada : segundosDeSubida;
        if (segundos <= 0.001f)
        {
            _agacheActual = objetivo;
            return;
        }

        // Rampa lineal en decibelios, que es como se percibe el volumen. Una
        // rampa sobre el factor lineal se oiria como un salto al principio.
        float paso = Mathf.Max(0.01f, agache) * Time.unscaledDeltaTime / segundos;
        _agacheActual = Mathf.MoveTowards(_agacheActual, objetivo, paso);
    }

    private void Aplicar()
    {
        if (_hayMezclador) { AplicarEnLosBuses(); return; }
        AplicarEnLasFuentes();
    }

    private void AplicarEnLosBuses()
    {
        Poner(paramGeneral,  nivelGeneral);
        Poner(paramVoz,      nivelVoz);
        Poner(paramPlanta,   nivelPlanta - _agacheActual);
        Poner(paramInterfaz, nivelInterfaz);
    }

    private void Poner(string parametro, float dB)
    {
        if (string.IsNullOrEmpty(parametro)) return;
        if (mezclador.SetFloat(parametro, dB)) return;

        // Un parametro mal escrito o sin exponer no da error: SetFloat devuelve
        // falso y el nivel se queda donde estuviera. Es justo el fallo mudo que
        // se descubre con las gafas puestas, asi que se dice una vez y se dice
        // nombrando el parametro.
        if (_avisoDeParametro || !log) return;
        _avisoDeParametro = true;
        Debug.LogError($"[MezcladorDeAudio] El mezclador no tiene expuesto el parametro " +
                       $"'{parametro}'. Revise la pestana Exposed Parameters: el nombre " +
                       "tiene que coincidir letra por letra. Hasta entonces, ese nivel no " +
                       "hace nada.");
    }

    /// <summary>
    /// Sin mezclador: se agacha fuente por fuente.
    ///
    /// Mientras no hay agache se anota el volumen que trae cada una, porque
    /// PasteurizerFxController lo reescribe en cada Update desde sus barritas del
    /// Inspector y hay que respetarlo. Mientras hay agache no se anota: se
    /// escribe encima, y como este componente corre el ultimo, lo que se oye es
    /// esto.
    /// </summary>
    private void AplicarEnLasFuentes()
    {
        bool agachando = _agacheActual > 0.01f;
        float factor = Mathf.Pow(10f, -_agacheActual / 20f);

        for (int i = _planta.Count - 1; i >= 0; i--)
        {
            var a = _planta[i];
            if (a == null) { _planta.RemoveAt(i); continue; }

            if (!agachando)
            {
                _volumenSinAgachar[a] = a.volume;
                continue;
            }

            float baseVol;
            if (!_volumenSinAgachar.TryGetValue(a, out baseVol))
            {
                baseVol = a.volume;
                _volumenSinAgachar[a] = baseVol;
            }

            a.volume = baseVol * factor;
        }
    }

    // ================================================================
    //  Utilidades
    // ================================================================

    private static bool Contiene(AudioSource[] lista, AudioSource a)
    {
        if (lista == null) return false;
        for (int i = 0; i < lista.Length; i++)
            if (ReferenceEquals(lista[i], a)) return true;
        return false;
    }

    private static bool Tiene(string texto, string trozo)
    {
        return texto.IndexOf(trozo, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// La ruta completa en la jerarquia. Un nombre suelto como "VozOff2" aparece
    /// dos veces en la escena y no sirve para ir a buscarlo.
    private static string Ruta(Component c)
    {
        var t = c.transform;
        string r = t.name;
        while (t.parent != null) { t = t.parent; r = t.name + "/" + r; }
        return r;
    }
}
