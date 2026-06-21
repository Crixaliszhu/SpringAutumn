using UnityEngine;
using SpringAutumn.Runtime;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.Map
{
    public class SettlementView : MonoBehaviour, ISelectable
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private TextMesh label;

        public string SettlementId { get; private set; }
        public string Id => SettlementId;
        public SelectionType Type { get; private set; } = SelectionType.Village;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Refresh(SettlementState state)
        {
            SettlementId = state.Id;
            Type = state.IsCity ? SelectionType.City : SelectionType.Village;
            gameObject.name = "Settlement_" + state.Id;
            if (label != null)
                label.text = state.Id;

            Renderer rendererToUse = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (rendererToUse != null)
                rendererToUse.material.color = NationColorPalette.Get(state.OwnerId);
        }

        public void OnSelected()
        {
            transform.localScale = _baseScale * 1.12f;
        }

        public void OnDeselected()
        {
            transform.localScale = _baseScale;
        }
    }
}
