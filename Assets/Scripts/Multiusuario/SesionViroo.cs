using System;
using System.Collections.Generic;
using UnityEngine;
using Viroo.Networking;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Lo poco que la experiencia necesita saber de la sesion de VIROO: cuanta
    /// gente hay y a quien le toca ejecutar las acciones que solo deben ocurrir
    /// una vez.
    ///
    /// Estaba repetido dentro de AvanceEnGrupo. Al aparecer el reinicio entre
    /// tandas hacia falta lo mismo en dos sitios, y dos copias del criterio de
    /// "quien manda" es justo la forma de que un dia dejen de coincidir y la
    /// accion se dispare dos veces o ninguna.
    /// </summary>
    public static class SesionViroo
    {
        /// <summary>
        /// Participantes conectados. Sin sesion de VIROO (pruebas en el editor)
        /// devuelve 1, de modo que lo que espere al grupo no se quede colgado.
        /// </summary>
        public static int ParticipantesConectados()
        {
            var jugadores = UnityEngine.Object.FindObjectsByType<Player>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int n = jugadores != null ? jugadores.Length : 0;
            return Mathf.Max(1, n);
        }

        /// <summary>
        /// Puesto del participante local en la sala, contando desde cero, por
        /// orden de identificador de cliente. El orden es el mismo en todos los
        /// clientes, de modo que cada uno sabe cual es su sitio sin negociarlo.
        ///
        /// Devuelve 0 si no hay datos de sesion (pruebas en el editor).
        /// </summary>
        public static int IndiceDelJugadorLocal()
        {
            var jugadores = UnityEngine.Object.FindObjectsByType<Player>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (jugadores == null || jugadores.Length == 0) return 0;

            string mio = null;
            var ids = new List<string>();

            foreach (var p in jugadores)
            {
                if (p is not IPlayer ip) continue;
                string id = Convert.ToString(ip.ClientId);
                if (string.IsNullOrEmpty(id)) continue;

                ids.Add(id);
                if (ip.IsLocalPlayer == true) mio = id;
            }

            if (mio == null || ids.Count == 0) return 0;

            ids.Sort(string.CompareOrdinal);
            int i = ids.IndexOf(mio);
            return i < 0 ? 0 : i;
        }

        /// <summary>
        /// Cierto en UN SOLO cliente de la sala: el del identificador menor.
        /// El criterio se evalua igual en todos, de modo que todos coinciden en
        /// quien es sin necesidad de negociarlo.
        ///
        /// Sin datos de sesion devuelve cierto, para no bloquear la experiencia
        /// en el editor ni si VIROO todavia no ha poblado los jugadores.
        /// </summary>
        public static bool SoyElClienteDesignado()
        {
            return IndiceDelJugadorLocal() == 0;
        }
    }
}
