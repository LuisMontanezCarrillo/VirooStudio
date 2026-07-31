using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Añadido para eventos modulares
using System.Collections;

public class FadeOutVR : MonoBehaviour
{
    [Header("Configuración de Teletransporte")]
    public float tiempoDeEspera = 4f;
    public float duracionFade = 2f;

    [Tooltip("Arrastra aquí el objeto hacia donde quieres teletransportar al jugador")]
    public Transform puntoDeDestino;

    [Header("Eventos de Red (VIROO)")]
    [Tooltip("Coloca aquí los Canvas, Puertas y Audios usando los componentes de red de VIROO")]
    public UnityEvent OnTeletransporteEjecutado; // Evento que lanzaremos a la red

    public void IniciarFadeOut()
    {
        StartCoroutine(RutinaFadeYTeletransporte());
    }

    IEnumerator RutinaFadeYTeletransporte()
    {
        // 1. Espera inicial
        yield return new WaitForSeconds(tiempoDeEspera);

        // 2. Crear panel negro dinámico (Lógica Local - Solo afecta a quien viaja)
        GameObject canvasObj = new GameObject("Canvas_FadeMagico");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 999; 

        Transform camaraVR = Camera.main.transform;
        canvasObj.transform.SetParent(camaraVR);
        canvasObj.transform.localPosition = new Vector3(0, 0, 0.5f); 
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        GameObject imgObj = new GameObject("Cuadro_Negro");
        imgObj.transform.SetParent(canvasObj.transform, false);
        Image imagenNegra = imgObj.AddComponent<Image>();
        imagenNegra.color = new Color(0, 0, 0, 0); 
        imagenNegra.rectTransform.sizeDelta = new Vector2(5000, 5000); 
        imagenNegra.raycastTarget = false; 

        // 3. OSCURECER (Fade Out Local)
        float tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, tiempo / duracionFade));
            yield return null;
        }

        // 4. TELETRANSPORTE (Local)
        if (puntoDeDestino != null)
        {
            Transform rootJugador = camaraVR.root;

            float diferenciaRotacion = puntoDeDestino.eulerAngles.y - camaraVR.eulerAngles.y;
            rootJugador.Rotate(0, diferenciaRotacion, 0);

            Vector3 diferenciaPosicion = puntoDeDestino.position - camaraVR.position;
            diferenciaPosicion.y = 0; 

            rootJugador.position += diferenciaPosicion;
        }

        // 5. INVOCAR CAMBIOS GLOBALES
        // Aquí delegamos los Canvas, la Puerta y el Audio al sistema de red de VIROO
        OnTeletransporteEjecutado?.Invoke();

        yield return new WaitForSeconds(0.5f);

        // 6. ACLARAR (Fade In Local)
        tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, tiempo / duracionFade));
            yield return null;
        }

        Destroy(canvasObj);
    }
}