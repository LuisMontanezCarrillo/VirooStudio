using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Trigger de click")]
        [Tooltip("KeyCode adicional para pin (solo si el Input Legacy esta activo).")]
        public KeyCode pinKey = KeyCode.None;

        [Header("Visuals")]
        public Color hoverColor = new Color(1.0f, 0.55f, 0.0f);
        public Color pinColor = new Color(1.0f, 0.90f, 0.0f);
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

        private void Awake()
        {
            if (registry == null) registry = GetComponent<PasteurizerPartsRegistry>();
            RefreshCamera();
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

            GameObject hit = TryRaycast();
            SetHover(hit);

            if (IsClickPressed())
            {
                if (hit != null) SetPinned(hit);
                else SetPinned(null);
            }
        }

        private bool IsClickPressed()
        {
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

        private GameObject TryRaycast()
        {
            // 1) VR ray source
            if (raySource != null)
            {
                if (Physics.Raycast(raySource.position, raySource.forward,
                                    out var rh, rayMaxDistance, hitLayers))
                {
                    if (IsValidPart(rh.collider.gameObject)) return rh.collider.gameObject;
                }
            }
            // 2) Mouse fallback
            if (alsoUseMouse && _mainCam != null)
            {
                var mp = GetMousePosition(out bool valid);
                if (valid)
                {
                    var ray = _mainCam.ScreenPointToRay(mp);
                    if (Physics.Raycast(ray, out var rh, _mainCam.farClipPlane, hitLayers))
                        if (IsValidPart(rh.collider.gameObject)) return rh.collider.gameObject;
                }
            }
            return null;
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
