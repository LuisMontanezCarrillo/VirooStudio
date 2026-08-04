using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ViroLab.Pasteurizador
{
    // Hover + Click sobre partes del pasteurizador.
    // Soporta dos modos simultaneos:
    //   - Mouse: raycast desde la camara principal en cada frame
    //   - VR: arrastras un GameObject (controlador o cabeza) en RaySource y se usa su forward
    //
    // IMPORTANTE: soporta tanto el Input System nuevo (Viroo/XR) como el legacy
    // UnityEngine.Input via #if ENABLE_INPUT_SYSTEM. No lanza excepciones si
    // el Input legacy esta deshabilitado en Player Settings.
    //
    // Tambien aplica un material highlight como overlay y dispara eventos.
    [DisallowMultipleComponent]
    public class PasteurizerHoverHandler : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("Registro de partes. Si esta null se busca en el mismo GameObject.")]
        public PasteurizerPartsRegistry registry;

        [Header("Source de Raycast (modo VR)")]
        [Tooltip("Si se asigna, el ray se lanza desde aqui usando su forward. Tipico: XR controller o head transform.")]
        public Transform raySource;
        [Tooltip("Distancia maxima del ray.")]
        public float rayMaxDistance = 8f;
        [Tooltip("Si true, ademas del ray VR tambien se intenta raycast con mouse.")]
        public bool alsoUseMouse = true;
        public LayerMask hitLayers = ~0;

        [Header("VR en VIROO (auto)")]
        [Tooltip("Busca el interactor de mano del rig de VIROO y lo usa como origen del ray " +
                 "y como gatillo. No requiere componentes de red: la seleccion es local para " +
                 "cada estudiante.")]
        public bool autoBindVirooController = true;
        [Tooltip("Mano preferida. Si no existe se usa la otra.")]
        public InteractorHandedness preferredHand = InteractorHandedness.Right;

        [Header("Trigger de click")]
        [Tooltip("KeyCode adicional para pin (solo si el Input Legacy esta activo).")]
        public KeyCode pinKey = KeyCode.None;

        [Header("Atajos de teclado")]
        [Tooltip("Tecla para cerrar el pin activo (default: Escape).")]
        public KeyCode clearPinKey = KeyCode.Escape;
        [Tooltip("Tecla para hacer focus/zoom sobre la pieza pineada (default: F).")]
        public KeyCode focusKey = KeyCode.F;
        [Tooltip("Distancia (m) a la que se posiciona la camara cuando hace focus.")]
        public float focusDistance = 1.5f;

        [Header("Visuals")]
        public Color hoverColor = new Color(0.16f, 0.60f, 0.05f);          // verde tenue (igual que Mat_BrilloInteractivo, atenuado)
        public Color pinColor = new Color(0.26666668f, 0.972549f, 0.078431375f); // verde casillero (Mat_BrilloInteractivo)
        [Range(0f, 1f)] public float emissiveIntensity = 0.6f;

        [Header("Events")]
        public PartEvent OnHoverEnter;
        public PartEvent OnHoverExit;
        public PartEvent OnPartPinned;
        public UnityEvent OnPinCleared;

        [System.Serializable] public class PartEvent : UnityEvent<PartHitInfo> {}

        public class PartHitInfo
        {
            public GameObject part;
            public string partName;
            public string subsystemKey;
            public SubsystemInfo subsystem;
            public string description;
        }

        private GameObject _hovered;
        private GameObject _pinned;
        private readonly Dictionary<Renderer, Material[]> _originalMats = new();
        private Camera _mainCam;

        // Interactor de mano del rig de VIROO. El rig se crea en runtime, asi que no se
        // puede resolver en Awake: se reintenta cada BindRetrySeconds hasta encontrarlo.
        private NearFarInteractor _handInteractor;
        private float _nextBindAttempt;
        private const float BindRetrySeconds = 0.5f;

        // Click inyectado desde fuera (ver NotifyExternalClick).
        private bool _externalClickQueued;

        // El rayo esta sobre un Canvas de UI (carrusel, cuestionario, dashboard...).
        // Mientras sea true no se pinea nada: la UI no tiene colliders fisicos, asi
        // que el raycast la atraviesa y golpearia la maquina que hay detras.
        private bool _rayOverUI;

        private void Awake()
        {
            if (registry == null) registry = GetComponent<PasteurizerPartsRegistry>();
            RefreshCamera();
        }

        /// Permite disparar el pin desde un evento externo, por ejemplo desde un
        /// ControllerButtonPressInteraction de VIROO cableado en el inspector.
        public void NotifyExternalClick()
        {
            _externalClickQueued = true;
        }

        /// Busca el interactor de mano del rig de VIROO y lo adopta como origen del ray.
        /// Se usa curveOrigin para que el ray coincida exactamente con la linea que el
        /// estudiante ve dibujada por el interactor.
        private void TryBindHandInteractor()
        {
            _nextBindAttempt = Time.unscaledTime + BindRetrySeconds;

            var interactors = FindObjectsByType<NearFarInteractor>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (interactors == null || interactors.Length == 0) return;

            NearFarInteractor chosen = null;
            foreach (var it in interactors)
            {
                if (it == null) continue;
                if (it.handedness == preferredHand) { chosen = it; break; }
                if (chosen == null) chosen = it;   // la otra mano como respaldo
            }
            if (chosen == null) return;

            // Solo re-suscribir si cambio el interactor: TryBind puede reintentarse
            // mientras curveOrigin siga sin resolverse, y no queremos listeners dobles.
            if (_handInteractor != chosen)
            {
                if (_handInteractor != null)
                {
                    _handInteractor.uiHoverEntered.RemoveListener(OnUIHoverEntered);
                    _handInteractor.uiHoverExited.RemoveListener(OnUIHoverExited);
                }
                _handInteractor = chosen;
                chosen.uiHoverEntered.AddListener(OnUIHoverEntered);
                chosen.uiHoverExited.AddListener(OnUIHoverExited);
            }

            if (chosen.curveOrigin != null) raySource = chosen.curveOrigin;
        }

        private void OnUIHoverEntered(UIHoverEventArgs args) => _rayOverUI = true;
        private void OnUIHoverExited(UIHoverEventArgs args) => _rayOverUI = false;

        private void OnDisable()
        {
            if (_handInteractor != null)
            {
                _handInteractor.uiHoverEntered.RemoveListener(OnUIHoverEntered);
                _handInteractor.uiHoverExited.RemoveListener(OnUIHoverExited);
            }
            _handInteractor = null;
            _rayOverUI = false;
        }

        private void RefreshCamera()
        {
            _mainCam = Camera.main;
            if (_mainCam == null)
            {
                // Fallback: cualquier camara activa en la escena (XR Rig sin MainCamera tag)
                var all = Camera.allCameras;
                if (all != null && all.Length > 0) _mainCam = all[0];
            }
        }

        private void Update()
        {
            if (registry == null || registry.Database == null) return;

            // Re-adquirir camara si se perdio (XR a veces la crea/destruye dinamicamente)
            if (_mainCam == null) RefreshCamera();

            // El rig de VIROO aparece despues del Awake de la escena.
            if (autoBindVirooController && (_handInteractor == null || raySource == null)
                && Time.unscaledTime >= _nextBindAttempt)
                TryBindHandInteractor();

            // Si el rayo esta sobre un panel de UI no se toca el pasteurizador:
            // la UI no tiene colliders, el raycast fisico la atravesaria y golpearia
            // la maquina que hay detras (era lo que hacia saltar la tarjeta al pulsar
            // ANTERIOR/SIGUIENTE del carrusel).
            GameObject hit = _rayOverUI ? null : TryRaycast();
            SetHover(hit);

            // Se consulta siempre para consumir el click y que no quede pendiente,
            // pero se ignora mientras el rayo este sobre la UI.
            bool clicked = IsClickPressed();
            if (_rayOverUI) clicked = false;

            if (clicked)
            {
                if (hit != null) SetPinned(hit);
                else SetPinned(null);
            }

            // Atajos: ESC cierra el pin, F hace focus sobre el pineado
            if (IsKeyPressed(clearPinKey))
                SetPinned(null);
            if (IsKeyPressed(focusKey) && _pinned != null)
                FocusCameraOn(_pinned);
        }

        private bool IsKeyPressed(KeyCode kc)
        {
            if (kc == KeyCode.None) return false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            var key = KeyCodeToKey(kc);
            if (key == Key.None) return false;
            return Keyboard.current[key].wasPressedThisFrame;
#else
            return Input.GetKeyDown(kc);
#endif
        }

        /// Mueve la camara para centrarse sobre el GO a una distancia comoda.
        /// Solo usable en modo desktop / debug (en VR no querras teleport asi).
        private void FocusCameraOn(GameObject go)
        {
            if (_mainCam == null) RefreshCamera();
            if (_mainCam == null) return;

            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

            float radius = Mathf.Max(b.size.x, b.size.y, b.size.z) * 0.5f + 0.1f;
            float dist = Mathf.Max(radius / Mathf.Tan(_mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad),
                                   focusDistance);
            _mainCam.transform.position = b.center - _mainCam.transform.forward * dist;
            _mainCam.transform.LookAt(b.center);
        }

        private bool IsClickPressed()
        {
            // 1) Gatillo del mando (rig de VIROO). Es input de XRI, no de red:
            //    el pin queda local para el estudiante que lo dispara.
            if (_externalClickQueued)
            {
                _externalClickQueued = false;
                return true;
            }
            if (_handInteractor != null && _handInteractor.selectInput != null
                && _handInteractor.selectInput.ReadWasPerformedThisFrame())
                return true;

#if ENABLE_INPUT_SYSTEM
            // Input System nuevo (Viroo/XR usa este)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (pinKey != KeyCode.None && Keyboard.current != null)
            {
                var key = KeyCodeToKey(pinKey);
                if (key != Key.None && Keyboard.current[key].wasPressedThisFrame)
                    return true;
            }
            return false;
#else
            bool m = Input.GetMouseButtonDown(0);
            bool k = pinKey != KeyCode.None && Input.GetKeyDown(pinKey);
            return m || k;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static Key KeyCodeToKey(KeyCode kc)
        {
            // Mapeo minimo (extender si haces falta). Devuelve Key.None si no se conoce.
            switch (kc)
            {
                case KeyCode.Space:   return Key.Space;
                case KeyCode.Return:  return Key.Enter;
                case KeyCode.Escape:  return Key.Escape;
                case KeyCode.Tab:     return Key.Tab;
                case KeyCode.E:       return Key.E;
                case KeyCode.F:       return Key.F;
                case KeyCode.G:       return Key.G;
                default:              return Key.None;
            }
        }
#endif

        private Vector2 GetMousePosition(out bool valid)
        {
            valid = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return Vector2.zero;
            var p = Mouse.current.position.ReadValue();
            valid = p.x >= 0 && p.x < Screen.width && p.y >= 0 && p.y < Screen.height;
            return p;
#else
            var mp = Input.mousePosition;
            valid = mp.x >= 0 && mp.x < Screen.width && mp.y >= 0 && mp.y < Screen.height;
            return new Vector2(mp.x, mp.y);
#endif
        }

        // Buffer reutilizable: evita generar basura por frame con RaycastAll.
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        private GameObject TryRaycast()
        {
            // 1) VR ray source
            if (raySource != null)
            {
                var go = FirstValidPartAlong(raySource.position, raySource.forward, rayMaxDistance);
                if (go != null) return go;
            }
            // 2) Mouse fallback. Si hay un interactor de mano activo estamos en VR:
            //    el raton solo generaria hovers fantasma y un raycast extra por frame.
            if (alsoUseMouse && _handInteractor == null && _mainCam != null)
            {
                // Mismo motivo que en VR: no atravesar la UI en modo escritorio.
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null && es.IsPointerOverGameObject()) return null;

                var mp = GetMousePosition(out bool valid);
                if (valid)
                {
                    var ray = _mainCam.ScreenPointToRay(mp);
                    var go = FirstValidPartAlong(ray.origin, ray.direction, _mainCam.farClipPlane);
                    if (go != null) return go;
                }
            }
            return null;
        }

        /// Devuelve la primera PIEZA VALIDA a lo largo del rayo, atravesando colliders
        /// que no son partes del pasteurizador.
        ///
        /// Antes se usaba Physics.Raycast, que devuelve solo el impacto mas cercano: si
        /// delante habia un collider ajeno (los muros invisibles de _CollisionWalls, por
        /// ejemplo) la funcion se rendia y devolvia null, o el hover se quedaba pegado a
        /// las pocas piezas que no estaban tapadas. Por eso siempre acababa mostrandose
        /// la misma descripcion.
        private GameObject FirstValidPartAlong(Vector3 origin, Vector3 direction, float maxDistance)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, maxDistance, hitLayers);
            if (count <= 0) return null;

            // RaycastNonAlloc no garantiza orden. Se ordena por distancia (insercion:
            // como mucho hay 16 elementos) para poder recorrer los impactos de mas
            // cercano a mas lejano y respetar lo que tape a lo que.
            for (int i = 1; i < count; i++)
            {
                var actual = _hits[i];
                int j = i - 1;
                while (j >= 0 && _hits[j].distance > actual.distance)
                {
                    _hits[j + 1] = _hits[j];
                    j--;
                }
                _hits[j + 1] = actual;
            }

            for (int i = 0; i < count; i++)
            {
                var col = _hits[i].collider;
                if (col == null) continue;
                var go = col.gameObject;

                // Un bloqueador (panel de UI) interrumpe la busqueda: lo que haya
                // detras no es alcanzable. Si su panel esta oculto no bloquea nada.
                var blocker = go.GetComponent<PasteurizerRayBlocker>();
                if (blocker != null)
                {
                    if (blocker.Blocks) return null;
                    continue;
                }

                // Lo que se puede atravesar sin mas: triggers y los muros invisibles
                // que solo existen para frenar al jugador al caminar.
                if (col.isTrigger || EsMuroDeColision(go.transform)) continue;

                if (IsValidPart(go)) return go;

                // Cualquier otro solido TAPA lo que hay detras. Es la oclusion normal:
                // sin esto el rayo atravesaba paredes y mesas y acababa señalando el
                // pasteurizador de la sala contigua, mostrando siempre la misma ficha.
                return null;
            }
            return null;
        }

        /// Los muros de "_CollisionWalls" existen para que el jugador no atraviese la
        /// maquina caminando, no para tapar la vista. Ademas hoy estan mal orientados y
        /// cruzan el propio modelo, asi que taparian las piezas que deben señalarse.
        private static bool EsMuroDeColision(Transform t)
        {
            var parent = t.parent;
            return parent != null && parent.name == "_CollisionWalls";
        }

        private bool IsValidPart(GameObject go)
        {
            return go != null && registry.ByName.ContainsKey(go.name);
        }

        private void SetHover(GameObject go)
        {
            if (_hovered == go) return;
            if (_hovered != null && _hovered != _pinned)
            {
                RestoreMaterials(_hovered);
                OnHoverExit?.Invoke(BuildHit(_hovered));
            }
            _hovered = go;
            if (_hovered != null && _hovered != _pinned)
            {
                ApplyTint(_hovered, hoverColor);
                OnHoverEnter?.Invoke(BuildHit(_hovered));
            }
        }

        private void SetPinned(GameObject go)
        {
            if (_pinned == go) return;
            if (_pinned != null && _pinned != _hovered) RestoreMaterials(_pinned);
            _pinned = go;
            if (_pinned != null)
            {
                ApplyTint(_pinned, pinColor);
                OnPartPinned?.Invoke(BuildHit(_pinned));
            }
            else
            {
                OnPinCleared?.Invoke();
            }
        }

        public void ClearPin() => SetPinned(null);

        private PartHitInfo BuildHit(GameObject go)
        {
            var nm = go.name;
            var key = registry.ResolveSubsystemKey(nm);
            var info = registry.Database.GetByKey(key);
            var specific = registry.Database.GetSpecificDescription(nm);
            return new PartHitInfo
            {
                part = go,
                partName = nm,
                subsystemKey = key,
                subsystem = info,
                description = specific ?? info?.description
            };
        }

        private void ApplyTint(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            if (!_originalMats.ContainsKey(r)) _originalMats[r] = r.sharedMaterials;
            var newMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                var src = r.sharedMaterials[i];
                var m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
                if (m.HasProperty("_Color"))     m.SetColor("_Color", color);
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", color * emissiveIntensity);
                }
                newMats[i] = m;
            }
            r.materials = newMats;
        }

        private void RestoreMaterials(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && _originalMats.TryGetValue(r, out var mats))
            {
                r.sharedMaterials = mats;
                _originalMats.Remove(r);
            }
        }
    }
}
