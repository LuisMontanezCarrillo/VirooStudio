using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Locución sincronizada con un tablero de texto, párrafo a párrafo.
///
/// ESTADO: SIN CABLEAR. En la escena hay una instancia, en Root/Sistema_Voz,
/// con sus cuatro referencias vacías, y nada en el proyecto la llama. Su
/// AvanzarAEscena2 sigue diciendo «aquí conectaremos la transición de VIROO más
/// adelante» y no hace nada. Parece un prototipo que quedó a medias.
///
/// Mientras estuvo así, Start() reventaba con NullReferenceException en cada
/// arranque de la escena, porque escribía en tableroTexto sin comprobarlo. No
/// rompía nada, pero llenaba la consola de excepciones justo donde hay que leer
/// los avisos de todo lo demás. Ahora comprueba sus referencias, avisa una vez
/// de lo que le falta y se aparta.
/// </summary>
public class VOManager : MonoBehaviour
{
    [System.Serializable]
    public struct Parrafo
    {
        [Tooltip("Segundo exacto del audio en el que aparece este texto")]
        public float tiempoDeAparicion;

        [TextArea(3, 6)]
        public string texto;
    }

    [Header("Conexiones de la Pantalla")]
    [Tooltip("Arrastra aquí tu TextMeshPro del tablero verde")]
    [SerializeField] private TextMeshProUGUI tableroTexto;
    [Tooltip("Arrastra aquí el botón de Continuar")]
    [SerializeField] private Button botonContinuar;

    [Header("Conexiones de Audio")]
    [SerializeField] private AudioSource fuenteDeAudio;
    [SerializeField] private AudioClip vozEnOff;

    [Header("El Guion (Cerebro)")]
    [SerializeField] private List<Parrafo> listaDeParrafos;
    [SerializeField] private float velocidadFade = 1.5f;

    private void Start()
    {
        if (botonContinuar != null) botonContinuar.gameObject.SetActive(false);

        // Sin tablero o sin audio no hay nada que sincronizar. Se avisa una vez y
        // se apaga el componente, en vez de lanzar una excepcion por arranque.
        if (tableroTexto == null || fuenteDeAudio == null || vozEnOff == null)
        {
            Debug.LogWarning("[VOManager] Sin cablear: falta " +
                             (tableroTexto == null ? "el tablero de texto; " : "") +
                             (fuenteDeAudio == null ? "la fuente de audio; " : "") +
                             (vozEnOff == null ? "el clip de voz; " : "") +
                             "el componente se desactiva. Si ya no hace falta, borra el objeto.",
                             this);
            enabled = false;
            return;
        }

        tableroTexto.text = "";

        StartCoroutine(ReproducirYSincronizar());
    }

    private IEnumerator ReproducirYSincronizar()
    {
        NarradorUnico.TomarLaPalabra(fuenteDeAudio);
        fuenteDeAudio.clip = vozEnOff;
        fuenteDeAudio.Play();

        int indiceActual = 0;

        while (fuenteDeAudio.isPlaying)
        {
            float tiempoActual = fuenteDeAudio.time;

            // Si es hora de mostrar el siguiente párrafo
            if (indiceActual < listaDeParrafos.Count && tiempoActual >= listaDeParrafos[indiceActual].tiempoDeAparicion)
            {
                StartCoroutine(CambiarTextoSuavemente(listaDeParrafos[indiceActual].texto));
                indiceActual++;
            }
            yield return null;
        }

        // Cuando el audio termina
        FinalizarExplicacion();
    }

    private IEnumerator CambiarTextoSuavemente(string nuevoTexto)
    {
        // 1. Desvanecer el texto actual (Fade Out)
        Color colorTexto = tableroTexto.color;
        while (colorTexto.a > 0)
        {
            colorTexto.a -= Time.deltaTime * velocidadFade;
            tableroTexto.color = colorTexto;
            yield return null;
        }

        // 2. Cambiar el texto mientras está invisible
        tableroTexto.text = nuevoTexto;

        // 3. Aparecer el nuevo texto (Fade In)
        while (colorTexto.a < 1)
        {
            colorTexto.a += Time.deltaTime * velocidadFade;
            tableroTexto.color = colorTexto;
            yield return null;
        }
    }

    private void FinalizarExplicacion()
    {
        StartCoroutine(CambiarTextoSuavemente("Instrucciones finalizadas. Apunta y selecciona 'Continuar'."));

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(true);
            botonContinuar.onClick.AddListener(AvanzarAEscena2);
        }
    }

    private void AvanzarAEscena2()
    {
        Debug.Log("[VOManager] Continuar pulsado. Este componente no tiene transición conectada.");
    }
}
