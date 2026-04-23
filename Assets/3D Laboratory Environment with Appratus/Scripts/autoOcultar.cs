using UnityEngine;
using System.Collections;

public class AutoOcultar : MonoBehaviour
{
    // Tiempo en segundos que el pop-up estará visible (ajústalo según lo que dure tu VOff)
    public float tiempoVisible = 18f; 

    // Cada vez que el objeto se encienda (cuando el estudiante le dé clic al robot)
    void OnEnable()
    {
        StartCoroutine(OcultarDespuesDeTiempo());
    }

    IEnumerator OcultarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(tiempoVisible);
        gameObject.SetActive(false); // Se apaga solo
    }
}
