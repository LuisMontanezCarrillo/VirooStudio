using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeOutVR : MonoBehaviour
{
    public float tiempoDeEspera = 4f;
    public float duracionFade = 2f;

    [Tooltip("Arrastra aquí el objeto hacia donde quieres teletransportar al jugador")]
    public Transform puntoDeDestino;

    public void IniciarFadeOut()
    {
        StartCoroutine(RutinaFadeYTeletransporte());
    }

    IEnumerator RutinaFadeYTeletransporte()
    {
        // 1. Espera inicial
        yield return new WaitForSeconds(tiempoDeEspera);

        // 2. Crear panel negro
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

        // 3. OSCURECER (Fade Out)
        float tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, tiempo / duracionFade));
            yield return null;
        }

        // 4. ¡EL TELETRANSPORTE! (Movemos el root/cuerpo entero del jugador)
        if (puntoDeDestino != null)
        {
            camaraVR.root.position = puntoDeDestino.position;
            camaraVR.root.rotation = puntoDeDestino.rotation;
        }

        yield return new WaitForSeconds(0.5f); // Un breve respiro en la oscuridad

        // 5. ACLARAR (Fade In)
        tiempo = 0;
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            imagenNegra.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, tiempo / duracionFade));
            yield return null;
        }

        // 6. Limpieza
        Destroy(canvasObj);
    }
}