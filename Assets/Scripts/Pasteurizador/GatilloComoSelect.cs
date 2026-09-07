using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ViroLab.Pasteurizador
{
    /// <summary>
    /// Unifica el boton de interaccion de los mandos: todas las interacciones se
    /// disparan con el gatillo (TriggerButton).
    ///
    /// En el mapeo XRI que usa el rig de VIROO, "Select" esta enlazado al grip
    /// ({GripButton}) y "Activate" y "UI Press" al gatillo ({TriggerButton}). Los
    /// XRSimpleInteractable de las escenas (lavamanos, maniqui, casillero, logos,
    /// pausa) escuchan Select, de modo que respondian al grip, mientras que la
    /// interfaz del tablero y la ficha del pasteurizador respondian al gatillo.
    ///
    /// Este componente NO modifica el paquete de VIROO ni los interactables: en
    /// tiempo de ejecucion localiza los interactores de mano del rig y aplica un
    /// binding override sobre sus acciones Select / Select Value, sustituyendo
    /// {GripButton} por {TriggerButton} y {Grip} por {Trigger}. Solo toca los
    /// bindings de &lt;XRController&gt;; los de seguimiento de manos (MetaAimHand) y
    /// los interactores de escritorio (raton) quedan como estan. El override vive
    /// solo en memoria: al salir de Play todo vuelve a su estado original.
    /// </summary>
    [AddComponentMenu("ViroLab/Pasteurizador/Gatillo como Select")]
    [DefaultExecutionOrder(-50)]
    public class GatilloComoSelect : MonoBehaviour
    {
        [Tooltip("Segundos entre rastreos del rig. El rig de VIROO puede aparecer despues de cargar la escena, por eso se reintenta.")]
        public float retrySeconds = 1f;

        [Tooltip("Escribir en la consola las acciones reasignadas.")]
        public bool log = true;

        private readonly HashSet<InputAction> _hechas = new HashSet<InputAction>();
        private float _proximo;

        private void Update()
        {
            if (Time.unscaledTime < _proximo) return;
            _proximo = Time.unscaledTime + Mathf.Max(0.1f, retrySeconds);

            var interactores = FindObjectsByType<XRBaseInputInteractor>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (interactores == null) return;

            foreach (var it in interactores)
            {
                if (it == null || EsDeEscritorio(it.transform)) continue;
                Reasignar(it.selectInput, it.name);
            }
        }

        private void Reasignar(XRInputButtonReader lector, string origen)
        {
            if (lector == null) return;
            switch (lector.inputSourceMode)
            {
                case XRInputButtonReader.InputSourceMode.InputActionReference:
                    Reasignar(lector.inputActionReferencePerformed != null ? lector.inputActionReferencePerformed.action : null, origen);
                    Reasignar(lector.inputActionReferenceValue != null ? lector.inputActionReferenceValue.action : null, origen);
                    break;
                case XRInputButtonReader.InputSourceMode.InputAction:
                    Reasignar(lector.inputActionPerformed, origen);
                    Reasignar(lector.inputActionValue, origen);
                    break;
            }
        }

        private void Reasignar(InputAction accion, string origen)
        {
            if (accion == null || _hechas.Contains(accion)) return;
            _hechas.Add(accion);

            int cambiados = 0;
            var bindings = accion.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || b.isPartOfComposite || string.IsNullOrEmpty(b.path)) continue;
                if (b.path.IndexOf("<XRController>", StringComparison.OrdinalIgnoreCase) < 0) continue;

                string nuevo = null;
                if (b.path.Contains("{GripButton}"))      nuevo = b.path.Replace("{GripButton}", "{TriggerButton}");
                else if (b.path.Contains("{Grip}"))       nuevo = b.path.Replace("{Grip}", "{Trigger}");
                if (nuevo == null) continue;

                accion.ApplyBindingOverride(i, nuevo);
                cambiados++;
            }

            if (log && cambiados > 0)
                Debug.Log($"[GatilloComoSelect] '{accion.actionMap?.name}/{accion.name}' ({origen}): {cambiados} binding(s) del grip pasan al gatillo.");
        }

        /// Los interactores de escritorio del rig de VIROO cuelgan de "DesktopControllers".
        private static bool EsDeEscritorio(Transform t)
        {
            while (t != null)
            {
                if (t.name.IndexOf("Desktop", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                t = t.parent;
            }
            return false;
        }
    }
}
