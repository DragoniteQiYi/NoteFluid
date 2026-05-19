# NoteFluid Code Wiki

## 项目概述

**NoteFluid MIDI Visualization Player** 是一个功能简单、易用、免费的MIDI可视化应用，基于WPF应用框架开发。

- **项目类型**: Windows桌面应用程序 (WPF)
- **.NET版本**: .NET 9.0
- **架构模式**: MVVM (Model-View-ViewModel)
- **主要功能**: MIDI文件播放、钢琴卷帘可视化、自由演奏模式

---

## 1. 项目架构

### 1.1 整体架构图

```
NoteFluid.Core
├── App.xaml / App.xaml.cs        # 应用程序入口，依赖注入配置
├── MainWindow.xaml / .cs         # 主窗口，包含导航框架
│
├── Command/                      # 命令层
│   └── RelayCommand.cs          # MVVM命令实现
│
├── Configs/                     # 配置模型层
│   ├── ConfigData.cs            # 配置根对象
│   ├── ThemeConfig.cs           # 主题配置
│   └── VisualizationConfig.cs   # 可视化配置
│
├── Models/                      # 数据模型层
│   ├── Note.cs                  # 音符模型
│   ├── MidiPlayer.cs            # MIDI播放器核心
│   ├── MusicPlaybackState.cs    # 播放状态
│   ├── MidiFileInfo.cs          # MIDI文件信息
│   ├── TrackInfo.cs             # 轨道信息
│   ├── AudioDeviceInfo.cs       # 音频设备信息
│   ├── PianoKey.cs              # 钢琴键模型
│   ├── ColorItem.cs             # 颜色项
│   └── MidiFileMetadata.cs      # MIDI元数据
│
├── Services/                     # 服务层
│   ├── AudioService.cs          # 音频设备管理
│   ├── MidiService.cs           # MIDI播放服务
│   ├── FileService.cs          # 文件操作服务
│   ├── ConfigService.cs         # 配置管理服务
│   ├── ThemeService.cs          # 主题切换服务
│   └── NavigateService.cs       # 导航服务
│
├── ViewModels/                  # 视图模型层
│   ├── MainViewModel.cs         # 主视图模型
│   ├── FileViewModel.cs         # 文件列表视图模型
│   ├── VisualizationViewModel.cs # 可视化视图模型
│   └── FreePlayViewModel.cs    # 自由演奏视图模型
│
├── Views/                       # 视图层
│   ├── MainMenu.xaml/.cs        # 主菜单页面
│   ├── Settings.xaml/.cs        # 设置页面
│   ├── FileList.xaml/.cs        # 文件列表页面
│   ├── MidiVisualization.xaml/.cs # MIDI可视化页面
│   ├── FreePlay.xaml/.cs       # 自由演奏页面
│   └── Instruments.xaml/.cs    # 乐器页面(占位)
│
├── Controls/                    # 自定义控件
│   └── PianoRollController.cs   # 钢琴卷帘控制器(待实现)
│
└── Converters/                  # 值转换器
    └── BoolToBrushConverter.cs  # 布尔值到画刷转换器
```

### 1.2 依赖关系

```
App.xaml.cs (依赖注入配置)
    ↓
    ├── Services (单例注册)
    │   ├── ThemeService
    │   ├── NavigateService
    │   ├── AudioService
    │   ├── FileService
    │   ├── MidiService
    │   └── ConfigService
    │
    ├── ViewModels (生命周期管理)
    │   ├── MainViewModel (单例)
    │   ├── FileViewModel (瞬态)
    │   ├── VisualizationViewModel (瞬态)
    │   └── FreePlayViewModel (瞬态)
    │
    └── Views (瞬态创建)
        ├── MainWindow
        ├── MainMenu
        ├── Settings
        ├── FileList
        ├── MidiVisualization
        └── FreePlay
```

---

## 2. 核心模块详解

### 2.1 应用程序入口 (App.xaml.cs)

**职责**: 
- 应用程序初始化
- 依赖注入容器配置
- 控制台调试窗口管理

**关键代码结构**:

```csharp
public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; set; }

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 注册服务 (单例)
        services.AddSingleton<ThemeService>();
        services.AddSingleton<NavigateService>();
        services.AddSingleton<AudioService>();
        services.AddSingleton<FileService>();
        services.AddSingleton<MidiService>();
        services.AddSingleton<ConfigService>();

        // 注册ViewModel
        services.AddSingleton<MainViewModel>();
        services.AddTransient<FileViewModel>();
        services.AddTransient<VisualizationViewModel>();
        services.AddTransient<FreePlayViewModel>();
        
        // 注册窗口/页面
        services.AddTransient<MainWindow>();
        services.AddTransient<MainMenu>();
        services.AddTransient<Settings>();
        services.AddTransient<FileList>();
        services.AddTransient<MidiVisualization>();
        services.AddTransient<FreePlay>();
    }
}
```

**特殊功能**: 
- 启动时分配控制台窗口用于调试输出
- 支持 Debug.WriteLine 和 Console.WriteLine 双通道输出

---

### 2.2 服务层 (Services)

#### 2.2.1 AudioService

**文件位置**: `Services/AudioService.cs`

**职责**: 音频设备管理与声音播放

**主要方法**:

| 方法名 | 描述 | 返回值 |
|--------|------|--------|
| `GetOutputDevices()` | 获取所有音频输出设备列表 | `List<AudioDeviceInfo>` |
| `GetDefaultDevice()` | 获取当前默认音频设备 | `AudioDeviceInfo` |
| `SetOutputDevice(int deviceNumber)` | 设置输出设备 | `bool` |
| `TestOutputDevice()` | 测试当前输出设备 | `void` |
| `PlayBeep(int frequency, int duration, double amplitude)` | 播放指定频率的声音 | `void` |
| `PlayBeepAsync(...)` | 异步播放声音 | `Task` |
| `PlayNote(double frequency, int durationMs, double amplitude)` | 播放音乐音符 | `void` |
| `StopPlayback()` | 停止当前播放 | `void` |

**核心实现**:

```csharp
private float[] GenerateSineWave(int frequency, int durationMs, double amplitude)
{
    // 生成正弦波音频数据
    // 采样率: 44100 Hz
    // 添加淡入淡出效果(10ms)避免咔嗒声
}
```

---

#### 2.2.2 MidiService

**文件位置**: `Services/MidiService.cs`

**职责**: MIDI文件播放控制

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `PlayMidiFile(FileInfo midiFileInfo)` | 播放MIDI文件 |
| `PauseMidiFile()` | 暂停播放 |
| `ResumeMidiFile()` | 恢复播放 |
| `StopMidiFile()` | 停止播放 |
| `SetProgress(double value)` | 设置播放进度(0-1) |
| `PlayNoteAsync(int midiNote)` | 播放指定音符 |

**事件**:

| 事件名 | 描述 | 参数 |
|--------|------|------|
| `OnMidiFilePlaying` | 播放状态变更 | `bool isPlaying` |
| `OnMidiFileResume` | 恢复播放 | `bool isResume` |
| `OnProgressChanged` | 进度更新 | `TimeSpan current, TimeSpan total` |
| `OnMidiFileCompleted` | 播放完成 | - |

**内部组件**:

```csharp
private MidiPlayer? _midiPlayer;     // MIDI播放器实例
private MidiFile? _currentMidiFile;   // 当前文件
private MidiOut? _midiOut;            // MIDI输出设备
private Timer? _progressTimer;       // 进度更新定时器(100ms)
```

---

#### 2.2.3 MidiPlayer

**文件位置**: `Models/MidiPlayer.cs`

**职责**: MIDI播放器的核心实现，处理MIDI事件时间同步

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `Start()` | 开始播放 |
| `Pause()` | 暂停播放 |
| `Resume()` | 恢复播放 |
| `Stop()` | 停止播放 |
| `SetPosition(double positionMs)` | 设置播放位置(毫秒) |
| `NoteOn(int midiNote, int velocity, int channel)` | 发送Note On消息 |
| `NoteOff(int midiNote, int channel)` | 发送Note Off消息 |
| `PlayNoteAsync(int midiNote, int durationMs, int velocity, int channel)` | 异步播放音符 |

**属性**:

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `IsPlaying` | `bool` | 是否正在播放 |
| `IsPaused` | `bool` | 是否暂停 |
| `TotalDurationMs` | `double` | 总时长(毫秒) |
| `CurrentTimeMs` | `double` | 当前时间(毫秒) |
| `OnPlaybackCompleted` | `Action` | 播放完成回调事件 |

**时间同步核心算法**:

```csharp
// tick转毫秒
private double CalculateMsFromTick(int tick)
{
    // 根据TempoEvent列表分段计算
    // 默认120 BPM = 500000微秒/四分音符
}

// 毫秒转tick
private int CalculateTickFromMs(double targetMs)
{
    // 逆运算，考虑tempo变化
}

// 处理MIDI事件
private void ProcessMidiEvent(MidiEvent midiEvent)
{
    // 处理 NoteOnEvent (含Velocity=0的Note Off)
    // 处理 NoteOffEvent
    // 处理 PatchChangeEvent (音色变化)
    // 处理 ControlChangeEvent (控制变化)
}
```

---

#### 2.2.4 FileService

**文件位置**: `Services/FileService.cs`

**职责**: MIDI文件目录管理与文件操作

**默认路径**:

```csharp
_midiFolderPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "NoteFluid",
    "MIDI"
);
```

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `GetAllMidiFilePaths()` | 获取所有.mid文件的完整路径 |
| `GetAllMidiFiles()` | 获取所有.mid文件的FileInfo |
| `GetAllMidiFileNames()` | 获取所有.mid文件的文件名 |
| `MidiFileExists(string fileName)` | 检查文件是否存在 |
| `GetMidiFolderPath()` | 获取MIDI文件夹路径 |
| `EnsureDirectoryExists()` | 确保目录存在，不存在则创建 |

---

#### 2.2.5 ConfigService

**文件位置**: `Services/ConfigService.cs`

**职责**: JSON配置文件读写管理

**配置文件路径**:

```csharp
_filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");
```

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `Read<T>()` | 同步读取配置 |
| `ReadAsync<T>()` | 异步读取配置 |
| `Write<T>(T config)` | 同步写入配置 |
| `WriteAsync<T>(T config)` | 异步写入配置 |
| `Update<T>(Action<T> updateAction)` | 局部更新配置 |
| `UpdateAsync<T>(Action<T> updateAction)` | 异步局部更新 |
| `Save()` | 保存当前ConfigData |
| `Exists()` | 检查配置文件是否存在 |

**线程安全**: 使用 `SemaphoreSlim` 实现单线程访问

---

#### 2.2.6 ThemeService

**文件位置**: `Services/ThemeService.cs`

**职责**: Material Design主题切换管理

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `GetCurrentTheme()` | 获取当前主题 |
| `ToggleBaseTheme()` | 切换明暗主题 |
| `SetBaseTheme(BaseTheme baseTheme)` | 设置基础主题 |
| `ChangePrimaryColor(Color color)` | 更改主色 |
| `ChangeSecondaryColor(Color color)` | 更改次要色 |
| `IsDarkTheme()` | 检查是否为深色主题 |

---

#### 2.2.7 NavigateService

**文件位置**: `Services/NavigateService.cs`

**职责**: 页面导航管理

**支持的页面**:

| 页面名 | 类型 |
|--------|------|
| "MainMenu" | MainMenu |
| "Settings" | Settings |
| "FileList" | FileList |
| "MidiVisualization" | MidiVisualization |
| "FreePlay" | FreePlay |

---

### 2.3 视图模型层 (ViewModels)

#### 2.3.1 MainViewModel

**文件位置**: `ViewModels/MainViewModel.cs`

**职责**: 主窗口和主菜单的业务逻辑

**主要属性**:

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `OutputDevices` | `List<AudioDeviceInfo>` | 音频设备列表 |
| `SelectedDevice` | `AudioDeviceInfo` | 当前选中设备 |
| `DefaultDevice` | `AudioDeviceInfo` | 默认设备 |
| `IsLoading` | `bool` | 加载状态 |
| `StatusMessage` | `string` | 状态消息 |
| `Colors` | `ObservableCollection<ColorItem>` | 预设颜色列表 |
| `SelectedColorIndex` | `int` | 选中颜色索引 |

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `Navigate(string pageName)` | 导航到指定页面 |
| `GetAudioDevices()` | 获取音频设备列表 |
| `SwitchDevice(int index)` | 切换音频设备 |
| `PlayTestAudio()` | 播放测试音频 |
| `SetBaseTheme(BaseTheme baseTheme)` | 设置主题 |

---

#### 2.3.2 FileViewModel

**文件位置**: `ViewModels/FileViewModel.cs`

**职责**: 文件列表页面逻辑，控制MIDI播放

**主要属性**:

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `FileInfos` | `List<FileInfo>` | 所有MIDI文件信息 |
| `FilteredFiles` | `List<FileInfo>` | 过滤后的文件列表 |
| `SelectedFile` | `FileInfo` | 当前选中文件 |
| `IsPlaying` | `bool` | 是否正在播放 |
| `IsPausing` | `bool` | 是否暂停 |
| `ProgressValue` | `double` | 播放进度(0-100) |
| `PlayIconKind` | `PackIconKind` | 播放图标类型 |

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `PlayStopAsync()` | 播放/暂停切换 |
| `SetProgressValue(double value)` | 设置播放进度 |
| `FilterFiles(string regexText)` | 使用正则表达式过滤文件 |
| `NavigateTo(string pagePath)` | 导航到其他页面 |

**支持的命令**: `PlayStopCommand`

---

#### 2.3.3 VisualizationViewModel

**文件位置**: `ViewModels/VisualizationViewModel.cs`

**职责**: MIDI可视化页面逻辑，钢琴卷帘渲染

**钢琴键常量**:

```csharp
private const int START_MIDI_NOTE = 21;        // 起始音符(A0)
private const int TOTAL_KEYS = 88;             // 总键数(88键钢琴)
private const int WHITE_KEY_COUNT = 52;       // 白键数量
private const double BASE_WHITE_KEY_HEIGHT = 130;  // 白键高度
private const double BASE_BLACK_KEY_HEIGHT = 80;   // 黑键高度
```

**主要方法**:

| 方法名 | 描述 |
|--------|------|
| `GeneratePianoKeys(double availableWidth)` | 生成钢琴键数据模型 |
| `DrawPiano(Canvas pianoCanvas, double actualWidth)` | 绘制钢琴UI |
| `PressKey(int midiNote)` | 处理按键按下 |
| `ReleaseKey(int midiNote)` | 处理按键释放 |
| `ChangePitchNameDisplay(bool state)` | 切换音名显示 |
| `ChangeOctaveDisplay(bool state)` | 切换八度显示 |

**钢琴键序列**:

```csharp
private readonly string[] keyboardNoteSequence =
    { "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#" };
```

---

#### 2.3.4 FreePlayViewModel

**文件位置**: `ViewModels/FreePlayViewModel.cs`

**职责**: 自由演奏页面逻辑，允许用户实时演奏钢琴

**与VisualizationViewModel对比**:

| 特性 | VisualizationViewModel | FreePlayViewModel |
|------|------------------------|-------------------|
| MIDI输出 | 使用共享MidiService | 独立MidiPlayer实例 |
| 文件关联 | 关联选中文件 | 不关联文件 |
| 用途 | 播放MIDI时可视化 | 自由演奏 |

---

### 2.4 模型层 (Models)

#### 2.4.1 Note

**文件位置**: `Models/Note.cs`

**描述**: MIDI音符数据模型

**属性**:

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `TrackNumber` | `int` | 轨道编号 |
| `NoteNumber` | `int` | MIDI音符编号(0-127) |
| `AbsoluteTime` | `long` | 绝对时间(微秒) |
| `Duration` | `long` | 持续时间(微秒) |
| `Velocity` | `int` | 按键力度(0-127,默认100) |
| `NoteName` | `string` | 音符名称(C, C#, D...) |
| `Octave` | `int` | 八度编号 |
| `AbsoluteTimeInSeconds` | `double` | 绝对时间(秒) |
| `DurationInSeconds` | `double` | 持续时间(秒) |

**音符名称映射**:

```csharp
private static readonly string[] NoteNames =
    { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
```

---

#### 2.4.2 PianoKey

**文件位置**: `Models/PianoKey.cs`

**描述**: 钢琴键UI数据模型

**属性**:

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `MidiNote` | `int` | MIDI音符编号 |
| `NoteName` | `string` | 音符名称 |
| `Octave` | `int` | 八度 |
| `IsBlackKey` | `bool` | 是否为黑键 |
| `Width` | `double` | 宽度 |
| `Height` | `double` | 高度 |
| `X` | `double` | X坐标 |
| `Y` | `double` | Y坐标 |
| `ZIndex` | `int` | Z轴层级 |
| `IsPressed` | `bool` | 是否按下 |
| `DisplayText` | `string` | 显示文本 |
| `KeyClickCommand` | `ICommand` | 点击命令 |

---

#### 2.4.3 TrackInfo

**文件位置**: `Models/TrackInfo.cs`

**描述**: MIDI轨道信息模型

**属性**:

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `TrackNumber` | `int` | 轨道编号 |
| `TrackName` | `string` | 轨道名称 |
| `NoteCount` | `int` | 音符数量 |
| `Color` | `Color` | 显示颜色 |
| `IsVisible` | `bool` | 是否可见 |
| `IsMuted` | `bool` | 是否静音 |
| `IsSolo` | `bool` | 是否独奏 |
| `DisplayName` | `string` | 显示名称(优先使用TrackName) |

**实现**: `INotifyPropertyChanged`

---

### 2.5 配置系统

#### 2.5.1 ConfigData

**文件位置**: `Configs/ConfigData.cs`

```csharp
public class ConfigData
{
    public ThemeConfig? Theme { get; set; }
    public VisualizationConfig? Visualization { get; set; }
}
```

#### 2.5.2 ThemeConfig

**文件位置**: `Configs/ThemeConfig.cs`

```csharp
public class ThemeConfig
{
    public bool IsDarkTheme { get; set; }    // 是否深色主题
    public int ColorIndex { get; set; }     // 主题颜色索引
}
```

#### 2.5.3 VisualizationConfig

**文件位置**: `Configs/VisualizationConfig.cs`

```csharp
public class VisualizationConfig
{
    public bool ShowPitchName { get; set; }   // 显示音名
    public bool ShowOctave { get; set; }     // 显示八度
    public double FallingSpeed { get; set; }  // 音符下落速度
}
```

---

### 2.6 命令系统

#### 2.6.1 RelayCommand

**文件位置**: `Command/RelayCommand.cs`

**描述**: MVVM命令实现，支持有无参数版本

```csharp
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;
    
    public RelayCommand(Action execute, Func<bool>? canExecute = null);
    public bool CanExecute(object? parameter);
    public void Execute(object? parameter);
    public void RaiseCanExecuteChanged();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;
    // ...
}
```

---

### 2.7 值转换器

#### 2.7.1 BoolToBrushConverter

**文件位置**: `Converters/BoolToBrushConverter.cs`

```csharp
public class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = Brushes.White;
    public Brush FalseBrush { get; set; } = Brushes.Gray;
    
    public object Convert(...);  // bool → Brush
    public object ConvertBack(...);  // NotImplementedException
}

public class StringToVisibilityConverter : IValueConverter
{
    // string → Visibility (空字符串为Collapsed)
}
```

---

## 3. 依赖包说明

### 3.1 NuGet包清单

| 包名 | 版本 | 用途 |
|------|------|------|
| `CommunityToolkit.Mvvm` | 8.4.2 | MVVM工具包 |
| `MaterialDesignThemes` | 5.3.2 | Material Design UI框架 |
| `MaterialDesignColors` | 5.3.2 | Material Design颜色 |
| `Melanchall.DryWetMidi` | 8.0.3 | MIDI文件解析 |
| `NAudio` | 2.3.0 | 音频处理 |
| `NAudio.Midi` | 2.3.0 | MIDI设备支持 |
| `Microsoft.Extensions.Configuration` | 10.0.8 | 配置管理 |
| `Microsoft.Extensions.Configuration.Json` | 10.0.8 | JSON配置 |
| `Microsoft.Extensions.DependencyInjection` | 10.0.8 | 依赖注入 |

### 3.2 核心依赖说明

**NAudio**: 用于音频输出和MIDI设备通信
**Melanchall.DryWetMidi**: 用于MIDI文件解析和音符提取
**MaterialDesignThemes**: 提供现代化UI组件和主题系统
**CommunityToolkit.Mvvm**: 简化MVVM模式实现

---

## 4. 项目运行方式

### 4.1 开发环境要求

- **.NET SDK**: 9.0 或更高版本
- **IDE**: Visual Studio 2022+ / Rider / VS Code
- **操作系统**: Windows 10/11

### 4.2 构建项目

```bash
# 使用 dotnet CLI
cd g:\VS工程\NoteFluid
dotnet build NoteFluid.Core/NoteFluid.Core.csproj

# 或使用 Visual Studio
# 打开 NoteFluid.sln 解决方案文件
# 选择 Release/Debug 配置
# 按 Ctrl+Shift+B 构建
```

### 4.3 运行项目

```bash
# 使用 dotnet CLI
dotnet run --project NoteFluid.Core/NoteFluid.Core.csproj

# 或在 Visual Studio 中
# 打开 NoteFluid.sln
# 设置启动项目为 NoteFluid.Core
# 按 F5 运行
```

### 4.4 发布为可执行文件

```bash
# 发布为自包含可执行文件
dotnet publish NoteFluid.Core/NoteFluid.Core.csproj -c Release -r win-x64 --self-contained

# 发布为单文件
dotnet publish NoteFluid.Core/NoteFluid.Core.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## 5. 页面导航流程

```
MainWindow (主窗口)
    │
    ├── MainMenu (主菜单)
    │   │
    │   ├── [演奏音乐] → FileList (文件列表) 
    │   │               │
    │   │               └── [可视化] → MidiVisualization (MIDI可视化)
    │   │
    │   ├── [自由演奏] → FreePlay (自由演奏)
    │   │
    │   ├── [设置] → Settings (设置)
    │   │
    │   └── [退出] → 关闭应用
    │
    └── [返回主菜单] → MainMenu
```

---

## 6. MIDI文件处理流程

```
1. FileService 获取 MIDI 文件列表
   └── 从 Documents/NoteFluid/MIDI 目录读取 .mid 文件

2. FileViewModel 选择要播放的文件
   └── MidiService.PlayMidiFile() 加载文件

3. MidiService 创建 MidiPlayer 实例
   └── 解析 MIDI 事件
   └── 初始化 MidiOut 设备
   └── 创建进度更新定时器

4. MidiPlayer 播放控制
   └── Start() / Pause() / Resume() / Stop()
   └── SetPosition() 跳转位置
   └── 定时器触发 UpdatePlayback()

5. ProcessMidiEvent 处理 MIDI 事件
   └── NoteOnEvent → 发送 Note On 消息
   └── NoteOffEvent → 发送 Note Off 消息
   └── PatchChangeEvent → 音色变化
   └── ControlChangeEvent → 控制变化

6. NAudio.MidiOut 发送 MIDI 消息到硬件/软件合成器
```

---

## 7. 主题系统

### 7.1 主题配置存储

```json
// Settings.json
{
  "Theme": {
    "IsDarkMode": false,
    "ColorIndex": 1
  },
  "Visualization": {
    "ShowPitchName": false,
    "ShowOctave": true,
    "FallingSpeed": 1
  }
}
```

### 7.2 预设颜色

```csharp
new[] {
    new { Name = "暖桃红", ColorHex = "#FF6B6B" },
    new { Name = "蜜橙", ColorHex = "#FFA94D" },
    new { Name = "薄荷绿", ColorHex = "#69DB7C" },
    new { Name = "天空蓝", ColorHex = "#4DABF7" },
    new { Name = "薰衣草紫", ColorHex = "#9775FA" }
}
```

---

## 8. 常见问题排查

### 8.1 没有MIDI输出设备

**问题**: 播放MIDI时提示"没有找到MIDI输出设备"

**解决方案**:
1. 安装虚拟MIDI端口软件(如 LoopBe1、MIDI-OX)
2. 或使用系统内置的Microsoft GS Wavetable Synth

### 8.2 播放无声

**检查项**:
1. 确认MIDI设备已正确选择
2. 检查系统音量设置
3. 验证MIDI文件不是空的

### 8.3 无法读取MIDI文件

**检查项**:
1. 确认文件扩展名为 `.mid` 或 `.midi`
2. 确认文件位于 `Documents/NoteFluid/MIDI/` 目录
3. 检查文件是否被其他程序占用

---

## 9. 待实现功能 (TODO)

根据项目当前状态，以下功能尚未完成:

1. **PianoRollController** - 钢琴卷帘控制器(空类)
2. **Instruments** - 乐器选择页面(仅基础框架)
3. **钢琴卷帘瀑布流动画** - 音符实时可视化(计划中)
4. **多语言本地化支持** - 国际化(计划中)
5. **TrackInfo 相关功能** - 轨道颜色、独奏、静音控制

---

## 10. 文件结构总结

```
NoteFluid/
│
├── NoteFluid.sln                    # 解决方案文件
├── README.md                        # 项目说明文档
│
└── NoteFluid.Core/                  # 主项目
    │
    ├── NoteFluid.Core.csproj        # 项目文件
    ├── Settings.json                # 运行时配置
    │
    ├── App.xaml / .cs               # 应用入口
    ├── MainWindow.xaml / .cs        # 主窗口
    │
    ├── Command/                     # 命令
    │   └── RelayCommand.cs
    │
    ├── Configs/                     # 配置模型
    │   ├── ConfigData.cs
    │   ├── ThemeConfig.cs
    │   └── VisualizationConfig.cs
    │
    ├── Controls/                    # 自定义控件
    │   └── PianoRollController.cs   # TODO
    │
    ├── Converters/                  # 值转换器
    │   └── BoolToBrushConverter.cs
    │
    ├── Models/                      # 数据模型
    │   ├── Note.cs
    │   ├── MidiPlayer.cs
    │   ├── MusicPlaybackState.cs
    │   ├── MidiFileInfo.cs
    │   ├── MidiFileMetadata.cs
    │   ├── TrackInfo.cs
    │   ├── AudioDeviceInfo.cs
    │   ├── PianoKey.cs
    │   └── ColorItem.cs
    │
    ├── Services/                    # 服务层
    │   ├── AudioService.cs
    │   ├── MidiService.cs
    │   ├── FileService.cs
    │   ├── ConfigService.cs
    │   ├── ThemeService.cs
    │   └── NavigateService.cs
    │
    ├── ViewModels/                  # 视图模型
    │   ├── MainViewModel.cs
    │   ├── FileViewModel.cs
    │   ├── VisualizationViewModel.cs
    │   └── FreePlayViewModel.cs
    │
    └── Views/                      # 视图
        ├── MainMenu.xaml / .cs
        ├── Settings.xaml / .cs
        ├── FileList.xaml / .cs
        ├── MidiVisualization.xaml / .cs
        ├── FreePlay.xaml / .cs
        └── Instruments.xaml / .cs   # TODO
```

---

**文档版本**: 1.0  
**生成日期**: 2026-05-19  
**项目状态**: 活跃开发中
