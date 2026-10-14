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
    /// binding override sobre sus acciones, sustituyendo {GripButton} por
    /// {TriggerButton} y {Grip} por {Trigger}. Solo toca los bindings de
    /// &lt;XRController&gt;; los de seguimiento de manos (MetaAimHand) y los
    /// interactores de escritorio (raton) quedan como estan. El override vive
    /// solo en memoria: al salir de Play todo vuelve a su estado original.
    ///
    /// QUE SE REASIGNA, Y POR QUE SON TRES COSAS Y NO UNA
    /// -------------------------------------------------
    /// En las pruebas del 29 de septiembre de 2026 el cuestionario seguia
    /// exigiendo el boton lateral del mando, pese a que este componente ya
    /// estaba en la escena. El motivo: solo reasignaba 'selectInput'.
    ///
    /// Los interactores de XRI llevan tres entradas distintas y cada familia de
    /// objetos escucha una:
    ///
    ///   selectInput    en XRBaseInputInteractor  -> los XRSimpleInteractable,
    ///                  que en esta escena son ocho (casillero, maniqui,
    ///                  lavamanos, logos, pausa).
    ///   activateInput  en XRBaseInputInteractor  -> el uso del objeto agarrado.
    ///   uiPressInput   en XRRayInteractor y en NearFarInteractor -> TODA la
    ///                  interfaz de Unity, que es por donde van los botones A, B
    ///                  y C del cuestionario y los del tablero. La escena tiene
    ///                  diez TrackedDeviceGraphicRaycaster, o sea diez lienzos
    ///                  que dependen de esta entrada.
    ///
    /// Reasignar solo la primera dejaba la mitad de la experiencia en el grip.
    ///
    /// POR QUE YA NO HAY VENTANA DE BUSQUEDA
    /// ------------------------------------
    /// Antes el componente se apagaba solo a los 25 segundos para no rastrear la
    /// escena durante toda la sesion. Pero en una sala VIROO los participantes
    /// entran cuando entran, y quien llegaba tarde se quedaba sin reasignar y sin
    /// que nadie se enterara. El rastreo se mantiene toda la sesion con un
    /// intervalo mas largo. El coste real nunca estuvo en buscar, sino en
    /// reasignar, y eso ocurre una sola vez por accion gracias a _hechas.
    /// </summary>
    [AddComponentMenu("ViroLab/Pasteurizador/Gatillo como Select")]
    [DefaultExecutionOrder(-50)]
    public class GatilloComoSelect : MonoBehaviour, IReiniciable
    {
        [Tooltip("Segundos entre rastreos del rig. El rig de VIROO puede aparecer despues " +
                 "de cargar la escena, y en una sala los participantes entran cuando " +
                 "entran, por eso se reintenta durante toda la sesion.")]
        public float retrySeconds = 2f;

        [Tooltip("Reasignar solo los interactores del jugador LOCAL. Los avatares de " +
                 "los demas no reciben entrada en esta maquina, de modo que tocar sus " +
                 "acciones no sirve de nada y cuesta.")]
        public bool soloElJugadorLocal = true;

        [Tooltip("Escribir en la consola las acciones reasignadas y el binding que " +
                 "tenian antes. Dejelo encendido para la sesion de comprobacion en la " +
                 "sala: es lo unico que dice que entrada usa de verdad el rig de VIROO.")]
        public bool log = true;

        private readonly HashSet<InputAction> _hechas = new HashSet<InputAction>();
        private float _proximo;
        private int _reasignadas;

        /// <summary>
        /// Vuelve a empezar para la tanda siguiente. Los visores de la segunda
        /// pareja del dia traen interactores nuevos, con acciones nuevas que
        /// nadie ha tocado todavia.
        /// </summary>
        public void ReiniciarAlInicio()
        {
            _reasignadas = 0;
            _proximo = 0f;
            enabled = true;
        }

        private void Update()
        {
            if (Time.unscaledTime < _proximo) return;
            _proximo = Time.unscaledTime + Mathf.Max(0.1f, retrySeconds);

            var interactores = FindObjectsByType<XRBaseInputInteractor>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (interactores == null) return;

            Transform raizLocal = null;
            if (soloElJugadorLocal && Camera.main != null)
                raizLocal = Camera.main.transform.root;

            foreach (var it in interactores)
            {
                if (it == null || EsDeEscritorio(it.transform)) continue;
                if (raizLocal != null && it.transform.root != raizLocal) continue;

                // Los XRSimpleInteractable de la escena: casillero, maniqui,
                // lavamanos, logos, pausa.
                Reasignar(it.selectInput, it.name + "/select");
                Reasignar(it.activateInput, it.name + "/activate");

                // Toda la interfaz de Unity, que es por donde van los botones del
                // cuestionario y los del tablero. Es lo que faltaba.
                var rayo = it as XRRayInteractor;
                if (rayo != null) Reasignar(rayo.uiPressInput, it.name + "/uiPress");

                var cerca = it as NearFarInteractor;
                if (cerca != null) Reasignar(cerca.uiPressInput, it.name + "/uiPress");
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
            string antes = null;
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

                if (antes == null) antes = b.path;
                accion.ApplyBindingOverride(i, nuevo);
                cambiados++;
            }

            _reasignadas += cambiados;

            // Se escriben los dos casos, el que cambia y el que no. Saber que una
            // accion YA estaba en el gatillo vale tanto como saber que se movio:
            // es lo que dice si el fallo del cuestionario era este o es otro.
            if (!log) return;

            if (cambiados > 0)
                Debug.Log($"[GatilloComoSelect] '{accion.actionMap?.name}/{accion.name}' ({origen}): " +
                          $"{cambiados} binding(s) pasan al gatillo. Antes: {antes}");
            else if (bindings.Count > 0)
                Debug.Log($"[GatilloComoSelect] '{accion.actionMap?.name}/{accion.name}' ({origen}): " +
                          $"sin cambios, no usa el grip. Bindings: {Bindings(accion)}");
        }

        /// Lista los bindings de mando de una accion, para la consola. Sin esto,
        /// una accion que no cambia no deja rastro y no hay forma de saber desde
        /// fuera que entrada usa de verdad el rig de VIROO.
        private static string Bindings(InputAction accion)
        {
            var sb = new System.Text.StringBuilder();
            var bindings = accion.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || b.isPartOfComposite || string.IsNullOrEmpty(b.path)) continue;
                if (b.path.IndexOf("<XRController>", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(b.effectivePath);
            }
            return sb.Length > 0 ? sb.ToString() : "ninguno de mando";
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
