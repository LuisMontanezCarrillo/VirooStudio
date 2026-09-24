using UnityEngine;

namespace ViroLab.Multiusuario
{
    /// <summary>
    /// Guarda una foto del estado de la escena al arrancar y la devuelve tal cual
    /// entre tandas.
    ///
    /// QUE RESUELVE
    /// ------------
    /// Los componentes que guardan avance saben reiniciarse solos (ver
    /// IReiniciable), pero buena parte del estado de la experiencia no vive en
    /// ningun script: vive en la jerarquia. Lienzos que se encienden y se apagan,
    /// y sobre todo componentes de interaccion que se desactivan a si mismos
    /// cuando ya se han usado.
    ///
    /// En esta escena hay tres de esos: el lavamanos, el maniqui y uno de los
    /// casilleros llaman a set_enabled(false) sobre su propio componente de
    /// interaccion al completarse. Sin devolverlos, la segunda pareja del dia
    /// entraria y no podria hacer clic en nada.
    ///
    /// Enumerar todo eso a mano en un UnityEvent seria una decena de entradas que
    /// hay que mantener al dia cada vez que se toque la escena, y una entrada que
    /// se olvide es un fallo silencioso que no aparece hasta la tanda siguiente.
    /// Asi que en vez de enumerarlo, se fotografia.
    ///
    /// POR QUE ES SEGURO
    /// -----------------
    /// La foto se toma en Awake y guarda referencias explicitas a los objetos que
    /// existian entonces, que son los que trae la escena guardada. Los jugadores
    /// de VIROO se instancian despues, de modo que nunca entran en la foto y
    /// nunca se tocan al restaurar.
    /// </summary>
    [AddComponentMenu("ViroLab/Multiusuario/Foto de la escena")]
    [DefaultExecutionOrder(-10000)]
    public class FotoDeLaEscena : MonoBehaviour, IReiniciable
    {
        [Header("Qué se fotografía")]
        [Tooltip("Raíces de la jerarquía cuyo estado se guarda. Arrastra aquí los " +
                 "objetos que contienen la experiencia. Todo lo que cuelgue de " +
                 "ellos entra en la foto. Lo que no cuelgue de ellos no se toca.")]
        public Transform[] raices;

        [Tooltip("Marcado: también se guarda y devuelve el estado activado de los " +
                 "componentes, no solo el de los objetos. Es lo que devuelve los " +
                 "clics al lavamanos, al maniquí y al casillero.")]
        public bool incluirComponentes = true;

        [Header("Diagnóstico")]
        public bool log = true;

        private GameObject[] _objetos;
        private bool[] _objetoActivo;
        private Behaviour[] _componentes;
        private bool[] _componenteActivo;

        private void Awake()
        {
            Fotografiar();
        }

        /// <summary>
        /// Vuelve a tomar la foto. Útil si algo del estado de partida se
        /// construye después de Awake.
        /// </summary>
        [ContextMenu("Volver a fotografiar")]
        public void Fotografiar()
        {
            if (raices == null || raices.Length == 0)
            {
                Debug.LogError("[FotoDeLaEscena] No hay ninguna raíz asignada: no se guardará " +
                               "nada y el reinicio entre tandas dejará la escena a medias.", this);
                _objetos = new GameObject[0];
                _objetoActivo = new bool[0];
                _componentes = new Behaviour[0];
                _componenteActivo = new bool[0];
                return;
            }

            var objetos = new System.Collections.Generic.List<GameObject>();
            var componentes = new System.Collections.Generic.List<Behaviour>();

            foreach (var raiz in raices)
            {
                if (raiz == null) continue;

                // true: incluye tambien lo que arranca apagado, que es justo lo
                // que despues se enciende y hay que volver a apagar.
                foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    objetos.Add(t.gameObject);

                    if (!incluirComponentes) continue;
                    foreach (var c in t.GetComponents<Behaviour>())
                    {
                        // Este componente no se fotografia a si mismo: si alguien
                        // lo desactivara, restaurarlo seria lo de menos.
                        if (c == null || ReferenceEquals(c, this)) continue;
                        componentes.Add(c);
                    }
                }
            }

            _objetos = objetos.ToArray();
            _objetoActivo = new bool[_objetos.Length];
            for (int i = 0; i < _objetos.Length; i++) _objetoActivo[i] = _objetos[i].activeSelf;

            _componentes = componentes.ToArray();
            _componenteActivo = new bool[_componentes.Length];
            for (int i = 0; i < _componentes.Length; i++) _componenteActivo[i] = _componentes[i].enabled;

            if (log)
                Debug.Log($"[FotoDeLaEscena] Guardados {_objetos.Length} objetos y " +
                          $"{_componentes.Length} componentes como estado de partida.", this);
        }

        /// <summary>
        /// Devuelve la escena a la foto. Solo escribe donde algo cambió, para no
        /// disparar los OnEnable de media escena cada vez.
        /// </summary>
        public void ReiniciarAlInicio()
        {
            int tocados = 0;

            if (_objetos != null)
                for (int i = 0; i < _objetos.Length; i++)
                {
                    var o = _objetos[i];
                    if (o == null) continue;
                    if (o.activeSelf == _objetoActivo[i]) continue;
                    o.SetActive(_objetoActivo[i]);
                    tocados++;
                }

            if (_componentes != null)
                for (int i = 0; i < _componentes.Length; i++)
                {
                    var c = _componentes[i];
                    if (c == null) continue;
                    if (c.enabled == _componenteActivo[i]) continue;
                    c.enabled = _componenteActivo[i];
                    tocados++;
                }

            if (log) Debug.Log($"[FotoDeLaEscena] Estado devuelto: {tocados} cambios deshechos.", this);
        }
    }
}
