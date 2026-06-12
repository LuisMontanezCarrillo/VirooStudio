using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utilidades para consolidar el Pasteurizador HTST en la Escena 1.
///
/// Menus (Viroo > Consolidar > ...):
///  - "Traer UI de partes (Canvas) a Escena 1": copia el Pasteurizador_UI_Canvas
///    (las ventanas emergentes que salen al seleccionar partes) desde la Escena 2
///    a la Escena 1. NO modifica la Escena 2. No duplica si ya existe.
///  - "Mover Pasteurizador + Pantalla a Escena 1": version original (mueve modelo + pantalla).
///
/// La UI del pasteurizador se auto-conecta en runtime (FindFirstObjectByType /
/// GetComponentInParent), asi que con que el Canvas y el modelo esten en la misma
/// escena, las ventanas emergentes vuelven a funcionar sin cablear nada a mano.
///
/// Todo es reversible: las escenas estan versionadas en git.
/// </summary>
public static class MoverPasteurizadorAEscena1
{
    const string EscenaDestino = "Assets/Scenes/Escena 1.unity";
    const string EscenaOrigen = "Assets/Scenes/Escena 2.unity";
    const string NombreRoot = "Root";

    const string PrefabCanvas = "/Pasteurizador_UI_Canvas.prefab";
    const string PrefabModelo = "/Pasteurizador_HTST.prefab";

    // ---------------------------------------------------------------------
    [MenuItem("Viroo/Consolidar/Traer UI de partes (Canvas) a Escena 1")]
    public static void TraerCanvas()
    {
        TraerCopia(new[] { PrefabCanvas });
    }

    [MenuItem("Viroo/Consolidar/Mover Pasteurizador + Pantalla a Escena 1")]
    public static void MoverModeloYPantalla()
    {
        MoverDesdeOrigen(new[] { PrefabModelo, PrefabCanvas });
    }

    // ---------------------------------------------------------------------
    /// <summary>Copia (no destructivo) las instancias de los prefabs dados desde Escena 2 a Escena 1.</summary>
    static void TraerCopia(string[] prefabsObjetivo)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var destino = EditorSceneManager.OpenScene(EscenaDestino, OpenSceneMode.Single);
        var origen = EditorSceneManager.OpenScene(EscenaOrigen, OpenSceneMode.Additive);

        Transform rootDestino = BuscarRoot(destino);

        // Evitar duplicados: si el prefab ya esta en la Escena 1, no lo traemos.
        var yaPresentes = new HashSet<string>();
        foreach (var go in destino.GetRootGameObjects())
            RecolectarPrefabs(go.transform, yaPresentes);

        var copiados = new List<string>();
        foreach (var go in origen.GetRootGameObjects())
        {
            string ruta = RutaPrefab(go);
            if (ruta == null) continue;
            foreach (var objetivo in prefabsObjetivo)
            {
                if (!ruta.EndsWith(objetivo)) continue;
                if (yaPresentes.Contains(ruta))
                {
                    Debug.Log($"[Consolidar] Ya existe en Escena 1, se omite: {go.name}");
                    break;
                }
                var copia = Object.Instantiate(go);
                copia.name = go.name; // quitar el sufijo "(Clone)"
                SceneManager.MoveGameObjectToScene(copia, destino);
                if (rootDestino != null) copia.transform.SetParent(rootDestino, true);
                copiados.Add(go.name);
                Debug.Log($"[Consolidar] Copiado a Escena 1: '{go.name}'  <-  {ruta}");
                break;
            }
        }

        EditorSceneManager.CloseScene(origen, true); // la Escena 2 NO se modifico

        if (copiados.Count == 0)
        {
            EditorUtility.DisplayDialog("Consolidacion",
                "No se copio nada: o no se encontro el objeto en la Escena 2, o ya estaba en la Escena 1.",
                "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(destino);
        EditorSceneManager.SaveScene(destino);
        Debug.Log($"[Consolidar] LISTO. Copiados a Escena 1: {string.Join(", ", copiados)}");
        EditorUtility.DisplayDialog("Consolidacion",
            $"Se copio a la Escena 1:\n - {string.Join("\n - ", copiados)}\n\nEntra a Play y selecciona una parte del pasteurizador para verificar las ventanas emergentes. Luego avisa para subir al repositorio.",
            "OK");
    }

    /// <summary>Mueve (destructivo en Escena 2) las instancias de los prefabs dados a la Escena 1.</summary>
    static void MoverDesdeOrigen(string[] prefabsObjetivo)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var destino = EditorSceneManager.OpenScene(EscenaDestino, OpenSceneMode.Single);
        var origen = EditorSceneManager.OpenScene(EscenaOrigen, OpenSceneMode.Additive);
        Transform rootDestino = BuscarRoot(destino);

        var encontrados = new List<GameObject>();
        foreach (var go in origen.GetRootGameObjects())
        {
            string ruta = RutaPrefab(go);
            if (ruta == null) continue;
            foreach (var objetivo in prefabsObjetivo)
                if (ruta.EndsWith(objetivo)) { encontrados.Add(go); break; }
        }

        if (encontrados.Count == 0)
        {
            Debug.LogError("[Consolidar] No se encontro ningun objeto objetivo en la Escena 2. No se guardo nada.");
            EditorSceneManager.CloseScene(origen, true);
            return;
        }

        foreach (var go in encontrados)
        {
            SceneManager.MoveGameObjectToScene(go, destino);
            if (rootDestino != null) go.transform.SetParent(rootDestino, true);
            Debug.Log($"[Consolidar] Movido a Escena 1: '{go.name}'");
        }

        EditorSceneManager.MarkSceneDirty(destino);
        EditorSceneManager.MarkSceneDirty(origen);
        EditorSceneManager.SaveScene(destino);
        EditorSceneManager.SaveScene(origen);
        EditorSceneManager.CloseScene(origen, true);
        EditorUtility.DisplayDialog("Consolidacion",
            $"Se movieron {encontrados.Count} objeto(s) a la Escena 1.", "OK");
    }

    // ---------------------------------------------------------------------
    static Transform BuscarRoot(Scene escena)
    {
        foreach (var go in escena.GetRootGameObjects())
            if (go.name == NombreRoot) return go.transform;
        Debug.LogWarning($"[Consolidar] No se encontro '{NombreRoot}' en la Escena 1; los objetos quedaran en la raiz.");
        return null;
    }

    static void RecolectarPrefabs(Transform t, HashSet<string> set)
    {
        string ruta = RutaPrefab(t.gameObject);
        if (ruta != null) set.Add(ruta);
        for (int i = 0; i < t.childCount; i++)
            RecolectarPrefabs(t.GetChild(i), set);
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
