using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ViroLab.Pasteurizador.Simulator;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// <summary>
    /// Instala y cablea la guía por voz del simulador 2D en la escena activa.
    /// Menú:  Viroo > Simulador > Instalar guía por voz (SCADA)
    /// </summary>
    public static class PasteurizerVoiceGuideInstaller
    {
        const string DIR = "Assets/3D Laboratory Environment with Appratus/Audios/VOff/Simulador_Guia/";

        [MenuItem("Viroo/Simulador/Instalar guía por voz (SCADA)", priority = 300)]
        public static void Install()
        {
            AssetDatabase.Refresh(); // asegura que los .ogg estén importados

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var engine = Object.FindObjectOfType<PasteurizerSimEngine>();
            if (engine == null)
            {
                EditorUtility.DisplayDialog("Guía por voz",
                    "No encontré el simulador (PasteurizerSimEngine) en la escena activa.\n\n" +
                    "Abre la escena que tiene el tablero SCADA y vuelve a ejecutar.", "OK");
                return;
            }

            var guide = engine.GetComponent<PasteurizerVoiceGuide>();
            if (guide == null) guide = Undo.AddComponent<PasteurizerVoiceGuide>(engine.gameObject);

            var src = engine.GetComponent<AudioSource>();
            if (src == null) src = Undo.AddComponent<AudioSource>(engine.gameObject);
            src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;

            guide.engine = engine;
            guide.source = src;
            guide.tablero = engine.transform;

            AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(DIR + n + ".ogg");
            guide.bienvenida    = C("VO_Sim_00_Bienvenida");
            guide.energia       = C("VO_Sim_01_Energia");
            guide.iniciar       = C("VO_Sim_02_Iniciar");
            guide.llenado       = C("VO_Sim_03_Llenado");
            guide.calefactor    = C("VO_Sim_04_Calefactor_Espera");
            guide.tempLista     = C("VO_Sim_05_TempLista");
            guide.refrigerador  = C("VO_Sim_06_Refrigerador");
            guide.bombaProducto = C("VO_Sim_07_BombaProducto");
            guide.acumulando    = C("VO_Sim_08_Acumulando");
            guide.valido        = C("VO_Sim_09_Valido");
            guide.alarma        = C("VO_Sim_10_Alarma");
            guide.tanqueLleno   = C("VO_Sim_11_TanqueLleno");
            guide.finLote       = C("VO_Sim_12_FinLote");
            guide.faltaEnergia  = C("VO_Sim_E1_FaltaEnergia");
            guide.faltaIniciar  = C("VO_Sim_E2_FaltaIniciar");
            guide.faltaTemp     = C("VO_Sim_E3_FaltaTemp");
            guide.faltaNivel    = C("VO_Sim_E4_FaltaNivel");

            var all = new[] {
                guide.bienvenida, guide.energia, guide.iniciar, guide.llenado, guide.calefactor,
                guide.tempLista, guide.refrigerador, guide.bombaProducto, guide.acumulando, guide.valido,
                guide.alarma, guide.tanqueLleno, guide.finLote,
                guide.faltaEnergia, guide.faltaIniciar, guide.faltaTemp, guide.faltaNivel
            };
            int loaded = 0; foreach (var c in all) if (c != null) loaded++;

            EditorUtility.SetDirty(guide);
            EditorSceneManager.MarkSceneDirty(scene);

            string msg = $"Guía por voz instalada en el objeto '{engine.name}'.\n\n" +
                         $"Audios asignados: {loaded}/17.\n";
            if (loaded < 17)
                msg += $"\n(Ojo: faltan {17 - loaded}. Revisa que estén en:\n{DIR})";
            else
                msg += "\nEntra a Play y acércate al tablero: sonará la bienvenida y luego cada audio según operes.";
            EditorUtility.DisplayDialog("Guía por voz", msg, "OK");
            Debug.Log($"<color=cyan>[Guía por voz]</color> Instalada. {loaded}/17 audios en '{engine.name}'.");
        }
    }
}
