using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ViroLab.Pasteurizador
{
    [Serializable]
    public class SubsystemInfo
    {
        public string key;
        public string tag;
        public string title;
        public string colorHex;
        public bool showLabel;
        public string description;

        public Color GetColor()
        {
            return TryParseHex(colorHex, out var c) ? c : Color.gray;
        }

        public static bool TryParseHex(string hex, out Color c)
        {
            c = Color.white;
            if (string.IsNullOrEmpty(hex)) return false;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6) return false;
            return ColorUtility.TryParseHtmlString("#" + hex, out c);
        }
    }

    [Serializable]
    public class SpecificDescription
    {
        public string namePrefix;
        public string description;
    }

    [Serializable]
    public class SubsystemsJsonRoot
    {
        public List<SubsystemInfo> subsystems;
        public List<SpecificDescription> specific;
    }

    public class PasteurizerSubsystemDatabase : ScriptableObject
    {
        [SerializeField] private List<SubsystemInfo> subsystems = new();
        [SerializeField] private List<SpecificDescription> specifics = new();

        private Dictionary<string, SubsystemInfo> _byKey;

        public IReadOnlyList<SubsystemInfo> Subsystems => subsystems;
        public IReadOnlyList<SpecificDescription> Specifics => specifics;

        public void Build()
        {
            _byKey = new Dictionary<string, SubsystemInfo>();
            foreach (var s in subsystems)
                if (!string.IsNullOrEmpty(s.key)) _byKey[s.key] = s;
        }

        public SubsystemInfo GetByKey(string key)
        {
            if (_byKey == null) Build();
            return key != null && _byKey.TryGetValue(key, out var v) ? v : null;
        }

        public string GetSpecificDescription(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return null;
            foreach (var sp in specifics)
            {
                if (!string.IsNullOrEmpty(sp.namePrefix) && partName.StartsWith(sp.namePrefix))
                    return sp.description;
            }
            return null;
        }

        public string ResolveSubsystemKey(string partName)
        {
            // Equipos externos auxiliares (v8+: caldera + tanques aux)
            // Chequear PRIMERO antes que las reglas mas generales.
            if (Regex.IsMatch(partName, "^T_RAW_01_|^T_RAW_FBX")) return "12_Silo_LecheCruda";
            if (Regex.IsMatch(partName, "^T_PROD_01_|^T_PROD_FBX")) return "12_Tanque_Producto";
            if (Regex.IsMatch(partName, "^T_HOT_01_")) return "12_Calderin_AguaCal";
            if (Regex.IsMatch(partName, "^T_COOL_01_")) return "12_Chiller_AguaFria";
            if (Regex.IsMatch(partName, "^T_WATER_01_")) return "12_TanqueAguaMakeup";
            if (Regex.IsMatch(partName, "^TRAP_01_")) return "12_TrampaVapor";
            if (Regex.IsMatch(partName, "^CB_01_")) return "12_Caldera_Vapor";
            if (Regex.IsMatch(partName, "^P_03_")) return "12_BombaAguaCal_P03";
            if (Regex.IsMatch(partName, "^P_04_")) return "12_BombaAguaFria_P04";

            // Same priority as topGroupOf() in visor.html
            if (Regex.IsMatch(partName, "^Skid_")) return "01_Skid_Bastidor";
            if (Regex.IsMatch(partName, "^DripPan_")) return "01_Skid_Bastidor";
            if (Regex.IsMatch(partName, "^(Gabinete|HMI|Medidor|Boton|Selector|Piloto|Cab_)")) return "02_Tablero_Control";
            if (Regex.IsMatch(partName, "^Tanque_")) return "03_Tanque_Balance";
            if (Regex.IsMatch(partName, "Bomba_Alimentacion")) return "04_Bomba_Alimentacion";
            if (Regex.IsMatch(partName, "Bomba_Booster")) return "05_Bomba_Booster";
            if (Regex.IsMatch(partName, "^PHE_")) return "06_Intercambiador_Placas";
            if (Regex.IsMatch(partName, "Valvula_FDV")) return "07_FDV_Diversion";
            if (Regex.IsMatch(partName, "^VlvSup_")) return "07_Valvulas_Top_PHE";
            if (Regex.IsMatch(partName, "Valvula_")) return "07_Valvulas";
            if (Regex.IsMatch(partName, "^Holding_")) return "08_Tubo_Retencion";
            if (Regex.IsMatch(partName, "Manometro")) return "10_Instrumentos";
            if (Regex.IsMatch(partName, "Cable_|Caja_Conexiones|Junction|Conduit_")) return "11_Cables_y_Conexiones";
            if (Regex.IsMatch(partName, "Manifold|Solenoide|TuboAire")) return "11_Manifold_Aire";
            if (Regex.IsMatch(partName, "YStrainer")) return "11_Filtro_Y_Strainer";
            if (Regex.IsMatch(partName, "Caudalimetro|Stub_(L|R|Top|Bot)")) return "11_Caudalimetro_FT";
            if (Regex.IsMatch(partName, "CheckValve")) return "11_Check_Valves";
            if (Regex.IsMatch(partName, "PSV_")) return "11_Relief_Valve_PSV";
            if (Regex.IsMatch(partName, "PRV_")) return "11_Reg_Presion_Vapor";
            if (Regex.IsMatch(partName, "Acom_")) return "11_Acometidas_Externas";
            if (Regex.IsMatch(partName, "Salida_Producto|Drenaje|Puerto_Muestra")) return "11_Salidas_y_Drenajes";
            if (Regex.IsMatch(partName, "Placa_Tag")) return "11_Placas_ID";
            if (Regex.IsMatch(partName, "Borde")) return "01_Skid_Bastidor";
            if (Regex.IsMatch(partName, "^(0[1-9]|10|11|12|13|14|15|16)_")) return "09_Tuberias_Proceso";
            if (Regex.IsMatch(partName, "(Suministro|Vapor|Aire|Servicio|Drenaje|Linea|Tuberia|Tanque_|Retorno|Acom|FT|CIP)", RegexOptions.IgnoreCase)) return "09_Tuberias_Proceso";
            return "99_Otros";
        }

        public void PopulateFrom(SubsystemsJsonRoot json)
        {
            subsystems.Clear();
            if (json?.subsystems != null) subsystems.AddRange(json.subsystems);
            specifics.Clear();
            if (json?.specific != null) specifics.AddRange(json.specific);
            Build();
        }

        public static PasteurizerSubsystemDatabase LoadFromResources(string path = "Pasteurizador/subsystems")
        {
            var json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError($"PasteurizerSubsystemDatabase: no encontrado JSON en Resources/{path}");
                return null;
            }
            var root = JsonUtility.FromJson<SubsystemsJsonRoot>(json.text);
            var db = CreateInstance<PasteurizerSubsystemDatabase>();
            db.PopulateFrom(root);
            return db;
        }
    }
}
