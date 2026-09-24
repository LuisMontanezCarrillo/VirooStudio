using UnityEngine;
using UnityEngine.UI;

public class ControladorCarrusel : MonoBehaviour, IReiniciable
{
    [Tooltip("Arrastra aquí el objeto Image 'Pantalla_Principal'")]
    public Image pantalla;

    [Tooltip("Arrastra aquí el componente AudioSource que pusimos en el Canvas")]
    public AudioSource reproductorAudio;

    [Tooltip("Coloca aquí las imágenes en orden")]
    public Sprite[] diapositivas;

    [Tooltip("Coloca aquí los audios en el MISMO ORDEN que las imágenes")]
    public AudioClip[] audiosVoz;

    // Los limites ya estaban protegidos en Siguiente() y Anterior(), de modo que
    // pulsar de mas no rompia nada. El problema era otro: el boton seguia visible
    // y pulsable sin hacer nada, y el estudiante no tenia forma de saber que habia
    // llegado al final del carrusel. Asignando aqui los dos botones, cada uno se
    // retira cuando deja de tener sentido.
    [Header("Botones de navegación")]
    [Tooltip("Botón SIGUIENTE. Se retira al llegar a la última diapositiva.")]
    public Button botonSiguiente;

    [Tooltip("Botón ANTERIOR. Se retira mientras se está en la primera diapositiva.")]
    public Button botonAnterior;

    [Tooltip("Marcado: el botón desaparece al llegar al límite. Sin marcar: sigue " +
             "visible pero apagado. Ambas opciones comunican el límite; la primera " +
             "es más clara con el rayo del mando.")]
    public bool ocultarEnLugarDeApagar = true;

    private int indiceActual = 0;

    void Awake()
    {
        NarradorUnico.Registrar(reproductorAudio);
    }

    void OnDestroy()
    {
        NarradorUnico.Olvidar(reproductorAudio);
    }

    void Start()
    {
        // Al iniciar, SOLO mostramos la primera imagen en silencio
        if (diapositivas.Length > 0)
        {
            pantalla.sprite = diapositivas[0];
        }

        ActualizarBotones();
    }

    public void Siguiente()
    {
        // Si no hemos llegado al final, avanzamos
        if (indiceActual < diapositivas.Length - 1)
        {
            indiceActual++;
            MostrarDiapositiva(indiceActual);
        }

        ActualizarBotones();
    }

    public void Anterior()
    {
        // Si no estamos en la primera, retrocedemos
        if (indiceActual > 0)
        {
            indiceActual--;
            MostrarDiapositiva(indiceActual);
        }

        ActualizarBotones();
    }

    /// <summary>
    /// Vuelve a la primera lámina, en silencio. La tanda siguiente tiene que
    /// encontrar el carrusel sin abrir, no por donde lo dejó la anterior.
    /// </summary>
    public void ReiniciarAlInicio()
    {
        DetenerAudio();
        indiceActual = 0;
        if (pantalla != null && diapositivas != null && diapositivas.Length > 0)
            pantalla.sprite = diapositivas[0];
        ActualizarBotones();
    }

    /// <summary>
    /// Detiene la locución de la diapositiva en curso. La llama la transición al
    /// salir de la zona del carrusel, para que la voz no viaje con el estudiante.
    /// </summary>
    public void DetenerAudio()
    {
        if (reproductorAudio != null) reproductorAudio.Stop();
    }

    private void MostrarDiapositiva(int indice)
    {
        // 1. Cambiar la imagen
        pantalla.sprite = diapositivas[indice];

        // 2. Manejar el audio
        // Verificamos que haya audios configurados y que correspondan al índice actual
        if (audiosVoz.Length > indice && audiosVoz[indice] != null)
        {
            // Calla cualquier otra narración de la experiencia antes de hablar.
            NarradorUnico.TomarLaPalabra(reproductorAudio);

            reproductorAudio.Stop(); // Detenemos el audio anterior si seguía sonando
            reproductorAudio.clip = audiosVoz[indice]; // Cargamos el nuevo audio
            reproductorAudio.Play(); // Le damos play
        }
    }

    /// Retira cada botón cuando ya no tiene a dónde llevar.
    private void ActualizarBotones()
    {
        int ultima = Mathf.Max(0, diapositivas.Length - 1);
        AplicarEstado(botonAnterior, indiceActual > 0);
        AplicarEstado(botonSiguiente, indiceActual < ultima);
    }

    private void AplicarEstado(Button boton, bool disponible)
    {
        if (boton == null) return;

        if (ocultarEnLugarDeApagar)
        {
            if (boton.gameObject.activeSelf != disponible)
                boton.gameObject.SetActive(disponible);
        }
        else
        {
            boton.interactable = disponible;
        }
    }
}
