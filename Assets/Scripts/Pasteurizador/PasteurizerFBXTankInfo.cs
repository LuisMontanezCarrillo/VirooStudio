using UnityEngine;

namespace ViroLab.Pasteurizador
{
    /// Marca un GameObject (raiz del FBX del tanque) con tag-ISA +
    /// titulo, para que el hover/click muestre el subsistema correcto
    /// sin depender del nombre de los meshes internos del FBX (que son
    /// "tripo_node_*").
    ///
    /// NOTA: el campo se llama `isaTag` (no `tag`) porque `Component.tag`
    /// es un miembro heredado de Unity y produce warning CS0108 si lo
    /// sombreamos.
    [DisallowMultipleComponent]
    public class PasteurizerFBXTankInfo : MonoBehaviour
    {
        [Tooltip("Tag P&ID del componente (ej. T-RAW-01, T-PROD-01)")]
        public string isaTag = "";

        [Tooltip("Titulo legible (ej. SILO LECHE CRUDA)")]
        public string title = "";

        /// Devuelve el SubsystemInfo asociado (T-RAW-01 o T-PROD-01).
        public SubsystemInfo Resolve(PasteurizerSubsystemDatabase db)
        {
            if (db == null) return null;
            if (isaTag == "T-RAW-01")  return db.GetByKey("12_Silo_LecheCruda");
            if (isaTag == "T-PROD-01") return db.GetByKey("12_Tanque_Producto");
            return null;
        }
    }
}
