# NoteFluid 项目技术文档

## 项目概述

**NoteFluid MIDI Visualization Player** 是一个基于 WPF 框架开发的 MIDI 可视化播放器应用。该应用能够读取 MIDI 文件并生成实时钢琴卷帘瀑布流动画，为用户提供直观的音乐可视化体验。

### 技术栈

- **.NET 9.0** - 目标框架
- **WPF** - UI 框架
- **CommunityToolkit.Mvvm** - MVVM 框架
- **MaterialDesignThemes** - Material Design 主题
- **Melanchall.DryWetMidi** - MIDI 文件解析
- **NAudio** - 音频处理

---

## 项目结构

```
NoteFluid.Core/
├── Configs/           # 配置数据模型
├── Converters/        # WPF 值转换器
├── Models/            # 数据模型
├── Services/          # 核心业务服务
├── Utilities/         # 工具类
├── ViewModels/        # MVVM 视图模型
├── Views/             # XAML 视图
├── App.xaml           # 应用程序入口
└── MainWindow.xaml    # 主窗口
```

---

## 核心服务

### 1. MidiService
MIDI 文件播放控制服务，负责 MIDI 文件的加载、播放、暂停、停止等操作。

**主要功能：**
- `LoadMidiFile(FileInfo midiFileInfo)` - 加载 MIDI 文件
- `PlayMidiFile(double delayMs)` - 播放 MIDI 文件
- `PauseMidiFile()` - 暂停播放
- `ResumeMidiFile()` - 恢复播放
- `StopMidiFile()` - 停止播放
- `SetProgress(double value)` - 设置播放进度

**事件：**
- `OnMidiFilePlaying` - 播放状态变更
- `OnProgressChanged` - 进度更新
- `OnMidiFileCompleted` - 播放完成
- `OnMidiPlaybackStarted` - 播放启动

### 2. WaterfallService
瀑布流可视化服务，负责生成和管理钢琴卷帘瀑布流动画。

**主要功能：**
- `LoadMidiFile(MidiFile midiFile)` - 加载 MIDI 数据用于可视化
- `Start(double delayMs)` - 启动瀑布流动画
- `Pause()` / `Resume()` - 暂停/恢复动画
- `Stop()` - 停止动画并清除所有瀑布条
- `Clear()` - 清除所有数据

**技术特点：**
- 对象池复用：使用 `ConcurrentBag` 实现瀑布条对象池，避免频繁 GC
- 布局缓存：预计算钢琴键位置，避免重复计算
- 乐器可见性缓存：减少 LINQ 查询，提升性能
- 异步处理：使用 `DispatcherTimer` 进行渲染循环

### 3. AudioService
音频输出设备管理服务，支持音频设备枚举、切换和测试。

**主要功能：**
- `GetOutputDevices()` - 获取所有音频输出设备
- `GetDefaultDevice()` - 获取默认音频设备
- `SetOutputDevice(int deviceNumber)` - 切换音频设备
- `TestOutputDevice()` - 测试音频设备（播放测试音）
- `PlayBeep(int frequency, int duration)` - 播放指定频率的 beep 音

### 4. InstrumentService
乐器信息管理服务，负责 MIDI 文件中乐器信息的读取和管理。

**主要功能：**
- `LoadMidiFile(MidiFileInfo midiFileInfo, MidiFile midiFile)` - 加载并解析乐器信息
- `UpdateInstrumentColor()` - 更新乐器颜色
- `UpdateInstrumentVisibility()` - 更新乐器可见性
- `UpdateInstrumentMute()` - 更新乐器静音状态
- `UpdateInstrumentSolo()` - 更新乐器独奏状态

**内置功能：**
- GM 标准乐器名称映射（128个乐器）
- 乐器分类（16大类）
- 乐器配置持久化

### 5. FileService
文件系统服务，负责 MIDI 文件的读取和管理。

**主要功能：**
- `GetAllMidiFilePaths()` - 获取所有 MIDI 文件路径
- `GetAllMidiFiles()` - 获取所有 MIDI 文件信息
- `GetMidiFolderPath()` - 获取 MIDI 文件夹路径
- `EnsureDirectoryExists()` - 确保目录存在

**默认路径：** `Documents/NoteFluid/MIDI/`

### 6. ConfigService
配置管理服务，负责应用程序配置的加载和保存。

**配置文件：** `Settings.json`

**配置结构：**
```json
{
  "Theme": {
    "IsDarkTheme": true,
    "ColorIndex": 0
  },
  "Visualization": {
    "ShowPitchName": true,
    "ShowOctave": true
  },
  "MidiFileConfigs": {
    "文件名.mid": {
      "Instruments": [...]
    }
  }
}
```

---

## 数据模型

### InstrumentInfo
乐器信息模型，包含乐器的各种属性和状态。

**属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| PatchNumber | int | GM 乐器编号 (0-127) |
| InstrumentName | string | 乐器名称 |
| Channel | int | MIDI 通道 (0-15) |
| NoteCount | int | 音符数量 |
| IsPercussion | bool | 是否为打击乐器 |
| InstrumentId | int | 运行时唯一标识 |
| DisplayName | string | 显示名称 |
| Color | Color | 瀑布条颜色 |
| IsVisible | bool | 是否可见 |
| IsMuted | bool | 是否静音 |
| IsSolo | bool | 是否独奏 |
| IconKind | PackIconKind | 对应图标 |

### MidiNoteEvent
MIDI 音符事件模型，用于瀑布流可视化。

**属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| NoteNumber | int | MIDI 音符编号 (21-108) |
| Channel | int | MIDI 通道 |
| PatchNumber | int | 乐器编号 |
| StartTimeMs | double | 开始时间（毫秒） |
| EndTimeMs | double | 结束时间（毫秒） |
| DurationMs | double | 持续时间（毫秒） |
| Color | Color | 颜色 |
| IsVisible | bool | 是否可见 |
| TrackIndex | int | 轨道索引 |

### AudioDeviceInfo
音频设备信息模型。

**属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| DeviceId | string | 设备ID |
| DeviceName | string | 设备名称 |
| DeviceFriendlyName | string | 设备友好名称 |
| Channels | int | 声道数 |
| IsDefault | bool | 是否为默认设备 |

---

## 视图模型

### MainViewModel
主视图模型，负责主题切换和音频设备管理。

**主要功能：**
- 主题切换（深色/浅色主题）
- 主题颜色选择
- 音频设备枚举和切换

### VisualizationViewModel
可视化页面视图模型，控制 MIDI 播放和瀑布流动画。

**主要功能：**
- 播放控制（播放/暂停/停止）
- 进度控制
- 钢琴键盘状态管理
- 显示设置（音名、八度）

### FileViewModel
文件列表视图模型，管理 MIDI 文件列表。

### InstrumentsViewModel
乐器列表视图模型，管理每个乐器的显示、颜色和静音设置。

---

## 视图

### MainMenu.xaml
主菜单页面，提供导航入口。

### FileList.xaml
MIDI 文件列表页面，显示可用的 MIDI 文件。

### MidiVisualization.xaml
MIDI 可视化主页面，包含：
- 顶部工具栏（返回、播放控制）
- 进度条
- 瀑布流画布
- 钢琴键盘

### Instruments.xaml
乐器列表页面，显示所有检测到的乐器及其配置选项。

### Settings.xaml
设置页面，包含：
- MIDI 文件夹管理
- 主题设置
- 音频设备选择

### WaterfallBar.xaml
瀑布条用户控件，表示单个音符的瀑布条。

---

## 工具类

### MidiPlayer
MIDI 播放器封装类，处理 MIDI 事件播放逻辑。

**主要功能：**
- `Start()` - 开始播放
- `Pause()` - 暂停
- `Resume()` - 恢复
- `Stop()` - 停止
- `SetPosition(double milliseconds)` - 设置播放位置
- `SetTimeOffset(double offsetMs)` - 设置时间偏移

### MidiInstrumentReader
MIDI 乐器读取工具类，从 MIDI 文件中提取乐器信息。

**主要功能：**
- `GetTrackInstruments(MidiFile midiFile)` - 获取所有轨道中的乐器

---

## 配置系统

### ThemeConfig
主题配置，包含深色/浅色主题设置和主题颜色索引。

### VisualizationConfig
可视化配置，包含音名和八度的显示设置。

### MidiFileConfig
单个 MIDI 文件的配置，包含该文件中所有乐器的配置信息。

### InstrumentConfig
单个乐器的配置，包含颜色、可见性、静音和独奏状态。

---

## 依赖注入配置

在 `App.xaml.cs` 中使用 Microsoft.Extensions.DependencyInjection 配置依赖注入：

```csharp
services.AddSingleton<NavigateService>();
services.AddSingleton<ThemeService>();
services.AddSingleton<AudioService>();
services.AddSingleton<ConfigService>();
services.AddSingleton<FileService>();
services.AddSingleton<InstrumentService>();
services.AddSingleton<MidiService>();
services.AddSingleton<WaterfallService>();
```

---

## 事件系统

服务间通过事件进行通信，主要事件包括：

| 事件 | 定义位置 | 说明 |
|------|----------|------|
| OnMidiFilePlaying | MidiService | 播放状态变更 |
| OnProgressChanged | MidiService | 进度更新 |
| OnMidiFileCompleted | MidiService | 播放完成 |
| OnInstrumentColorChanged | InstrumentService | 乐器颜色变更 |
| OnInstrumentVisibilityChanged | InstrumentService | 乐器可见性变更 |
| OnInstrumentMuteChanged | InstrumentService | 乐器静音状态变更 |
| OnInstrumentSoloChanged | InstrumentService | 乐器独奏状态变更 |
| OnBarReached | WaterfallService | 瀑布条到达底部 |
| OnAllBarsCompleted | WaterfallService | 所有瀑布条完成 |

---

## 开发指南

### 添加新的服务
1. 在 `Services/` 目录下创建服务类
2. 在 `App.xaml.cs` 中注册服务
3. 通过构造函数注入到需要使用的地方

### 添加新的视图
1. 在 `Views/` 目录下创建 XAML 和代码后台文件
2. 在 `NavigateService` 中注册视图
3. 创建对应的 `ViewModel`

### 修改瀑布流样式
- 修改 `WaterfallService` 中的 `FallSpeed` 属性可调整下落速度
- 修改 `WaterfallBar.xaml` 可调整瀑布条样式

---

## 版本信息

- 框架：.NET 9.0-windows
- WPF 版本：WPF
- 最低 Windows 版本：Windows 10
