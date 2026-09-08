using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Viroo.Networking;

/// <summary>
/// Reparte a los usuarios de una sesion multiusuario entre varios puntos de
/// llegada a la Escena 3, para que no aterricen todos en el mismo sitio.
///
/// GestorCuestionario teletransporta al usuario local a su campo
/// puntoDestinoEscena3 cuando supera el cuestionario. Este componente no toca
/// ese codigo: mantiene actualizado ese campo con el punto que le corresponde
/// al usuario local, a partir de dos criterios:
///   1) un indice determinista, igual en todos los clientes, obtenido al
///      ordenar los jugadores de la sesion por su ClientId;
///   2) si ese punto ya esta ocupado por el avatar de otro usuario (alguien
///      que llego antes), se avanza al siguiente punto libre.
/// Sin sesion de VIROO (pruebas en el editor) no hay otros jugadores y se usa
/// el primer punto, que es el original. El resto de la transicion no cambia.
/// </summary>
[AddComponentMenu("ViroLab/Escena 3/Destino multiusuario")]
public class DestinoEscena3Multiusuario : MonoBehaviour
{
    [Tooltip("GestorCuestionario cuyo campo puntoDestinoEscena3 se actualiza. Si esta vacio se busca en la escena (incluye objetos inactivos).")]
    public GestorCuestionario gestor;

    [Tooltip("Puntos de llegada, en orden de preferencia. El primero debe ser el punto original (Punto_Inicio_Escena3).")]
    public Transform[] puntos;

    [Tooltip("Radio en metros, medido en planta, dentro del cual un punto se considera ocupado por otro usuario.")]
    public float radioOcupado = 0.7f;

    [Tooltip("Segundos entre reevaluaciones del punto asignado.")]
    public float intervalo = 0.5f;

    [Tooltip("Escribir en la consola cada cambio de punto asignado.")]
    public bool log = true;

    private float _proximo;
    private Transform _ultimo;

    private struct Jugador { public string id; public bool local; public Vector3 pos; }

    private void Update()
    {
        if (Time.unscaledTime < _proximo) return;
        _proximo = Time.unscaledTime + Mathf.Max(0.1f, intervalo);

        if (gestor == null)
            gestor = FindObjectsByType<GestorCuestionario>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (gestor == null || puntos == null || puntos.Length == 0) return;

        var elegido = Elegir();
        if (elegido == null || elegido == _ultimo) return;

        gestor.puntoDestinoEscena3 = elegido;
        _ultimo = elegido;
        if (log) Debug.Log($"[DestinoEscena3Multiusuario] Punto de llegada asignado: {elegido.name}");
    }

    private Transform Elegir()
    {
        var jugadores = LeerJugadores();
        int n = puntos.Length;

        // 1) indice determinista: posicion del usuario local en la lista ordenada por ClientId
        int idx = 0;
        var orden = jugadores.Where(j => !string.IsNullOrEmpty(j.id))
                             .OrderBy(j => j.id, StringComparer.Ordinal).ToList();
        int iLocal = orden.FindIndex(j => j.local);
        if (iLocal >= 0) idx = iLocal % n;

        // 2) primer punto libre a partir de ese indice
        for (int k = 0; k < n; k++)
        {
            var cand = puntos[(idx + k) % n];
            if (cand == null) continue;
            if (!Ocupado(cand.position, jugadores)) return cand;
        }
        return puntos[idx % n];
    }

    /// Jugadores de VIROO presentes en la escena (componente Player de cada avatar, local y remotos).
    private List<Jugador> LeerJugadores()
    {
        var lista = new List<Jugador>();
        var players = FindObjectsByType<Player>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (players == null) return lista;

        foreach (var p in players)
        {
            if (p == null) continue;
            var j = new Jugador { id = null, local = false, pos = p.transform.position };
            if (p is IPlayer ip)
            {
                j.id = Convert.ToString(ip.ClientId);
                j.local = ip.IsLocalPlayer;
            }
            if (string.IsNullOrEmpty(j.id)) j.id = p.GetInstanceID().ToString();
            lista.Add(j);
        }

        // Respaldo: si ninguno se declara local, el mas cercano a la camara lo es.
        if (lista.Count > 0 && !lista.Any(j => j.local) && Camera.main != null)
        {
            var cam = Camera.main.transform.position;
            int mejor = 0; float dMin = float.MaxValue;
            for (int i = 0; i < lista.Count; i++)
            {
                var d = lista[i].pos - cam; d.y = 0f;
                if (d.sqrMagnitude < dMin) { dMin = d.sqrMagnitude; mejor = i; }
            }
            var l = lista[mejor]; l.local = true; lista[mejor] = l;
        }
        return lista;
    }

    private bool Ocupado(Vector3 punto, List<Jugador> jugadores)
    {
        float r2 = radioOcupado * radioOcupado;
        foreach (var j in jugadores)
        {
            if (j.local) continue;
            var d = j.pos - punto; d.y = 0f;
            if (d.sqrMagnitude < r2) return true;
        }
        return false;
    }
}
