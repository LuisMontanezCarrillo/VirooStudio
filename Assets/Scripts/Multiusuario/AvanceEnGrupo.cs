using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Viroo.Interactions;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Hace que los participantes avancen de reto EN GRUPO, usando el
    /// teletransporte propio de VIROO en vez de mover a cada jugador por su
    /// cuenta.
    ///
    /// POR QUE ESTO ES NECESARIO
    /// -------------------------
    /// En una sala VIROO, varias personas con visor comparten el mismo espacio
    /// fisico. La proteccion frente a choques no es un modulo aparte que se
    /// encienda: ES la correspondencia entre la posicion relativa fisica y la
    /// virtual. La documentacion de VIROO lo dice de forma expresa:
    ///
    ///   "users must always keep their relative position within the virtual
    ///    world the same as their relative position in the real world, to avoid
    ///    collisions"
    ///
    ///   "For Room Player VR users, the teleport moves all the users in the room
    ///    at the same time (to keep the users' positioning consistent with each
    ///    other)"
    ///
    ///   docs/3.0/viroo-studio/Environment-Customization/Locomotion/Users-movement.html
    ///
    /// Nuestras transiciones movian a mano el PlayerRoot de cada jugador, que es
    /// el objeto que contiene el avatar, el Chaperone y el XROrigin. Cada visor
    /// acababa con un mapeo distinto entre la sala y el mundo virtual: medido
    /// sobre la escena, dos personas a medio metro en la sala podian verse a
    /// 14,2 metros en la experiencia. Con eso, el aviso de proximidad no puede
    /// funcionar aunque este activo, porque mide sobre posiciones que ya no
    /// corresponden a la realidad.
    ///
    /// COMO FUNCIONA
    /// -------------
    /// 1. Cada participante, al terminar su parte, llama a AnunciarQueTermine().
    ///    Eso ejecuta una accion de VIROO de tipo difusion, de modo que TODOS los
    ///    clientes se enteran.
    /// 2. Cada cliente lleva su propia cuenta de cuantos han anunciado.
    /// 3. Cuando estan todos, UN SOLO cliente (el de menor identificador, para
    ///    que no se dispare cuatro veces) ejecuta el TeleportAllAction, que
    ///    mueve al grupo entero conservando las posiciones relativas.
    ///
    /// LO QUE HAY QUE CABLEAR EN EL INSPECTOR
    /// --------------------------------------
    /// Las acciones de VIROO no se pueden crear editando el archivo de escena a
    /// mano: llevan identificador, inyeccion de dependencias y validadores de
    /// editor. Hay que anadirlas desde Unity. Ver las instrucciones al final de
    /// este archivo.
    /// </summary>
    [AddComponentMenu("ViroLab/Multiusuario/Avance en grupo")]
    public class AvanceEnGrupo : MonoBehaviour, IReiniciable
    {
        [Header("Acciones de VIROO")]
        [Tooltip("UnityEventAction (de difusión) cuyo evento debe llamar a " +
                 "RegistrarAnuncio de ESTE componente. Es el aviso de «ya terminé» " +
                 "que viaja a todos los clientes.")]
        public ObjectAction anuncioDeFin;

        [Tooltip("TeleportAllAction que mueve a todo el grupo. Su campo Teleport " +
                 "Position se fija desde aquí antes de ejecutarlo.")]
        public TeleportAllAction saltoDeGrupo;

        [Tooltip("Destino del grupo. Es el transform donde se situará el primer " +
                 "jugador de la sala; el resto conserva su posición relativa.")]
        public Transform destino;

        [Header("Regla de avance")]
        [Tooltip("Marcado: se espera a que TODOS los participantes hayan terminado. " +
                 "Sin marcar: el primero que termina hace avanzar al grupo.")]
        public bool esperarATodos = true;

        [Tooltip("Segundos máximos de espera. Pasados, el grupo avanza igualmente y " +
                 "se deja constancia en la consola. Evita que una sesión se quede " +
                 "bloqueada si alguien abandona. Pon 0 para esperar sin límite.")]
        public float esperaMaxima = 180f;

        [Header("Avisos para la interfaz")]
        [Tooltip("Se dispara en el cliente de quien acaba de terminar, mientras " +
                 "espera a los demás. Úsalo para mostrar «esperando al resto».")]
        public UnityEvent AlQuedarEnEspera;

        [Tooltip("Se dispara en todos los clientes cuando cambia la cuenta. El " +
                 "entero es cuántos han terminado; úsalo para un marcador.")]
        public UnityEventInt AlCambiarLaCuenta;

        [Tooltip("Se dispara justo antes del salto de grupo, en todos los clientes. " +
                 "Úsalo para el fundido a negro.")]
        public UnityEvent AntesDelSalto;

        [Tooltip("Se dispara tras el salto, en todos los clientes. Úsalo para el " +
                 "fundido de entrada y para encender lo que toque del reto nuevo. " +
                 "Existe porque el TeleportAllAction de VIROO NO expone ningún evento " +
                 "en el Inspector: su OnActionExecuted es un evento de C#, no un " +
                 "UnityEvent serializado, así que no hay forma de engancharse a él " +
                 "desde el editor.")]
        public UnityEvent DespuesDelSalto;

        [Tooltip("Segundos entre disparar «Antes del salto» y mover al grupo. Tiene " +
                 "que dar tiempo a que el fundido llegue a negro: si el salto ocurre " +
                 "en el mismo fotograma, el participante ve el teletransporte a plena " +
                 "luz y el negro le llega después. 2 segundos es la duración por " +
                 "defecto del fundido.")]
        public float margenAntesDelSalto = 2f;

        [Tooltip("Segundos entre ejecutar el salto y disparar «Después del salto». " +
                 "Da margen a que VIROO haya colocado a los participantes.")]
        public float margenTrasElSalto = 0.35f;

        [Serializable] public class UnityEventInt : UnityEvent<int> { }

        [Header("Diagnóstico")]
        public bool log = true;

        private int _anuncios;
        private bool _yaAnuncie;
        private bool _yaSalto;
        private float _instanteDelPrimerAnuncio = -1f;


        // ================================================================
        //  Puesta a punto
        // ================================================================

        /// Al anadir el componente desde el Inspector, deja los campos ya
        /// rellenos con las acciones que haya en este mismo objeto.
        private void Reset() { BuscarLoQueFalte(); }

        private void Awake()
        {
            // Red de seguridad. Las dos acciones van siempre en el mismo objeto, y
            // olvidar un arrastre no se nota hasta que el estudiante ya ha hecho el
            // reto entero y el grupo no avanza.
            BuscarLoQueFalte();

            if (anuncioDeFin == null)
                Debug.LogWarning("[AvanceEnGrupo] Sin acción de difusión: ni asignada ni en este " +
                                 "objeto. Añade un UnityEventAction.", this);
            if (saltoDeGrupo == null)
                Debug.LogWarning("[AvanceEnGrupo] Sin TeleportAllAction: ni asignado ni en este " +
                                 "objeto. Añádelo o el grupo no se moverá.", this);
        }

        private void BuscarLoQueFalte()
        {
            // El TeleportAllAction TAMBIEN es un ObjectAction, asi que para el
            // aviso hay que coger el que no lo sea.
            if (anuncioDeFin == null)
                foreach (var a in GetComponents<ObjectAction>())
                    if (a is not TeleportAllAction) { anuncioDeFin = a; break; }

            if (saltoDeGrupo == null) saltoDeGrupo = GetComponent<TeleportAllAction>();
        }

        // ================================================================
        //  Entrada: lo llama el reto cuando el participante termina
        // ================================================================

        /// <summary>
        /// El participante local ha terminado su parte. Difunde el aviso al resto.
        /// Conéctalo donde antes se llamaba a IniciarFadeOut o a la transición.
        /// </summary>
        public void AnunciarQueTermine()
        {
            if (_yaAnuncie || _yaSalto) return;
            _yaAnuncie = true;

            if (anuncioDeFin == null)
            {
                Debug.LogError("[AvanceEnGrupo] Falta la acción de aviso. Sin ella no " +
                               "hay forma de decirle al resto que has terminado, y el " +
                               "grupo no avanzará.", this);
                return;
            }

            if (log) Debug.Log("[AvanceEnGrupo] Anunciando fin de reto al resto de la sala.");
            anuncioDeFin.Execute();

            AlQuedarEnEspera?.Invoke();
        }

        // ================================================================
        //  Recepción: lo llama el UnityEvent de la acción difundida
        // ================================================================

        /// <summary>
        /// Alguien ha terminado. Se ejecuta en TODOS los clientes, incluido el de
        /// quien lo anunció. Conéctalo al evento del UnityEventAction.
        /// </summary>
        public void RegistrarAnuncio()
        {
            if (_yaSalto) return;

            _anuncios++;
            if (_instanteDelPrimerAnuncio < 0f) _instanteDelPrimerAnuncio = Time.unscaledTime;

            int total = ParticipantesConectados();
            if (log) Debug.Log($"[AvanceEnGrupo] Han terminado {_anuncios} de {total}.");
            AlCambiarLaCuenta?.Invoke(_anuncios);

            if (!esperarATodos || _anuncios >= total) IntentarSaltar("están todos");
        }

        private void Update()
        {
            if (_yaSalto || esperaMaxima <= 0f || _instanteDelPrimerAnuncio < 0f) return;
            if (Time.unscaledTime - _instanteDelPrimerAnuncio < esperaMaxima) return;

            Debug.LogWarning($"[AvanceEnGrupo] Se agotó la espera de {esperaMaxima:F0} s con " +
                             $"{_anuncios} de {ParticipantesConectados()} participantes. " +
                             "El grupo avanza igualmente para no quedarse bloqueado.", this);
            IntentarSaltar("se agotó la espera");
        }

        // ================================================================
        //  Salto de grupo
        // ================================================================

        private void IntentarSaltar(string motivo)
        {
            if (_yaSalto) return;
            _yaSalto = true;

            StartCoroutine(RutinaDelSalto(motivo));
        }

        /// <summary>
        /// El salto, en orden: se avisa, se deja que el fundido cierre, se mueve al
        /// grupo y se avisa de la llegada.
        ///
        /// El primer margen no es decorativo. Antes se invocaba AntesDelSalto y se
        /// ejecutaba el teletransporte en el mismo fotograma, de modo que el
        /// participante veia el salto a plena luz y el fundido le llegaba despues.
        /// </summary>
        private IEnumerator RutinaDelSalto(string motivo)
        {
            AntesDelSalto?.Invoke();

            if (margenAntesDelSalto > 0f) yield return new WaitForSeconds(margenAntesDelSalto);

            // Un unico cliente ejecuta el salto. Si lo ejecutaran los dos, el grupo
            // se teletransportaria dos veces seguidas.
            if (SoyElClienteDesignado())
            {
                if (saltoDeGrupo == null)
                {
                    Debug.LogError("[AvanceEnGrupo] Falta el TeleportAllAction. El grupo no se " +
                                   "moverá. NO sustituyas esto moviendo el PlayerRoot a mano: " +
                                   "eso rompe la protección frente a choques.", this);
                }
                else
                {
                    if (destino != null) saltoDeGrupo.TeleportPosition = destino;
                    if (log) Debug.Log($"[AvanceEnGrupo] Ejecutando el salto de grupo ({motivo}).");
                    saltoDeGrupo.Execute();
                }
            }
            else if (log)
            {
                Debug.Log($"[AvanceEnGrupo] Salto de grupo ({motivo}); lo ejecuta otro cliente.");
            }

            // DespuesDelSalto se dispara en TODOS los clientes, tambien en los que no
            // ejecutaron el teletransporte, porque esta rutina corre en todos.
            if (margenTrasElSalto > 0f) yield return new WaitForSeconds(margenTrasElSalto);
            DespuesDelSalto?.Invoke();
        }

        /// <summary>
        /// Fuerza el avance del grupo sin esperar a nadie. Pensado para que quien
        /// modera la prueba pueda desatascar una sesión desde el cliente de
        /// escritorio.
        /// </summary>
        public void ForzarAvance()
        {
            Debug.LogWarning("[AvanceEnGrupo] Avance forzado manualmente.", this);
            IntentarSaltar("avance forzado");
        }

        // ================================================================
        //  Utilidades de sesión
        // ================================================================

        // El criterio de cuanta gente hay y de a quien le toca ejecutar vive en
        // SesionViroo, porque el reinicio entre tandas necesita exactamente lo
        // mismo. Dos copias del criterio es la forma de que un dia dejen de
        // coincidir y la accion se dispare dos veces o ninguna.
        private int ParticipantesConectados() => SesionViroo.ParticipantesConectados();
        private bool SoyElClienteDesignado()  => SesionViroo.SoyElClienteDesignado();

        // ================================================================
        //  Reinicio entre tandas
        // ================================================================

        /// <summary>
        /// Borra la cuenta de anuncios y el cierre del salto. Sin esto, la tanda
        /// siguiente heredaria un _yaSalto en cierto y el grupo no avanzaria
        /// nunca del reto donde quedo la anterior.
        /// </summary>
        public void ReiniciarAlInicio()
        {
            _anuncios = 0;
            _yaAnuncie = false;
            _yaSalto = false;
            _instanteDelPrimerAnuncio = -1f;
        }

    }
}

/*
CABLEADO EN UNITY
=================
Las acciones de VIROO llevan identificador, inyeccion de dependencias y
validadores de editor, asi que no se pueden crear editando el archivo de escena
a mano: hay que anadirlas desde el Inspector.

La lista completa y actualizada esta en CABLEADO.md, en esta misma carpeta.
Cubre las dos transiciones, el reinicio entre tandas, donde van las marcas de
reunion y que hay que retirar cuando todo esto este probado.

Se saco de aqui a proposito: al entrar ReunionEnElCentro dejo de ser cierto que
la salida del reto llame directamente a AnunciarQueTermine, y dos copias de unas
instrucciones es la forma de que un dia dejen de coincidir.
*/
