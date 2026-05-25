using UnityEngine;
using UnityEngine.UI;

public class ControladorCarrusel : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto Image 'Pantalla_Principal'")]
    public Image pantalla;

    [Tooltip("Arrastra aquí el componente AudioSource que pusimos en el Canvas")]
    public AudioSource reproductorAudio;

    [Tooltip("Coloca aquí las imágenes en orden")]
    public Sprite[] diapositivas;

    [Tooltip("Coloca aquí los audios en el MISMO ORDEN que las imágenes")]
    public AudioClip[] audiosVoz;

    private int indiceActual = 0;

    void Start()
    {
        // Al iniciar, SOLO mostramos la primera imagen en silencio
        if (diapositivas.Length > 0)
        {
            pantalla.sprite = diapositivas[0];
        }
    }

    public void Siguiente()
    {
        // Si no hemos llegado al final, avanzamos
        if (indiceActual < diapositivas.Length - 1)
        {
            indiceActual++;
            MostrarDiapositiva(indiceActual);
        }
    }

    public void Anterior()
    {
        // Si no estamos en la primera, retrocedemos
        if (indiceActual > 0)
        {
            indiceActual--;
            MostrarDiapositiva(indiceActual);
        }
    }

    private void MostrarDiapositiva(int indice)
    {
        // 1. Cambiar la imagen
        pantalla.sprite = diapositivas[indice];

        // 2. Manejar el audio
        // Verificamos que haya audios configurados y que correspondan al índice actual
        if (audiosVoz.Length > indice && audiosVoz[indice] != null)
        {
            reproductorAudio.Stop(); // Detenemos el audio anterior si seguía sonando
            reproductorAudio.clip = audiosVoz[indice]; // Cargamos el nuevo audio
            reproductorAudio.Play(); // Le damos play
        }
    }
}