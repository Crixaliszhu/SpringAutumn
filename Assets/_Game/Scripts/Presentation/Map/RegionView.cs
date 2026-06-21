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
        private Vector3 _baseScale;
        private Color _baseColor = Color.gray;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Bind(string regionId, MapLayerController controller)
        {
            RegionId = regionId;
        }

        public void Refresh(RegionState state)
        {
            RegionId = state.Id;
            _baseColor = NationColorPalette.Get(state.OwnerId);
            ApplyColor(_baseColor);
            if (label != null)
                label.text = FormatLabel(state.Id);
        }

        private static string FormatLabel(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return string.Empty;

            string prefix = regionId;
            string suffix = string.Empty;
            int split = regionId.LastIndexOf("_R", System.StringComparison.Ordinal);
            if (split >= 0)
            {
                prefix = regionId.Substring(0, split);
                suffix = regionId.Substring(split + 2);
            }

            switch (prefix)
            {
                case "QIN": prefix = "秦"; break;
                case "JIN": prefix = "晋"; break;
                case "QI": prefix = "齐"; break;
                case "CHU": prefix = "楚"; break;
                case "ZHOU": prefix = "周"; break;
                case "PLAYER": prefix = "流"; break;
                case "NEU": prefix = "中"; break;
            }

            return prefix + suffix;
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
            ApplyColor(Color.Lerp(_baseColor, Color.white, 0.35f));
        }

        public void OnDeselected()
        {
            transform.localScale = _baseScale;
            ApplyColor(_baseColor);
        }
    }
}
