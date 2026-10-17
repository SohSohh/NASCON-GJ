using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NasconGJ.UI
{
    /// <summary>
    /// Menu item feel: on hover/select the label slides right and warms up, a crystal marker appears,
    /// an ember underline draws in. Mouse hover selects, so keyboard/gamepad and mouse share one highlight.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
    {
        public static AudioSource UISource;
        public static AudioClip HoverClip;
        public static AudioClip SelectClip;

        [SerializeField] private RectTransform label;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Graphic marker;
        [SerializeField] private RectTransform underline;
        [SerializeField] private Color normalColor = new(0.85f, 0.76f, 0.66f, 0.72f);
        [SerializeField] private Color hoverColor = new(1f, 0.78f, 0.5f, 1f);
        [SerializeField] private float shift = 18f;
        [SerializeField] private float underlineWidth = 200f;

        private bool hot;
        private float k;
        private Vector2 labelBase;

        private void Awake()
        {
            if (label != null) labelBase = label.anchoredPosition;
            GetComponent<Button>().transition = Selectable.Transition.None;
            Apply(0f);
        }

        private void OnDisable()
        {
            hot = false;
            k = 0f;
            Apply(0f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && GetComponent<Button>().interactable) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            hot = true;
            if (UISource != null && HoverClip != null) UISource.PlayOneShot(HoverClip, 0.5f);
        }

        public void OnDeselect(BaseEventData eventData) => hot = false;
        public void OnSubmit(BaseEventData eventData) => PlaySelect();
        public void OnPointerClick(PointerEventData eventData) => PlaySelect();

        private static void PlaySelect()
        {
            if (UISource != null && SelectClip != null) UISource.PlayOneShot(SelectClip, 0.7f);
        }

        private void Update()
        {
            float target = hot ? 1f : 0f;
            if (Mathf.Approximately(k, target)) return;
            k = Mathf.MoveTowards(k, target, Time.unscaledDeltaTime * 7f);
            Apply(Mathf.SmoothStep(0f, 1f, k));
        }

        private void Apply(float e)
        {
            if (label != null) label.anchoredPosition = labelBase + new Vector2(shift * e, 0f);
            if (labelText != null) labelText.color = Color.Lerp(normalColor, hoverColor, e);
            if (marker != null)
            {
                var c = marker.color;
                c.a = e;
                marker.color = c;
                marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f + (1f - e) * 90f);
                marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, e);
            }
            if (underline != null) underline.sizeDelta = new Vector2(underlineWidth * e, underline.sizeDelta.y);
        }
    }
}
