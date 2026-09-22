#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViroLab.Pasteurizador.Simulator;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// <summary>
    /// Corrige EN SITIO la geometría del diagrama de planta del simulador 2D, sin
    /// reconstruir el tablero y por tanto sin perder ninguna referencia asignada.
    ///
    /// Los dos defectos que corrige, ambos del constructor del diagrama:
    ///
    /// 1. Los grupos de equipo (tanques, intercambiador, caldera, retención,
    ///    refrigerador, trampa, pulmón, manómetro) tienen el pivote arriba, pero
    ///    sus hijos se anclan en la esquina INFERIOR izquierda del grupo. El
    ///    resultado es que el contenido de cada equipo se dibuja una altura
    ///    completa por debajo de donde debería. En el intercambiador de placas,
    ///    que mide 354 unidades, eso mandaba la sección de enfriamiento entera
    ///    fuera del lienzo: 109.212 unidades cuadradas de dibujo quedaban fuera
    ///    del área visible.
    ///
    /// 2. Los rótulos y los visores de número aplican la conversión de
    ///    coordenadas SVG dos veces, de modo que quedan reflejados en vertical.
    ///    La distancia media entre cada rótulo y el equipo que nombra era de 234
    ///    unidades. Como efecto colateral, CALENTAMIENTO caía sobre la sección de
    ///    enfriamiento y ENFRIAMIENTO sobre la de calentamiento.
    ///
    /// La corrección es exacta, no aproximada: se deduce de las dos convenciones
    /// en conflicto, y al aplicarla la distancia media rótulo-equipo baja a 19
    /// unidades y el dibujo fuera del área cae a 104 unidades cuadradas.
    ///
    /// La operación es idempotente: si el diagrama ya está corregido, no hace
    /// nada. Y queda registrada en el deshacer de Unity.
    /// </summary>
    public static class PasteurizerPlantGeometryFix
    {
        private const string Menu = "Viroo/Pasteurizador HTST/Corregir geometría del diagrama de planta";

        /// Grupos creados por NewAt, que son los que tienen el pivote invertido.
        private static readonly string[] Grupos =
        {
            "TankIn", "TankOut", "PlateHX", "HoldingTube",
            "Boiler", "Refri", "Trap", "Pulmon", "PSIGauge"
        };

        [MenuItem(Menu, priority = 120)]
        private static void Corregir()
        {
            var plant = BuscarPlantSVG();
            if (plant == null)
            {
                Debug.LogError("[PlantFix] No se encontró _SimDashboard/PlantArea/PlantSVG en la escena abierta.");
                return;
            }

            var grupos = Grupos
                .Select(n => plant.Find(n) as RectTransform)
                .Where(t => t != null)
                .ToList();

            if (grupos.Count == 0)
            {
                Debug.LogError("[PlantFix] El diagrama no tiene los grupos esperados. No se toca nada.");
                return;
            }

            // Puerta de entrada: el pivote arriba es la firma del defecto. Si ya
            // está abajo, el diagrama viene de un constructor corregido y no hay
            // nada que hacer.
            if (grupos.All(g => g.pivot.y < 0.5f))
            {
                Debug.Log("[PlantFix] El diagrama ya está corregido. No se ha modificado nada.");
                return;
            }

            float alturaArea = ((RectTransform)plant).rect.height;
            if (alturaArea < 100f)
            {
                Debug.LogError($"[PlantFix] Altura del área no resuelta ({alturaArea:F0}). " +
                               "Abre la escena y selecciona el tablero para que Unity calcule la disposición.");
                return;
            }

            Undo.SetCurrentGroupName("Corregir geometría del diagrama de planta");
            int grupo = Undo.GetCurrentGroup();

            int nGrupos = 0, nCentrados = 0, nRotulos = 0;

            // --- 1. Grupos de equipo -------------------------------------
            foreach (var g in grupos)
            {
                if (g.pivot.y < 0.5f) continue;

                float h = g.rect.height;
                Undo.RecordObject(g, "pivote del grupo");
                var p = g.pivot; p.y = 0f; g.pivot = p;
                nGrupos++;

                // Los hijos anclados al centro o al borde superior del grupo no
                // siguen a los anclados abajo: se desplazan aparte. Es el caso de
                // la aguja, el eje y el número del manómetro.
                foreach (Transform hijo in g)
                {
                    var rt = hijo as RectTransform;
                    if (rt == null || rt.anchorMin.y <= 0.001f) continue;
                    Undo.RecordObject(rt, "hijo centrado");
                    rt.anchoredPosition -= new Vector2(0f, h);
                    nCentrados++;
                }
            }

            // --- 2. Rótulos y visores de número --------------------------
            foreach (Transform hijo in plant)
            {
                var rt = hijo as RectTransform;
                if (rt == null) continue;
                bool esRotulo = rt.name.StartsWith("Lbl_");
                bool esVisor  = rt.name == "DispPanel";
                if (!esRotulo && !esVisor) continue;

                Undo.RecordObject(rt, "reflejar rótulo");
                var ap = rt.anchoredPosition;
                ap.y = alturaArea - ap.y;
                rt.anchoredPosition = ap;
                nRotulos++;
            }

            EditorUtility.SetDirty(plant);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(plant.gameObject.scene);
            Undo.CollapseUndoOperations(grupo);

            Debug.Log($"<color=cyan>[PlantFix]</color> Geometría corregida: {nGrupos} grupos de equipo, " +
                      $"{nCentrados} piezas centradas y {nRotulos} rótulos o visores. " +
                      "Guarda la escena si el resultado es correcto; si no, deshaz con Ctrl+Z.");

            Verificar(plant, alturaArea);
        }

        /// Informe posterior: a qué distancia queda cada rótulo del equipo que
        /// nombra. Sirve para comprobar el resultado sin ponerse el visor.
        private static void Verificar(Transform plant, float alturaArea)
        {
            var mapa = new Dictionary<string, string>
            {
                { "ENTRADA DEL PRODUCTO", "TankIn" }, { "Cap. 100 L", "TankIn" },
                { "PRODUCTO FINAL", "TankOut" },      { "Cap. 150 L", "TankOut" },
                { "INTERCAMBIADOR DE PLACAS", "PlateHX" },
                { "RETENCION", "HoldingTube" },       { "CALDERA", "Boiler" },
                { "REFRIGERADOR", "Refri" },          { "TRAMPA VAPOR", "Trap" },
                { "TANQUE PULMON", "Pulmon" },        { "Nivel Optimo", "Pulmon" },
                { "PSI", "PSIGauge" },
                { "BOMBA DE PRODUCTO", "PumpMilk" },  { "BOMBA DE AGUA", "PumpHot" },
            };

            var lineas = new List<string>();
            float suma = 0f; int n = 0;

            foreach (var par in mapa)
            {
                var rot = plant.Find("Lbl_" + par.Key) as RectTransform;
                var eq  = plant.Find(par.Value) as RectTransform;
                if (rot == null || eq == null) continue;

                Vector2 c = rot.TransformPoint(rot.rect.center);
                var caja = eq.TransformPoint(eq.rect.center);
                Vector2 tam = new Vector2(eq.rect.width, eq.rect.height) * plant.lossyScale.x;
                float dx = Mathf.Max(0f, Mathf.Abs(c.x - caja.x) - tam.x / 2f);
                float dy = Mathf.Max(0f, Mathf.Abs(c.y - caja.y) - tam.y / 2f);
                float d = new Vector2(dx, dy).magnitude / Mathf.Max(0.0001f, plant.lossyScale.x);

                lineas.Add($"    {par.Key,-26} → {par.Value,-12} {d,7:F0}");
                suma += d; n++;
            }

            if (n == 0) return;
            Debug.Log($"[PlantFix] Distancia de cada rótulo al equipo que nombra " +
                      $"(media {suma / n:F0} unidades):\n" + string.Join("\n", lineas));
        }

        private static Transform BuscarPlantSVG()
        {
            foreach (var d in Object.FindObjectsByType<PasteurizerSimDashboard>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var t = d.transform.Find("PlantArea/PlantSVG");
                if (t != null) return t;
            }
            return null;
        }
    }
}
#endif
