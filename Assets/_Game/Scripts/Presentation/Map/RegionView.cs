using UnityEngine;
using SpringAutumn.Runtime;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.Map
{
    public class RegionView : MonoBehaviour, ISelectable
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private TextMesh label;

        public string RegionId { get; private set; }
        public string Id => RegionId;
        public SelectionType Type => SelectionType.Region;
        private MapLayerController _controller;
        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Bind(string regionId, MapLayerController controller)
        {
            RegionId = regionId;
            _controller = controller;
        }

        public void Refresh(RegionState state)
        {
            RegionId = state.Id;
            ApplyColor(NationColorPalette.Get(state.OwnerId));
            if (label != null)
                label.text = state.Id;
        }

        private void OnMouseUpAsButton()
        {
            _controller?.EnterRegion(RegionId);
        }

        private void ApplyColor(Color color)
        {
            Renderer rendererToUse = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (rendererToUse != null)
                rendererToUse.material.color = color;
        }

        public void OnSelected()
        {
            transform.localScale = _baseScale * 1.05f;
        }

        public void OnDeselected()
        {
            transform.localScale = _baseScale;
        }
    }
}
