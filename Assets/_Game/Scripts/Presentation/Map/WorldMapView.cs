using System.Collections.Generic;
using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.Map
{
    public class WorldMapView : MonoBehaviour
    {
        [SerializeField] private Transform regionRoot;
        [SerializeField] private RegionView regionViewPrefab;
        [SerializeField] private NationBorderView nationBorderView;
        [SerializeField] private int layoutColumns = 6;
        [SerializeField] private Vector2 regionSpacing = new Vector2(1.15f, 0.82f);

        [Header("拓扑布局")]
        [Tooltip("配置坐标(mapX/mapY)的缩放系数，用于把布局整体收进相机视野。")]
        [SerializeField] private float layoutScale = 1.46f;

        [Header("邻接连线")]
        [SerializeField] private bool drawAdjacencyLines = true;
        [SerializeField] private Color lineColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        [SerializeField] private float lineWidth = 0.03f;
        [Tooltip("连线在 Z 轴上的偏移，正值使其位于区域块之后。")]
        [SerializeField] private float lineZOffset = 0.2f;

        private readonly Dictionary<string, RegionView> _regionViews = new Dictionary<string, RegionView>();
        private readonly List<LineRenderer> _adjacencyLines = new List<LineRenderer>();
        private GameApplication _application;
        private MapLayerController _controller;
        private Transform _lineRoot;
        private Material _lineMaterial;
        private bool _linesBuilt;

        public void Bind(GameApplication application, MapLayerController controller)
        {
            Unsubscribe();
            _application = application;
            _controller = controller;
            Subscribe();
            Refresh();
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<RegionCaptured>(OnRegionCaptured);
        }

        private void Subscribe()
        {
            _application?.Events.Subscribe<RegionCaptured>(OnRegionCaptured);
        }

        private void Unsubscribe()
        {
            _application?.Events.Unsubscribe<RegionCaptured>(OnRegionCaptured);
        }

        public void Refresh()
        {
            WorldRuntime world = _application?.World;
            if (world == null)
                return;

            ConfigDatabase config = _application?.Config;
            bool useConfigLayout = UsesConfigLayout(world, config);

            // 先计算所有区域位置（含居中偏移），再统一应用，便于连线复用同一坐标。
            var positions = new Dictionary<string, Vector3>();
            int index = 0;
            foreach (var region in world.Regions.GetAll())
            {
                positions[region.Id] = ResolveRawPosition(config, region.Id, index, useConfigLayout);
                index++;
            }

            Vector3 centerOffset = ComputeCenterOffset(positions.Values);

            foreach (var region in world.Regions.GetAll())
            {
                RegionView view = GetOrCreate(region.Id);
                view.Bind(region.Id, _controller);
                view.transform.localPosition = positions[region.Id] - centerOffset;
                view.Refresh(region);
            }

            if (drawAdjacencyLines)
                BuildAdjacencyLines(world, positions, centerOffset);

            nationBorderView?.Refresh(world);
        }

        /// <summary>任一区域配置了非零坐标即视为启用拓扑布局，否则回退到旧的序号网格。</summary>
        private static bool UsesConfigLayout(WorldRuntime world, ConfigDatabase config)
        {
            if (config == null)
                return false;
            foreach (var region in world.Regions.GetAll())
            {
                if (config.Regions.TryGetValue(region.Id, out var rc)
                    && (Mathf.Abs(rc.mapX) > Mathf.Epsilon || Mathf.Abs(rc.mapY) > Mathf.Epsilon))
                {
                    return true;
                }
            }
            return false;
        }

        private Vector3 ResolveRawPosition(ConfigDatabase config, string regionId, int index, bool useConfigLayout)
        {
            if (useConfigLayout && config != null && config.Regions.TryGetValue(regionId, out var rc))
            {
                return new Vector3(
                    rc.mapX * regionSpacing.x * layoutScale,
                    rc.mapY * regionSpacing.y * layoutScale,
                    0f);
            }
            return CalculateRegionPosition(index);
        }

        private static Vector3 ComputeCenterOffset(IEnumerable<Vector3> positions)
        {
            bool any = false;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in positions)
            {
                any = true;
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            if (!any)
                return Vector3.zero;
            return new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        }

        private Vector3 CalculateRegionPosition(int index)
        {
            int columns = Mathf.Max(1, layoutColumns);
            int row = index / columns;
            int col = index % columns;
            float x = (col - (columns - 1) * 0.5f) * regionSpacing.x;
            float y = (1.5f - row) * regionSpacing.y;
            return new Vector3(x, y, 0f);
        }

        private RegionView GetOrCreate(string regionId)
        {
            if (_regionViews.TryGetValue(regionId, out var existing))
                return existing;

            RegionView view = regionViewPrefab != null
                ? Instantiate(regionViewPrefab, regionRoot != null ? regionRoot : transform)
                : new GameObject(regionId).AddComponent<RegionView>();
            view.name = "Region_" + regionId;
            _regionViews.Add(regionId, view);
            return view;
        }

        /// <summary>按邻接关系绘制区域间连线。拓扑稳定，仅首次构建。</summary>
        private void BuildAdjacencyLines(WorldRuntime world, Dictionary<string, Vector3> positions, Vector3 centerOffset)
        {
            if (_linesBuilt)
                return;

            EnsureLineRoot();

            var drawn = new HashSet<string>();
            foreach (var region in world.Regions.GetAll())
            {
                if (!positions.TryGetValue(region.Id, out var fromPos))
                    continue;

                foreach (var neighborId in region.NeighborRegionIds)
                {
                    if (!positions.TryGetValue(neighborId, out var toPos))
                        continue;

                    // 同一条边只画一次（A|B 与 B|A 视为同一条）。
                    string key = string.CompareOrdinal(region.Id, neighborId) <= 0
                        ? region.Id + "|" + neighborId
                        : neighborId + "|" + region.Id;
                    if (!drawn.Add(key))
                        continue;

                    Vector3 a = fromPos - centerOffset;
                    Vector3 b = toPos - centerOffset;
                    a.z = lineZOffset;
                    b.z = lineZOffset;
                    CreateLine(a, b);
                }
            }

            _linesBuilt = true;
        }

        private void EnsureLineRoot()
        {
            if (_lineRoot == null)
            {
                var rootObject = new GameObject("AdjacencyLines");
                _lineRoot = rootObject.transform;
                _lineRoot.SetParent(regionRoot != null ? regionRoot : transform, false);
            }
            if (_lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                _lineMaterial = new Material(shader);
            }
        }

        private void CreateLine(Vector3 a, Vector3 b)
        {
            var lineObject = new GameObject("Edge");
            lineObject.transform.SetParent(_lineRoot, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.material = _lineMaterial;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);

            _adjacencyLines.Add(line);
        }

        private void OnRegionCaptured(RegionCaptured evt)
        {
            if (_application?.World == null)
                return;
            if (_regionViews.TryGetValue(evt.RegionId, out var view))
                view.Refresh(_application.World.Regions.Get(evt.RegionId));
        }
    }
}
