《春秋问鼎》WorldConfig V2（24 Region + 21 城 + 68 村完整初始配置表）

    

    
    
    一、设计目标

    WorldConfig 是《春秋问鼎》世界的静态初始化数据。

    作用：

    定义天下初始版图；

    定义各势力的领土范围；

    定义 Region 战略关系；

    定义城、村基础属性；

    作为新游戏创建的基础模板。

    游戏运行后，所有变化进入 World State，不修改 WorldConfig。

    

    
    
    二、世界规模

    
    2.1 势力数量

    势力ID
名称
类型
初始 Region
ZHOU
周王室
Kingdom
1
QIN
秦
Kingdom
5
JIN
晋
Kingdom
5
QI
齐
Kingdom
5
CHU
楚
Kingdom
5
PLAYER
玩家流民
Player
1
NEUTRAL
中立势力
Neutral
2

    合计：

    7 个势力；

    24 个 Region。

    

    
    
    2.2 Settlement 数量

    类型
数量
城市
21
村庄
68
合计
89

    其中：

    国家 Region：21 个（21 城 + 63 村）；

    玩家 Region：1 个（1 流民村）；

    中立 Region：2 个（4 村）。

    

    
    
    
    三、Region 战略配置

    
    3.1 周王室

    Region ID
名称
核心城
附属村庄
邻接 Region
ZHOU_R01
洛邑地区
CITY_ZHOU_001
V_ZHOU_001~003
QIN_R02、JIN_R03、QI_R01

    

    
    
    3.2 秦国

    Region ID
名称
核心城
附属村庄
邻接 Region
QIN_R01
咸阳地区
CITY_QIN_001
V_QIN_001~003
QIN_R02
QIN_R02
雍城地区
CITY_QIN_002
V_QIN_004~006
QIN_R01、QIN_R03、ZHOU_R01、JIN_R01、QIN_R05
QIN_R03
陇西地区
CITY_QIN_003
V_QIN_007~009
QIN_R02、QIN_R04
QIN_R04
西戎边境
CITY_QIN_004
V_QIN_010~012
QIN_R03、NEU_R01
QIN_R05
函谷关地区
CITY_QIN_005
V_QIN_013~015
QIN_R02、JIN_R01、ZHOU_R01

    

    
    
    3.3 晋国

    Region ID
名称
核心城
附属村庄
邻接 Region
JIN_R01
河西地区
CITY_JIN_001
V_JIN_001~003
QIN_R02、QIN_R05、JIN_R02
JIN_R02
晋阳地区
CITY_JIN_002
V_JIN_004~006
JIN_R01、JIN_R03、JIN_R05
JIN_R03
绛都地区
CITY_JIN_003
V_JIN_007~009
JIN_R02、ZHOU_R01、QI_R01、JIN_R04
JIN_R04
太行地区
CITY_JIN_004
V_JIN_010~012
JIN_R03、QI_R02
JIN_R05
北狄边境
CITY_JIN_005
V_JIN_013~015
JIN_R02、NEU_R02

    

    
    
    3.4 齐国

    Region ID
名称
核心城
附属村庄
邻接 Region
QI_R01
临淄地区
CITY_QI_001
V_QI_001~003
ZHOU_R01、JIN_R03、QI_R02、QI_R03
QI_R02
泰山地区
CITY_QI_002
V_QI_004~006
QI_R01、JIN_R04、CHU_R01
QI_R03
胶东地区
CITY_QI_003
V_QI_007~009
QI_R01、QI_R04
QI_R04
海滨地区
CITY_QI_004
V_QI_010~012
QI_R03、QI_R05
QI_R05
东夷边境
CITY_QI_005
V_QI_013~015
QI_R04、NEU_R02

    

    
    
    3.5 楚国

    Region ID
名称
核心城
附属村庄
邻接 Region
CHU_R01
汉水北岸
CITY_CHU_001
V_CHU_001~003
QI_R02、CHU_R02
CHU_R02
郢都地区
CITY_CHU_002
V_CHU_004~006
CHU_R01、CHU_R03
CHU_R03
云梦泽地区
CITY_CHU_003
V_CHU_007~009
CHU_R02、CHU_R04
CHU_R04
荆南地区
CITY_CHU_004
V_CHU_010~012
CHU_R03、CHU_R05
CHU_R05
南蛮边境
CITY_CHU_005
V_CHU_013~015
CHU_R04、PLAYER_R01

    

    
    
    3.6 玩家流民区域

    Region ID
名称
核心城
附属村庄
邻接 Region
PLAYER_R01
南方流民区域
无
V_PLAYER_001
CHU_R05、NEU_R01

    

    
    
    3.7 中立区域

    Region ID
名称
核心城
附属村庄
邻接 Region
NEU_R01
西南荒地
无
V_NEU_001、V_NEU_002
QIN_R04、PLAYER_R01
NEU_R02
东北边地
无
V_NEU_003、V_NEU_004
JIN_R05、QI_R05

    

    
    
    
    四、Settlement 初始模板

    
    4.1 国都（5 座）

    适用：

    洛邑；

    咸阳；

    绛都；

    临淄；

    郢都。

    属性
数值
户数
500
人口
2500
土地
25000亩
粮食
500000斤
铜钱
5000
守军
100

    

    
    
    4.2 普通城市（16 座）

    属性
数值
户数
300
人口
1500
土地
15000亩
粮食
300000斤
铜钱
2000
守军
50

    

    
    
    4.3 国家普通村庄（63 座）

    属性
数值
户数
100
人口
500
土地
5000亩
粮食
100000斤
铜钱
500
守军
20

    

    
    
    4.4 玩家流民村

    属性
数值
户数
30
人口
150
土地
1500亩
粮食
50000斤
铜钱
300
守军
10
民心
90

    

    
    
    4.5 中立村庄

    属性
数值
户数
80
人口
400
土地
4000亩
粮食
80000斤
铜钱
300
守军
15

    

    
    
    
    五、特殊战略 Region

    Region ID
战略意义
ZHOU_R01
天下中心、王都
QIN_R04
秦国西部边境
JIN_R05
晋国北部边境
QI_R05
齐国东部边境
CHU_R05
楚国南部边境
NEU_R01
玩家早期扩张目标
NEU_R02
国家扩张缓冲区域

    

    
    
    六、世界战略格局

    整体战略链：

    秦 —— 晋 —— 周 —— 齐
 |                  |
西南荒地          楚
 |                  |
玩家流民 —— 楚南边境

    主要发展路线：

    玩家：

    流民村
↓
吞并西南荒地
↓
形成地方势力
↓
攻占楚国边境 Region
↓
进入诸侯争霸

    国家：

    发展经济
↓
巩固边境
↓
争夺邻接 Region
↓
逐步吞并扩张

    

    
    
    七、Unity 配置文件拆分建议

    Assets/Configs/World/

├── NationConfig.json
├── RegionConfig.json
├── SettlementTemplate.json
├── SettlementInstance.json
└── MapNeighborConfig.json

    其中：

    NationConfig：国家基础信息；

    RegionConfig：战略区域关系；

    SettlementTemplate：城村基础模板；

    SettlementInstance：89 个具体据点；

    MapNeighborConfig：地图连接关系。

    

    
    
    八、最终 WorldConfig 结构

    WorldConfig
│
├── Nation（7个势力）
│
├── Region（24个战略区域）
│
└── Settlement（89个据点）
      │
      ├── City（21座）
      │
      └── Village（68个）

    核心设计理念：

    Nation 决定天下格局；Region 决定战略版图；Settlement 决定经济、建设与战争。

    这份 WorldConfig 可以作为 Unity 项目中 JSON 配置的直接数据来源，并用于 New Game 时生成完整的 World Runtime State。