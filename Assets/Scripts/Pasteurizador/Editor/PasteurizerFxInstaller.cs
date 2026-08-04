using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ViroLab.Pasteurizador;
using ViroLab.Pasteurizador.Simulator;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// <summary>
    /// Instala y cablea los FX de sonido del pasteurizador en la escena activa.
    /// Menú:  Viroo > Simulador > Instalar FX de sonido
    /// </summary>
    public static class PasteurizerFxInstaller
    {
        const string DIR = "Assets/3D Laboratory Environment with Appratus/Audios/FX pasteurizador (U. Salle)/";

        [MenuItem("Viroo/Simulador/Instalar FX de sonido", priority = 301)]
        public static void Install()
        {
            AssetDatabase.Refresh();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            var engine = Object.FindFirstObjectByType<PasteurizerSimEngine>();
            if (engine == null)
            {
                EditorUtility.DisplayDialog("FX de sonido",
                    "No encontré el simulador (PasteurizerSimEngine) en la escena activa.\nAbre la escena del tablero y vuelve a ejecutar.", "OK");
                return;
            }

            var fx = engine.GetComponent<PasteurizerFxController>();
            if (fx == null) fx = Undo.AddComponent<PasteurizerFxController>(engine.gameObject);

            fx.engine = engine;
            fx.hover = Object.FindFirstObjectByType<PasteurizerHoverHandler>();

            AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(DIR + n + ".ogg");

            // --- proceso: one-shots ---
            fx.inicioClip  = C("FX_Inicio Pasteurizador");
            fx.finalClip   = C("FX_Pasteurizador final");
            fx.pumpStart   = C("FX_Bomba Pasteurizador Inicio");
            fx.pumpStop    = C("FX_Bomba Pasteurizador Apagado");
            fx.waterStart  = C("FX_Bomba de Agua Inicio");
            fx.waterStop   = C("FX_Bomba de Agua Apagando");
            fx.boilerStart = C("FX_Caldera Inicio");
            fx.boilerStop  = C("FX_Caldera Apagando");
            // --- proceso: loops ---
            fx.procLoopClip   = C("FX_Pasteurizador Proceso (LOOP)");
            fx.pumpLoopClip   = C("FX_Bomba Pasteurizador En Proceso");
            fx.waterLoopClip  = C("FX_Bomba de Agua Andando");
            fx.boilerLoopClip = C("FX_Caldera Proceso");
            fx.vaporLoopClip  = C("FX_Vapor Caldero");

            // --- al tocar piezas (subsistema -> FX) ---
            fx.touchMap = new[]
            {
                new PasteurizerFxController.TouchFx { subsystemKey = "02_Tablero_Control", clip = C("FX_Tablero Pasteurizador") },
                new PasteurizerFxController.TouchFx { subsystemKey = "12_Caldera_Vapor",   clip = C("FX_Cajon Caldero") },
                new PasteurizerFxController.TouchFx { subsystemKey = "07_Valvulas",        clip = C("FX_Llave Pasteurizador") },
                new PasteurizerFxController.TouchFx { subsystemKey = "07_FDV_Diversion",   clip = C("FX_Llave Pasteurizador") },
            };

            var all = new[] {
                fx.inicioClip, fx.finalClip, fx.pumpStart, fx.pumpStop, fx.waterStart, fx.waterStop,
                fx.boilerStart, fx.boilerStop, fx.procLoopClip, fx.pumpLoopClip, fx.waterLoopClip,
                fx.boilerLoopClip, fx.vaporLoopClip
            };
            int loaded = 0; foreach (var c in all) if (c != null) loaded++;
            int touch = 0; foreach (var m in fx.touchMap) if (m.clip != null) touch++;

            EditorUtility.SetDirty(fx);
            EditorSceneManager.MarkSceneDirty(scene);

            EditorUtility.DisplayDialog("FX de sonido",
                $"FX instalados en '{engine.name}'.\n\n" +
                $"• FX de proceso asignados: {loaded}/13\n" +
                $"• FX al tocar piezas: {touch} mapeos\n\n" +
                (loaded < 13 ? $"(Faltan {13 - loaded}. Revisa la carpeta:\n{DIR})\n\n" : "") +
                "Entra a Play: los FX sonarán según operes el tablero, y al tocar el Tablero, la Caldera o las Válvulas en el 3D.",
                "OK");
            Debug.Log($"<color=cyan>[FX pasteurizador]</color> {loaded}/13 FX de proceso + {touch} FX al tocar, en '{engine.name}'.");
        }
    }
}
