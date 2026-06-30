using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViroLab.Pasteurizador
{
    // Controlador de la UI "tarjeta" que muestra tag + nombre + descripcion al hacer click.
    // Engancha los eventos de PasteurizerHoverHandler.
    public class PasteurizerDescriptionCard : MonoBehaviour
    {
        [Header("Refs")]
        public PasteurizerHoverHandler hover;
        public CanvasGroup canvasGroup;
        public Image borderImage;
        public Image tagBackground;
        public TMP_Text tagLabel;
        public TMP_Text titleLabel;
        public TMP_Text partNameLabel;
        public TMP_Text descriptionLabel;
        public Button closeButton;

        [Header("Behavior")]
        public bool showOnHover = false;
        public bool showOnPin = true;
        [Range(0f, 1f)] public float fadeAlpha = 0f;

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = fadeAlpha;
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClick);
        }

        private void OnEnable()
        {
            if (hover == null) return;
            hover.OnPartPinned.AddListener(HandlePin);
            hover.OnPinCleared.AddListener(Hide);
            if (showOnHover) hover.OnHoverEnter.AddListener(HandleHover);
        }

        private void OnDisable()
        {
            if (hover == null) return;
            hover.OnPartPinned.RemoveListener(HandlePin);
            hover.OnPinCleared.RemoveListener(Hide);
            if (showOnHover) hover.OnHoverEnter.RemoveListener(HandleHover);
        }

        private void HandleHover(PasteurizerHoverHandler.PartHitInfo info)
        {
            if (!showOnHover) return;
            Populate(info);
        }

        private void HandlePin(PasteurizerHoverHandler.PartHitInfo info)
        {
            if (!showOnPin) return;
            Populate(info);
        }

        private void Populate(PasteurizerHoverHandler.PartHitInfo info)
        {
            if (info?.subsystem == null)
            {
                Hide();
                return;
            }
            var color = info.subsystem.GetColor();
            if (borderImage != null) borderImage.color = color;
            if (tagBackground != null) tagBackground.color = color;
            if (tagLabel != null) tagLabel.text = string.IsNullOrEmpty(info.subsystem.tag) ? "?" : info.subsystem.tag;
            if (titleLabel != null) titleLabel.text = info.subsystem.title ?? info.partName;
            // El nombre de malla (p. ej. T_RAW_FBX_..., Gabinete_Manilla) no aporta al estudiante: ocultar siempre.
            if (partNameLabel != null) partNameLabel.gameObject.SetActive(false);
            if (descriptionLabel != null) descriptionLabel.text = info.description ?? info.subsystem.description ?? "";
            Show();
        }

        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            else gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = fadeAlpha;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            else gameObject.SetActive(false);
        }

        private void OnCloseClick()
        {
            if (hover != null) hover.ClearPin();
            else Hide();
        }
    }
}
