一、设计目标
Game Engine Bootstrap 负责解决一个核心问题：
当玩家点击“开始游戏”或“读取存档”后，Unity 如何一步一步创建一个可运行的天下世界。
它是连接：
Unity 场景生命周期； 
Config 配置系统； 
WorldRuntime； 
GameEngine； 
SaveSystem； 
UI 系统。 
的核心入口。

二、整体启动架构
完整启动链路：
Unity Scene
     |
     V
GameLauncher
     |
     +----------------+
     |                |
     V                V
New Game         Load Save
     |                |
     V                V
ConfigLoader     SaveManager
     |                |
     V                V
ConfigDatabase   SaveData
     |                |
     V                |
WorldFactory <--------+
     |
     V
WorldRuntime
     |
     V
GameEngine.Initialize()
     |
     V
SystemManager.Initialize()
     |
     V
Game Loop Start

三、核心组件职责
3.1 GameLauncher
GameLauncher 是整个游戏的入口。
建议作为 Unity Scene 中的常驻对象。
例如：
BootstrapScene
|
└── GameLauncher(GameObject)
职责：
启动游戏； 
创建新世界； 
读取存档； 
初始化 GameEngine； 
管理游戏状态切换。 

3.2 ConfigLoader
负责：
JSON 文件
    |
    V
ConfigDatabase
特点：
游戏启动时只加载一次； 
生命周期贯穿整个游戏； 
只读。 

3.3 WorldFactory
负责创建 WorldRuntime。
两种来源：
新游戏
WorldConfig
      |
      V
WorldFactory.CreateNewWorld()
      |
      V
WorldRuntime

读取存档
SaveData
    |
    V
WorldFactory.CreateFromSave()
    |
    V
WorldRuntime

3.4 GameEngine
GameEngine 是天下模拟核心。
管理：
游戏时间； 
Tick 驱动； 
SystemManager； 
命令队列； 
游戏暂停与恢复。 

3.5 SaveManager
负责：
保存 WorldRuntime； 
读取 SaveData； 
自动存档； 
存档版本兼容。 

四、Unity 生命周期设计
推荐使用独立启动场景：
Bootstrap Scene
        |
        V
Main Menu Scene
        |
        V
Game Scene

4.1 游戏启动
Unity 执行：
void Awake()
{
    GameLauncher.Initialize();
}

流程：
Unity Start
     |
     V
GameLauncher
     |
     V
Load Config
     |
     V
进入主菜单

4.2 玩家点击“新游戏”
流程：
Button Click
      |
      V
GameLauncher.NewGame()
      |
      V
WorldFactory.Create()
      |
      V
WorldRuntime
      |
      V
GameEngine.Initialize()
      |
      V
进入游戏场景

4.3 玩家点击“读取存档”
流程：
Button Click
      |
      V
GameLauncher.LoadGame(slot)
      |
      V
SaveManager.Load(slot)
      |
      V
WorldRuntime
      |
      V
GameEngine.Initialize()
      |
      V
进入游戏

五、GameLauncher 类设计
public class GameLauncher : MonoBehaviour
{
    public ConfigDatabase Config;

    public GameEngine Engine;

    public SaveManager SaveManager;

    public void Initialize()
    {
        Config = ConfigLoader.Load();
    }

    public void NewGame()
    {
        var world =
            WorldFactory.CreateNewWorld(Config);

        StartGame(world);
    }

    public void LoadGame(int slot)
    {
        var world =
            SaveManager.Load(slot);

        StartGame(world);
    }

    private void StartGame(WorldRuntime world)
    {
        Engine.Initialize(world);

        SceneManager.LoadScene("GameScene");
    }
}

六、GameEngine 生命周期
定义游戏状态机：
Uninitialized
       |
       V
Initializing
       |
       V
Running
       |
       +------+
       |      |
       V      V
    Pause   Saving
       |
       V
    Running
       |
       V
    GameOver

状态枚举
public enum GameState
{
    None,
    Initializing,
    Running,
    Paused,
    Saving,
    GameOver
}

七、Game Loop 启动设计
初始化完成后：
Engine.Initialize(world);
内部执行：
Initialize()
      |
      V
Load Systems
      |
      V
Create CommandQueue
      |
      V
Set GameTime
      |
      V
State = Running

八、Game Tick 驱动设计
V1 推荐方案：时间推进
由于微信小游戏需要节省性能：
推荐：
现实 5 秒
        |
        V
游戏 1 个月

例如：
void Update()
{
    timer += Time.deltaTime;

    if(timer >= 5)
    {
        Engine.NextMonth();

        timer = 0;
    }
}

支持暂停
Running
    |
点击暂停
    |
Paused
暂停后：
Update()
{
    return;
}
不进行 Tick。

九、游戏退出流程
玩家退出：
Exit Button
     |
     V
SaveManager.AutoSave()
     |
     V
Unload Scene
     |
     V
返回菜单

十、异常恢复设计
Config 加载失败
例如：
Region ID 不存在
处理：
停止启动
+
显示错误日志

存档版本不兼容
例如：
Save Version 1.0
Game Version 2.0
处理：
执行版本迁移器
或者提示玩家无法读取

十一、Unity 推荐工程目录
Assets
|
├── Scripts
|
├── Bootstrap
|    |
|    └── GameLauncher.cs
|
├── Engine
|    |
|    ├── GameEngine.cs
|    └── GameState.cs
|
├── Config
|
├── Runtime
|
├── Systems
|
├── Save
|
└── UI

十二、与整体架构关系
             Unity Scene
                  |
                  V
            GameLauncher
                  |
      +-----------+-----------+
      |                       |
      V                       V
 ConfigLoader            SaveManager
      |                       |
      V                       |
 ConfigDatabase               |
      |                       |
      +-----------+-----------+
                  |
                  V
            WorldFactory
                  |
                  V
            WorldRuntime
                  |
                  V
             GameEngine
                  |
                  V
            SystemManager
                  |
                  V
              Game Tick

十三、设计总结
Game Engine Bootstrap 的核心思想：
Unity 只负责启动和表现，真正的天下运行由 GameEngine 驱动。
完整生命周期：
启动游戏
   |
加载 Config
   |
创建/读取 WorldRuntime
   |
初始化 GameEngine
   |
启动 Game Tick
   |
运行天下模拟
   |
保存 WorldRuntime
   |
退出游戏