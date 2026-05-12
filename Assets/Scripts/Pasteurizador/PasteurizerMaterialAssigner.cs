using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ViroLab.Pasteurizador
{
    // Asigna materiales URP a cada Renderer hijo segun el nombre del GameObject.
    // Reglas equivalentes a PALETTE de visor.html.
    [DisallowMultipleComponent]
    public class PasteurizerMaterialAssigner : MonoBehaviour
    {
        public enum PaletteRule
        {
            Actuador, Solenoide, Vapor, AguaFria, Negro, HMI,
            Verde, Rojo, Amarillo, Cara, Gabinete, SkidMetal, PHEMetal,
            Backing, InoxDefault
        }

        [System.Serializable]
        public class PaletteEntry
        {
            public PaletteRule rule;
            public Material material;
        }

        [Tooltip("Materiales URP por regla. Se setean desde el Editor builder.")]
        public List<PaletteEntry> palette = new();

        [Tooltip("Aplica materiales al activar el componente. Desactiva si lo manejas externamente.")]
        public bool applyOnAwake = false;

        private Dictionary<PaletteRule, Material> _byRule;

        private void Awake()
        {
            if (applyOnAwake) Apply();
        }

        public void Apply()
        {
            BuildMap();
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                var rule = ClassifyName(r.gameObject.name);
                if (_byRule.TryGetValue(rule, out var mat) && mat != null)
                    r.sharedMaterial = mat;
            }
        }

        private void BuildMap()
        {
            _byRule = new Dictionary<PaletteRule, Material>();
            foreach (var e in palette)
                _byRule[e.rule] = e.material;
        }

        public static PaletteRule ClassifyName(string name)
        {
            // Order matters - first match wins, mirrors visor.html PALETTE
            if (Regex.IsMatch(name, "Actuador|Domo", RegexOptions.IgnoreCase)) return PaletteRule.Actuador;
            if (Regex.IsMatch(name, "Solenoide|TuboAire|Manifold_Solenoide|Aire_Comprimido", RegexOptions.IgnoreCase)) return PaletteRule.Solenoide;
            if (Regex.IsMatch(name, "Acom_Vapor|Vapor_|Acom_AguaCal")) return PaletteRule.Vapor;
            if (Regex.IsMatch(name, "Acom_AguaFria|AguaFria|Glicol")) return PaletteRule.AguaFria;
            if (Regex.IsMatch(name, "Cable_|Conduit_|PrensaEstopa|Negro|Tapa_Ventilador|Caja_Bornes")) return PaletteRule.Negro;
            if (Regex.IsMatch(name, "HMI")) return PaletteRule.HMI;
            if (Regex.IsMatch(name, "Boton_Start|Piloto_Marcha|Indicador$")) return PaletteRule.Verde;
            if (Regex.IsMatch(name, "Boton_Stop|Boton_Emergencia|Aguja")) return PaletteRule.Rojo;
            if (Regex.IsMatch(name, "Boton_Reset|Piloto_Alarma")) return PaletteRule.Amarillo;
            if (Regex.IsMatch(name, "Cara")) return PaletteRule.Cara;
            if (Regex.IsMatch(name, "Gabinete|Panel|Caja_Acom_Elec")) return PaletteRule.Gabinete;
            if (Regex.IsMatch(name, "Skid|Columna|Viga|Base")) return PaletteRule.SkidMetal;
            if (Regex.IsMatch(name, "Placa_(Fija|Movil)|Separador|Tornillo|Barra")) return PaletteRule.PHEMetal;
            if (Regex.IsMatch(name, "Backing")) return PaletteRule.Backing;
            return PaletteRule.InoxDefault;
        }

        // Construye 15 materiales URP con los mismos colores/metalness/roughness del PALETTE.
        // Llamado desde el Editor builder.
        public static List<PaletteEntry> BuildDefaultPaletteURP(Shader urpLitShader)
        {
            (PaletteRule rule, uint color, bool metal)[] data =
            {
                (PaletteRule.Actuador,     0x143dbf, false),
                (PaletteRule.Solenoide,    0x1f4fd9, false),
                (PaletteRule.Vapor,        0x1f4fd9, false),
                (PaletteRule.AguaFria,     0x1f4fd9, false),
                (PaletteRule.Negro,        0x161616, false),
                (PaletteRule.HMI,          0x267fbf, false),
                (PaletteRule.Verde,        0x0fa524, false),
                (PaletteRule.Rojo,         0xc40d0d, false),
                (PaletteRule.Amarillo,     0xc9a206, false),
                (PaletteRule.Cara,         0xeef1f7, false),
                (PaletteRule.Gabinete,     0xc8ccd0, false),
                (PaletteRule.SkidMetal,    0x9fa3a8, true ),
                (PaletteRule.PHEMetal,     0xa5a9ae, true ),
                (PaletteRule.Backing,      0x0c0c0c, false),
                (PaletteRule.InoxDefault,  0xd0d3d8, true ),
            };
            var list = new List<PaletteEntry>(data.Length);
            foreach (var d in data)
            {
                var mat = new Material(urpLitShader) { name = "Mat_" + d.rule };
                Color c = new Color(
                    ((d.color >> 16) & 0xFF) / 255f,
                    ((d.color >> 8)  & 0xFF) / 255f,
                    ((d.color)       & 0xFF) / 255f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);
                if (mat.HasProperty("_Metallic"))  mat.SetFloat("_Metallic", d.metal ? 0.7f : 0.15f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", d.metal ? 0.65f : 0.45f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", d.metal ? 0.65f : 0.45f);
                list.Add(new PaletteEntry { rule = d.rule, material = mat });
            }
            return list;
        }
    }
}
