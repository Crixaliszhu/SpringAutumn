一、地图层级重新定义
不再是一张地图显示所有细节。
改为：
World Map
    |
Region Map

二、World Map 视图结构
WorldMapView
|
├── RegionView
|
├── NationBorderView
|
├── RegionLabel
|
└── WorldCamera

World Map 只显示
Region Mesh； 
势力颜色； 
Region 名称； 
战争标记； 
国家边界。 
不显示：
城邑
村庄
建筑

三、Region Map 视图结构
RegionMapView
|
├── TerrainView
|
├── CityView
|
├── VillageView
|
├── ArmyView
|
├── RoadView（V2）
|
└── RegionCamera

四、数据关系
WorldRuntime
      |
      |
 RegionState
      |
      |
 SettlementState
对应：
WorldMapView
       |
  RegionView

RegionMapView
       |
 +-----+------+
 |            |
CityView  VillageView

五、地图切换
进入区域：
WorldMap

点击 Region

Load RegionMap
返回：
RegionMap

点击返回

Load WorldMap

六、Camera 分离设计
World Camera：
负责：
查看天下； 
缩放； 
拖动。 
Region Camera：
负责：
查看城村； 
观察军队； 
战斗。 
两套摄像机独立。

七、未来扩展
Region Map 可扩展：
山脉； 
河流； 
道路； 
天气； 
季节； 
城池等级模型。 

八、最终架构
                 Main Menu
                      |
                      |
                World Map
                      |
             Region Selection
                      |
                Region Map
                      |
        +-------------+-------------+
        |             |             |
      City          Village       Army
        |
    Command
        |
    GameEngine
        |
    EventBus
        |
      UI更新