using UnityEngine;
using System.Collections;

public class ControladorPuerta : MonoBehaviour
{
    [Tooltip("¿Cuánto tiempo tardará en abrirse completamente?")]
    public float duracionApertura = 4f;

    [Tooltip("¿Cuántos grados debe girar? (Usa 90 o -90)")]
    public float anguloApertura = 90f;

    // Aquí guardaremos la posición original (cerrada)
    private Quaternion rotacionCerrada; 

    void Start()
    {
        // Memorizamos la rotación exacta que tiene la puerta al iniciar el juego
        rotacionCerrada = transform.localRotation;
    }

    public void AbrirPuerta()
    {
        StartCoroutine(AnimacionAbrir());
    }

    // Esta es la nueva función que llamaremos en la oscuridad
    public void CerrarInstante()
    {
        transform.localRotation = rotacionCerrada;
    }

    IEnumerator AnimacionAbrir()
    {
        Quaternion rotacionInicial = transform.localRotation;
        Quaternion rotacionFinal = transform.localRotation * Quaternion.Euler(0, anguloApertura, 0);
        
        float tiempo = 0;

        while (tiempo < duracionApertura)
        {
            tiempo += Time.deltaTime;
            
            float progreso = tiempo / duracionApertura;
            float progresoSuave = Mathf.SmoothStep(0f, 1f, progreso);
            
            transform.localRotation = Quaternion.Lerp(rotacionInicial, rotacionFinal, progresoSuave);
            
            yield return null; 
        }

        transform.localRotation = rotacionFinal;
    }
}