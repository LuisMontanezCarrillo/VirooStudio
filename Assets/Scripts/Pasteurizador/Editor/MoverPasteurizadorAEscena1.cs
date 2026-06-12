using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ViroLab.Pasteurizador;
using ViroLab.Pasteurizador.EditorTools;

/// <summary>
/// Repara la UI de info de partes (las ventanas emergentes que salen al hacer click
/// en una pieza del pasteurizador) en la escena ACTIVA.
///
/// Por que hace falta: el pasteurizador de la Escena 1 se llama "E" (lo renombraron),
/// y el builder original #4 conecta la UI buscando el objeto por el nombre exacto
/// "Pasteurizador_HTST". Como no lo encuentra, el popup queda sin cablear.
/// El paso #9 (VR) si lo conecta por componente (FindFirstObjectByType), asi que
/// ejecutando #4 + #9 en orden la UI queda funcional sin importar el nombre.
///
/// Uso: abre la Escena 1 (con el pasteurizador presente) y ejecuta:
///   Viroo > Consolidar > Reparar UI de partes (popups) en Escena 1
/// Luego entra a Play y haz click en una pieza: la tarjeta flota frente a la camara.
///
/// Reutiliza el codigo probado de PasteurizerUIBuilder y PasteurizerUIVRSetup.
/// </summary>
public static class RepararUIPartesEscena1
{
    [MenuItem("Viroo/Consolidar/Reparar UI de partes (popups) en Escena 1", priority = 200)]
    public static void Reparar()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Reparar UI de partes", "No hay escena cargada.", "OK");
            return;
        }

        // Aviso temprano si no esta el pasteurizador (su HoverHandler) en la escena activa.
        if (Object.FindFirstObjectByType<PasteurizerHoverHandler>() == null)
        {
            EditorUtility.DisplayDialog("Reparar UI de partes",
                "No encontre el pasteurizador (PasteurizerHoverHandler) en la escena activa.\n\n" +
                "Abre la Escena 1 (con el pasteurizador 'E' presente) y vuelve a ejecutar.",
                "OK");
            return;
        }

        // Paso 4: instancia el Canvas y hace el cableado inicial (card/panel).
        PasteurizerUIBuilder.InstantiateUIInScene();

        // Paso 9: pasa el Canvas a World Space y conecta el popup por componente
        // (robusto al nombre "E"). Solo aparece al hacer click en una pieza.
        PasteurizerUIVRSetup.SetupForVR();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("<color=cyan>[Reparar UI de partes]</color> Listo. Entra a Play y haz click en una pieza del pasteurizador.");
    }
}
