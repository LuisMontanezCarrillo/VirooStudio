using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViroLab.Pasteurizador
{
    // Hover + Click sobre partes del pasteurizador.
    // Soporta dos modos simultaneos:
    //   - Mouse: raycast desde la camara principal en cada frame
    //   - VR: arrastras un GameObject (controlador o cabeza) en RaySource y se usa su forward
    // Aplica un material highlight como overlay y dispara eventos.
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
        [Tooltip("KeyCode adicional para pin (ademas de left-click del mouse).")]
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
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (registry == null || registry.Database == null) return;

            GameObject hit = TryRaycast();
            SetHover(hit);

            bool clicked = Input.GetMouseButtonDown(0)
                           || (pinKey != KeyCode.None && Input.GetKeyDown(pinKey));
            if (clicked)
            {
                if (hit != null) SetPinned(hit);
                else SetPinned(null);
            }
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
                var mp = Input.mousePosition;
                if (mp.x >= 0 && mp.x < Screen.width && mp.y >= 0 && mp.y < Screen.height)
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
