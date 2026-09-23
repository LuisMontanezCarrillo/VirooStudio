using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Viroo.Interactions;
using Viroo.Networking;

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
    public class AvanceEnGrupo : MonoBehaviour
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

        [Serializable] public class UnityEventInt : UnityEvent<int> { }

        [Header("Diagnóstico")]
        public bool log = true;

        private int _anuncios;
        private bool _yaAnuncie;
        private bool _yaSalto;
        private float _instanteDelPrimerAnuncio = -1f;

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

            AntesDelSalto?.Invoke();

            // Un unico cliente ejecuta el salto. Si lo ejecutaran los cuatro, el
            // grupo se teletransportaria cuatro veces seguidas.
            if (!SoyElClienteDesignado())
            {
                if (log) Debug.Log($"[AvanceEnGrupo] Salto de grupo ({motivo}); lo ejecuta otro cliente.");
                return;
            }

            if (saltoDeGrupo == null)
            {
                Debug.LogError("[AvanceEnGrupo] Falta el TeleportAllAction. El grupo no se " +
                               "moverá. NO sustituyas esto moviendo el PlayerRoot a mano: " +
                               "eso rompe la protección frente a choques.", this);
                return;
            }

            if (destino != null) saltoDeGrupo.TeleportPosition = destino;

            if (log) Debug.Log($"[AvanceEnGrupo] Ejecutando el salto de grupo ({motivo}).");
            saltoDeGrupo.Execute();
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

        /// Participantes conectados a la sesión. Sin sesión de VIROO (pruebas en
        /// el editor) devuelve 1, de modo que el avance es inmediato.
        private int ParticipantesConectados()
        {
            var jugadores = FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int n = jugadores != null ? jugadores.Length : 0;
            return Mathf.Max(1, n);
        }

        /// Cliente designado para ejecutar el salto: el de identificador menor.
        /// El criterio es el mismo en todos los clientes, de modo que todos
        /// coinciden en quién es.
        private bool SoyElClienteDesignado()
        {
            var jugadores = FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (jugadores == null || jugadores.Length == 0) return true;   // sin sesión, yo mismo

            string menor = null;
            string mio = null;

            foreach (var p in jugadores)
            {
                if (p is not IPlayer ip) continue;
                string id = Convert.ToString(ip.ClientId);
                if (string.IsNullOrEmpty(id)) continue;

                if (menor == null || string.CompareOrdinal(id, menor) < 0) menor = id;
                if (ip.IsLocalPlayer == true) mio = id;
            }

            if (menor == null || mio == null) return true;   // sin datos, no bloquear
            return mio == menor;
        }
    }
}

/*
INSTRUCCIONES DE CABLEADO EN UNITY
==================================
Las acciones de VIROO llevan identificador, inyección de dependencias y
validadores de editor, así que hay que crearlas desde el inspector. Por cada
transición (reto 1 a escena 2, y escena 2 a reto 3):

1. Crear un objeto vacío, por ejemplo "Avance_Reto1".

2. Añadirle "Viroo/Actions/UnityEventAction". Ese es el aviso de fin, y se
   difunde a todos los clientes.

3. Añadirle "Viroo/Actions/TeleportAllAction". Dejar su Teleport Position
   vacío: lo fija este componente antes de ejecutarlo.

4. Añadirle este componente, "ViroLab/Multiusuario/Avance en grupo", y asignar:
      Anuncio de fin  -> el UnityEventAction del paso 2
      Salto de grupo  -> el TeleportAllAction del paso 3
      Destino         -> Punto_Destino_Planta  (o Punto_Inicio_Escena3)

5. En el UnityEventAction del paso 2, en su evento expuesto, añadir una entrada
   que llame a AvanceEnGrupo.RegistrarAnuncio de este mismo objeto.

6. Donde hoy se llama a la transición, llamar en su lugar a
   AvanceEnGrupo.AnunciarQueTermine.

7. Conectar AntesDelSalto al fundido a negro, y AlQuedarEnEspera al cartel de
   "esperando al resto del grupo".

QUE HAY QUE RETIRAR
-------------------
- FadeOutVR: su teletransporte manual (mueve camaraVR.root).
- GestorCuestionario: el bloque de teletransporte de FinalizarCuestionario.
- DestinoEscena3Multiusuario: queda sin sentido. Repartir a cada usuario a un
  punto distinto es justo lo contrario de lo que exige una sala compartida.
*/
