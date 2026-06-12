using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mueve el Pasteurizador HTST completo y la pantalla de simulacion (Pasteurizador_UI_Canvas)
/// desde la Escena 2 hacia la Escena 1, dejandolos bajo el "Root" de la Escena 1.
/// Conserva la transformada en el mundo, el vinculo al prefab y la configuracion de cada instancia.
///
/// Uso: menu  Viroo > Consolidar > Mover Pasteurizador + Pantalla a Escena 1
///
/// Es reversible: ambas escenas estan versionadas en git. Si algo sale mal,
/// descarta los cambios de las escenas y vuelve a intentar.
/// </summary>
public static class MoverPasteurizadorAEscena1
{
    const string EscenaDestino = "Assets/Scenes/Escena 1.unity";
    const string EscenaOrigen = "Assets/Scenes/Escena 2.unity";
    const string NombreRoot = "Root";

    // Se identifica cada objeto por el prefab del que proviene (robusto frente a renombres como "E").
    static readonly string[] PrefabsObjetivo =
    {
        "/Pasteurizador_HTST.prefab",        // el pasteurizador completo (modelo 654 partes)
        "/Pasteurizador_UI_Canvas.prefab",   // la pantalla de simulacion (SCADA)
    };

    [MenuItem("Viroo/Consolidar/Mover Pasteurizador + Pantalla a Escena 1")]
    public static void Mover()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var destino = EditorSceneManager.OpenScene(EscenaDestino, OpenSceneMode.Single);
        var origen = EditorSceneManager.OpenScene(EscenaOrigen, OpenSceneMode.Additive);

        // Buscar el "Root" de la Escena 1 para colgar ahi los objetos.
        Transform rootDestino = null;
        foreach (var go in destino.GetRootGameObjects())
        {
            if (go.name == NombreRoot) { rootDestino = go.transform; break; }
        }
        if (rootDestino == null)
            Debug.LogWarning($"[Mover] No se encontro un objeto '{NombreRoot}' en la Escena 1. Los objetos quedaran en la raiz.");

        // Localizar los objetos objetivo entre las raices de la Escena 2.
        var encontrados = new List<GameObject>();
        foreach (var go in origen.GetRootGameObjects())
        {
            string ruta = RutaPrefab(go);
            if (ruta == null) continue;
            foreach (var objetivo in PrefabsObjetivo)
            {
                if (ruta.EndsWith(objetivo))
                {
                    encontrados.Add(go);
                    Debug.Log($"[Mover] Encontrado en Escena 2: '{go.name}'  <-  {ruta}");
                    break;
                }
            }
        }

        if (encontrados.Count == 0)
        {
            Debug.LogError("[Mover] No se encontro ningun objeto objetivo en la Escena 2. No se guardo nada.");
            EditorSceneManager.CloseScene(origen, true);
            return;
        }

        foreach (var go in encontrados)
        {
            SceneManager.MoveGameObjectToScene(go, destino);
            if (rootDestino != null)
                go.transform.SetParent(rootDestino, true); // true = conserva la posicion en el mundo
            Debug.Log($"[Mover] Movido a Escena 1: '{go.name}'");
        }

        EditorSceneManager.MarkSceneDirty(destino);
        EditorSceneManager.MarkSceneDirty(origen);
        EditorSceneManager.SaveScene(destino);
        EditorSceneManager.SaveScene(origen);
        EditorSceneManager.CloseScene(origen, true);

        Debug.Log($"[Mover] LISTO. Se movieron {encontrados.Count} objeto(s) a la Escena 1. Revisa la escena y guarda el proyecto.");
        EditorUtility.DisplayDialog("Consolidacion",
            $"Se movieron {encontrados.Count} objeto(s) a la Escena 1.\n\nRevisa visualmente la Escena 1 y avisa para subir al repositorio.",
            "OK");
    }

    /// <summary>Ruta del asset de prefab del que proviene la instancia (o null si no es instancia de prefab).</summary>
    static string RutaPrefab(GameObject go)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go))
            return null;
        var origen = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
        if (origen == null)
            origen = PrefabUtility.GetCorrespondingObjectFromSource(go);
        return origen != null ? AssetDatabase.GetAssetPath(origen) : null;
    }
}
