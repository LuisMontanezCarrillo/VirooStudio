using UnityEngine;
using UnityEngine.UI;

namespace ViroLab.Pasteurizador
{
    /// Convierte un Canvas a World Space y lo posiciona/orienta cada frame
    /// relativo a la cámara del jugador (típico VR).
    ///
    /// Configuración recomendada VR:
    ///   - distance      = 1.5  (m, cómodo para leer)
    ///   - verticalOffset= -0.15 (ligeramente bajo la mirada)
    ///   - worldScale    = 0.001 (compensa el sizeDelta en píxeles del Canvas)
    ///   - followMode    = FaceCameraSmoothed (no marea: lerp lento)
    ///
    /// Si querés que aparezca SOLO cuando hay pinned part, asigná `hover`
    /// y poné `showOnlyWhenPinned = true`.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class PasteurizerWorldCanvas : MonoBehaviour
    {
        public enum FollowMode
        {
            Static,              // Una vez posicionado, queda quieto
            FaceCameraRigid,     // Sigue/orienta a la cámara cada frame (puede marear)
            FaceCameraSmoothed,  // Sigue/orienta con lerp suave (recomendado)
            AnchorToTransform,   // Pegado a un Transform (ej. controller izquierdo)
            HUDAnchor,           // HUD estilo videojuego: anclado a un punto del viewport
        }

        [Header("Modo")]
        public FollowMode followMode = FollowMode.FaceCameraSmoothed;

        [Header("Posición frente a la cámara")]
        [Tooltip("Distancia (m) frente a la cámara.")]
        public float distance = 1.5f;
        [Tooltip("Offset vertical (m) — negativo = abajo de la mirada.")]
        public float verticalOffset = -0.15f;
        [Tooltip("Offset lateral (m) — negativo = izquierda.")]
        public float horizontalOffset = 0f;

        [Header("Escala del canvas en World Space")]
        [Tooltip("WorldScale del Canvas. 0.001 convierte 1px=1mm.")]
        public float worldScale = 0.001f;

        [Header("Suavizado (FaceCameraSmoothed)")]
        [Range(0.5f, 20f)] public float positionLerp = 6f;
        [Range(0.5f, 20f)] public float rotationLerp = 6f;

        [Header("Anchor mode")]
        [Tooltip("Solo si followMode = AnchorToTransform")]
        public Transform anchor;
        public Vector3 anchorLocalOffset = new Vector3(0.1f, 0.05f, 0.15f);
        public Vector3 anchorLocalEuler = new Vector3(-30f, 30f, 0f);

        [Header("HUD Anchor (viewport-relative)")]
        [Tooltip("Posición en viewport. (0,0)=abajo-izq, (1,1)=arriba-der, (0.5,0.5)=centro.")]
        public Vector2 viewportAnchor = new Vector2(0.18f, 0.82f);
        [Tooltip("Distancia (m) al plano del HUD desde la cámara.")]
        public float hudDistance = 0.8f;
        [Tooltip("Si true, el HUD se mueve con lerp; si false, queda pegado rígido a la cabeza.")]
        public bool hudSmoothing = false;
        [Range(1f, 30f)] public float hudLerp = 12f;

        [Header("Visibilidad condicional")]
        public PasteurizerHoverHandler hover;
        [Tooltip("Si true, oculta el canvas hasta que se pinee una parte.")]
        public bool showOnlyWhenPinned = false;

        [Header("Fade animado")]
        [Tooltip("Velocidad del fade in/out del alpha (por segundo). 0 = sin animación.")]
        [Range(0f, 20f)] public float fadeSpeed = 8f;

        [Header("Debug")]
        public bool logSetupOnAwake = true;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Camera _cam;
        private bool _hasPinned;
        private float _targetAlpha = 0f;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            // Asegurar CanvasGroup en el propio canvas para poder ocultar
            // por alpha sin desactivar GameObjects (eso rompía la suscripción
            // a eventos de la card al inicio).
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            ApplyCanvasMode();
            RefreshCamera();

            // AUTO-CONECTAR hover si quedó null (típico al ejecutar el menú
            // antes de instanciar el pasteurizador, o si se modificó la escena).
            if (hover == null)
            {
                hover = FindFirstObjectByType<PasteurizerHoverHandler>();
                if (hover != null)
                    Debug.Log("<color=cyan>[PasteurizerWorldCanvas]</color> Auto-conectado a HoverHandler en escena.");
            }

            // AUTO-CONECTAR la card hija si su hover está vacío
            var card = GetComponentInChildren<PasteurizerDescriptionCard>(true);
            if (card != null && card.hover == null && hover != null)
            {
                card.hover = hover;
                Debug.Log("<color=cyan>[PasteurizerWorldCanvas]</color> Auto-conectada DescriptionCard al hover.");
            }

            if (logSetupOnAwake)
                Debug.Log($"<color=cyan>[PasteurizerWorldCanvas]</color> mode={followMode}, " +
                          $"distance={distance}, scale={worldScale}, " +
                          $"camFound={_cam != null}, hoverFound={hover != null}, cardFound={card != null}");
        }

        private void OnEnable()
        {
            if (hover != null)
            {
                hover.OnPartPinned.AddListener(OnPin);
                hover.OnPinCleared.AddListener(OnPinCleared);
            }
            if (showOnlyWhenPinned) SetVisible(_hasPinned);
        }

        private void OnDisable()
        {
            if (hover != null)
            {
                hover.OnPartPinned.RemoveListener(OnPin);
                hover.OnPinCleared.RemoveListener(OnPinCleared);
            }
        }

        private void OnPin(PasteurizerHoverHandler.PartHitInfo _)
        {
            _hasPinned = true;
            if (showOnlyWhenPinned) SetVisible(true);
        }

        private void OnPinCleared()
        {
            _hasPinned = false;
            if (showOnlyWhenPinned) SetVisible(false);
        }

        /// Hace visible/invisible el canvas SIN desactivar GameObjects
        /// (para no romper la suscripción a eventos de hijos como la card).
        /// Si fadeSpeed > 0, el alpha se anima en Update; si = 0, snap.
        private void SetVisible(bool on)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _targetAlpha = on ? 1f : 0f;
                _canvasGroup.blocksRaycasts = on;
                _canvasGroup.interactable = on;
                // Si no hay fade, snap inmediato (compat con código viejo)
                if (fadeSpeed <= 0f) _canvasGroup.alpha = _targetAlpha;
            }
            // Asegurar que el Canvas mismo siga habilitado
            if (_canvas != null) _canvas.enabled = true;
        }

        private void Update()
        {
            // Lerp del alpha hacia el target (fade in/out animado)
            if (_canvasGroup != null && fadeSpeed > 0f
                && !Mathf.Approximately(_canvasGroup.alpha, _targetAlpha))
            {
                _canvasGroup.alpha = Mathf.MoveTowards(
                    _canvasGroup.alpha, _targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            }
        }

        private void ApplyCanvasMode()
        {
            if (_canvas == null) return;
            _canvas.renderMode = RenderMode.WorldSpace;
            // Para que el ScreenPointToRay y eventuales clicks no se rompan
            if (_canvas.worldCamera == null)
            {
                if (_cam == null) RefreshCamera();
                if (_cam != null) _canvas.worldCamera = _cam;
            }
            transform.localScale = Vector3.one * worldScale;
        }

        private void RefreshCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var all = Camera.allCameras;
                if (all != null && all.Length > 0) _cam = all[0];
            }
        }

        private void LateUpdate()
        {
            if (_cam == null) RefreshCamera();

            switch (followMode)
            {
                case FollowMode.Static:
                    return;

                case FollowMode.AnchorToTransform:
                    if (anchor == null) return;
                    transform.position = anchor.TransformPoint(anchorLocalOffset);
                    transform.rotation = anchor.rotation * Quaternion.Euler(anchorLocalEuler);
                    return;

                case FollowMode.FaceCameraRigid:
                    if (_cam == null) return;
                    PlaceTargetInstant();
                    return;

                case FollowMode.FaceCameraSmoothed:
                    if (_cam == null) return;
                    PlaceTargetSmoothed();
                    return;

                case FollowMode.HUDAnchor:
                    if (_cam == null) return;
                    PlaceHUDAnchor();
                    return;
            }
        }

        private void PlaceHUDAnchor()
        {
            var camT = _cam.transform;
            // viewportAnchor: x e y normalizados 0..1; z = distancia en metros
            var vp = new Vector3(viewportAnchor.x, viewportAnchor.y, hudDistance);
            var targetPos = _cam.ViewportToWorldPoint(vp);

            // Orientación: PARALELO al plano de la cámara (HUD plano clásico).
            // Antes usaba LookRotation(targetPos - camT.position) que producía
            // perspectiva inclinada cuando el HUD estaba en esquinas.
            // Ahora el canvas es perpendicular al forward de la cámara →
            // siempre se ve frontal, sin deformación.
            var targetRot = Quaternion.LookRotation(camT.forward, camT.up);

            if (hudSmoothing)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * hudLerp);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * hudLerp);
            }
            else
            {
                transform.SetPositionAndRotation(targetPos, targetRot);
            }
        }

        private void PlaceTargetInstant()
        {
            var (targetPos, targetRot) = ComputeTarget();
            transform.SetPositionAndRotation(targetPos, targetRot);
        }

        private void PlaceTargetSmoothed()
        {
            var (targetPos, targetRot) = ComputeTarget();
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionLerp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerp);
        }

        private (Vector3 pos, Quaternion rot) ComputeTarget()
        {
            var camT = _cam.transform;
            // Flatten en plano horizontal para no inclinarse cuando bajás la cabeza
            var fwd = camT.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            var right = Vector3.Cross(Vector3.up, fwd);

            var pos = camT.position
                      + fwd * distance
                      + Vector3.up * verticalOffset
                      + right * horizontalOffset;
            var rot = Quaternion.LookRotation(pos - camT.position, Vector3.up);
            return (pos, rot);
        }
    }
}
