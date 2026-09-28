using System;
using System.Collections.Generic;
using UnityEngine;
using Viroo.Networking;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Lo poco que la experiencia necesita saber de la sesion de VIROO: quien
    /// esta haciendo los retos y a quien le toca ejecutar las acciones que solo
    /// deben ocurrir una vez.
    ///
    /// QUIEN CUENTA COMO PARTICIPANTE
    /// ------------------------------
    /// Edgar Gonzalez confirma que los usuarios conectados desde PC tambien son
    /// participantes de la sesion. Eso importa mucho aqui: el cliente de
    /// escritorio de quien modera aparece en la lista de jugadores pero nunca va
    /// a terminar un reto. Si se contara, el grupo esperaria indefinidamente a
    /// alguien que no esta jugando, y solo avanzaria al agotarse la espera
    /// maxima, en cada transicion y en cada una de las veinte tandas del dia.
    ///
    /// La regla: si hay alguien en una arena, solo cuentan los de la arena. Sin
    /// nadie en ninguna arena (pruebas en el editor, o una sesion enteramente de
    /// escritorio) se cuentan todos, que es el comportamiento razonable cuando la
    /// distincion no aplica.
    ///
    /// El criterio se evalua igual en todos los clientes, de modo que todos
    /// llegan al mismo numero y al mismo designado sin negociar nada.
    /// </summary>
    public static class SesionViroo
    {
        private static Player[] Jugadores()
        {
            return UnityEngine.Object.FindObjectsByType<Player>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                ?? Array.Empty<Player>();
        }

        /// <summary>
        /// Cierto si este jugador esta dentro de una sala VR. Se comprueban
        /// varias formas de "sin arena" porque VIROO puede representarla como
        /// nulo, cadena vacia, cero o un GUID vacio, y no se puede saber cual sin
        /// una sesion real delante.
        /// </summary>
        private static bool EstaEnUnaArena(IPlayer ip)
        {
            string a = Convert.ToString(ip.ArenaId);
            if (string.IsNullOrWhiteSpace(a)) return false;

            a = a.Trim();
            if (a == "0") return false;
            if (a == Guid.Empty.ToString()) return false;
            return true;
        }

        /// Identificadores de cliente de quienes cuentan como participantes,
        /// ordenados igual en todos los clientes.
        private static List<string> IdsDeParticipantes(out string mio)
        {
            mio = null;
            var enArena = new List<string>();
            var todos = new List<string>();
            string mioEnArena = null, mioEnTodos = null;

            foreach (var p in Jugadores())
            {
                if (p is not IPlayer ip) continue;

                string id = Convert.ToString(ip.ClientId);
                if (string.IsNullOrEmpty(id)) continue;

                todos.Add(id);
                if (ip.IsLocalPlayer == true) mioEnTodos = id;

                if (!EstaEnUnaArena(ip)) continue;
                enArena.Add(id);
                if (ip.IsLocalPlayer == true) mioEnArena = id;
            }

            // Con alguien en una arena, manda la arena. Sin nadie, todos.
            var lista = enArena.Count > 0 ? enArena : todos;
            mio = enArena.Count > 0 ? mioEnArena : mioEnTodos;

            lista.Sort(string.CompareOrdinal);
            return lista;
        }

        /// <summary>
        /// Participantes que van a terminar los retos. Sin sesion de VIROO
        /// (pruebas en el editor) devuelve 1, de modo que lo que espere al grupo
        /// no se quede colgado.
        /// </summary>
        public static int ParticipantesConectados()
        {
            var lista = IdsDeParticipantes(out _);
            return Mathf.Max(1, lista.Count);
        }

        /// <summary>
        /// Puesto del participante local, contando desde cero y por orden de
        /// identificador de cliente. El orden es el mismo en todos los clientes,
        /// de modo que cada uno sabe cual es su sitio sin negociarlo.
        ///
        /// Devuelve 0 si no hay datos de sesion.
        /// </summary>
        public static int IndiceDelJugadorLocal()
        {
            var lista = IdsDeParticipantes(out string mio);
            if (mio == null || lista.Count == 0) return 0;

            int i = lista.IndexOf(mio);
            return i < 0 ? 0 : i;
        }

        /// <summary>
        /// Cierto en UN SOLO cliente: el del identificador menor entre los
        /// participantes. Quien no cuenta como participante nunca resulta
        /// designado, de modo que el cliente de escritorio de quien modera no
        /// puede disparar el salto antes de tiempo.
        ///
        /// Sin datos de sesion devuelve cierto, para no bloquear la experiencia.
        /// </summary>
        public static bool SoyElClienteDesignado()
        {
            var lista = IdsDeParticipantes(out string mio);
            if (lista.Count == 0) return true;      // sin sesion, yo mismo
            if (mio == null) return false;          // estoy fuera de la cuenta
            return mio == lista[0];
        }

        /// <summary>
        /// Quien hay conectado, con su identificador, su arena y si cuenta o no.
        /// Se imprime al contar participantes para poder ver EN EL ENSAYO si la
        /// cuenta coincide con el numero de estudiantes con visor.
        /// </summary>
        public static string Retrato()
        {
            var jugadores = Jugadores();
            if (jugadores.Length == 0) return "sin sesión de VIROO";

            bool hayArena = false;
            foreach (var p in jugadores)
                if (p is IPlayer ip && EstaEnUnaArena(ip)) { hayArena = true; break; }

            var sb = new System.Text.StringBuilder();
            foreach (var p in jugadores)
            {
                if (p is not IPlayer ip) continue;

                bool cuenta = !hayArena || EstaEnUnaArena(ip);
                string arena = Convert.ToString(ip.ArenaId);
                if (string.IsNullOrWhiteSpace(arena)) arena = "(sin arena)";

                sb.Append(sb.Length == 0 ? "" : " | ");
                sb.Append(Convert.ToString(ip.ClientId));
                sb.Append(ip.IsLocalPlayer == true ? " (yo)" : "");
                sb.Append(' ').Append(arena);
                sb.Append(cuenta ? " [cuenta]" : " [NO cuenta]");
            }
            return sb.Length == 0 ? "jugadores sin datos" : sb.ToString();
        }
    }
}
