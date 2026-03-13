using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
        // Ocultar botón y limpiar el tablero al inicio
        if (botonContinuar != null) botonContinuar.gameObject.SetActive(false);
        tableroTexto.text = "";
        
        StartCoroutine(ReproducirYSincronizar());
    }

    private IEnumerator ReproducirYSincronizar()
    {
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
        Debug.Log("Cargando la preparación del operario...");
        // Aquí conectaremos la transición de VIROO más adelante
    }
}
