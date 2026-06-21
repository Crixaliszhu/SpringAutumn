using UnityEngine;
using SpringAutumn.Runtime;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.Map
{
    public class ArmyView : MonoBehaviour, ISelectable
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private TextMesh label;

        public string ArmyId { get; private set; }
        public string Id => ArmyId;
        public SelectionType Type => SelectionType.Army;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Refresh(ArmyState state)
        {
            ArmyId = state.Id;
            gameObject.name = "Army_" + state.Id;
            if (label != null)
                label.text = state.NationId + " " + state.Soldiers;

            Renderer rendererToUse = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (rendererToUse != null)
                rendererToUse.material.color = NationColorPalette.Get(state.NationId);
        }

        public void OnSelected()
        {
            transform.localScale = _baseScale * 1.15f;
        }

        public void OnDeselected()
        {
            transform.localScale = _baseScale;
        }
    }
}
