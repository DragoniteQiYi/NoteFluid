# Instruments 功能实现总结

## 已实现功能

### 1. MidiFileConfig 配置类
**文件**: `Configs/MidiFileConfig.cs`

- `MidiFileConfig` - 存储单个MIDI文件的配置
  - `FileKey` - 文件唯一标识
  - `FileName` - 文件名
  - `Tracks` - 轨道配置列表
  - `LastModified` - 最后修改时间

- `TrackConfig` - 单个轨道配置
  - `TrackNumber` - 轨道编号
  - `TrackName` - 轨道名称
  - `InstrumentNumber` - 乐器编号 (MIDI Program Number)
  - `InstrumentName` - 乐器名称
  - `ColorHex` - 轨道颜色 (十六进制)
  - `IsVisible` - 是否可见
  - `IsMuted` - 是否静音
  - `IsSolo` - 是否独奏

### 2. ConfigData 更新
**文件**: `Configs/ConfigData.cs`

添加了 `Dictionary<string, MidiFileConfig> MidiFileConfigs` 属性，用于存储所有MIDI文件的配置。

### 3. VisualizationService 服务
**文件**: `Services/VisualizationService.cs`

核心功能：
- **彩虹六色分配**: 红、橙、黄、绿、蓝、紫循环分配给轨道
- **配置持久化**: 自动保存到 Settings.json
- **轨道信息管理**: 加载、更新、查询轨道颜色和状态
- **乐器名称映射**: 完整的128种GM乐器名称

主要方法：
- `LoadMidiFile(MidiFileInfo)` - 加载MIDI文件并初始化配置
- `GetOrCreateMidiFileConfig()` - 获取或创建文件配置
- `UpdateTrackColor()` - 更新轨道颜色
- `UpdateTrackVisibility()` - 更新可见性
- `UpdateTrackMute()` - 更新静音状态
- `UpdateTrackSolo()` - 更新独奏状态
- `GetVisibleTrackColors()` - 获取可见轨道颜色字典

### 4. InstrumentsViewModel
**文件**: `ViewModels/InstrumentsViewModel.cs`

功能：
- 从 `FileService` 获取选中的MIDI文件
- 使用NAudio读取MIDI文件信息
- 解析轨道名称和乐器信息
- 管理轨道列表 (`ObservableCollection<TrackInfo>`)
- 提供颜色修改、可见性切换、静音、独奏功能

### 5. Instruments 页面 (XAML)
**文件**: `Views/Instruments.xaml`

UI组件：
- 顶部工具栏（返回按钮、标题）
- 文件信息栏（文件名、轨道数、音符数、时长）
- 轨道列表（使用ItemsControl）
  - 轨道编号
  - 轨道名称
  - 颜色选择器（点击弹出颜色菜单）
  - 可见性按钮（眼睛图标）
  - 静音按钮（音量图标）
  - 独奏按钮（S图标）
- 重置颜色按钮
- 底部说明栏

### 6. Instruments 代码后置
**文件**: `Views/Instruments.xaml.cs`

功能：
- 依赖注入获取ViewModel
- 颜色选择菜单处理
- 自定义颜色对话框（使用Windows Forms ColorDialog）
- 可见性/静音/独奏按钮事件处理
- 多个IValueConverter实现

### 7. 依赖注入注册
**文件**: `App.xaml.cs`

注册了：
- `VisualizationService` (Singleton)
- `InstrumentsViewModel` (Transient)
- `Instruments` 页面 (Transient)

### 8. 导航服务更新
**文件**: `Services/NavigateService.cs`

添加了 `"Instruments"` 页面导航支持。

### 9. 主菜单更新
**文件**: `Views/MainMenu.xaml`, `MainMenu.xaml.cs`

添加了"乐器配置"按钮，可以导航到Instruments页面。

### 10. 项目文件更新
**文件**: `NoteFluid.Core.csproj`

添加了 `<UseWindowsForms>true</UseWindowsForms>` 以支持颜色选择对话框。

## 使用流程

1. **首次使用**:
   - 从主菜单点击"乐器配置"
   - 如果没有选择文件，会显示提示
   - 返回文件列表选择一个MIDI文件

2. **加载文件**:
   - 系统读取MIDI文件
   - 自动为每个轨道分配彩虹色（红橙黄绿蓝紫循环）
   - 解析轨道名称和乐器信息
   - 保存配置到 Settings.json

3. **修改配置**:
   - 点击颜色圆圈选择新颜色
   - 点击眼睛图标切换可见性
   - 点击音量图标切换静音
   - 点击S图标切换独奏
   - 点击"重置颜色"恢复默认彩虹色

4. **配置持久化**:
   - 所有修改自动保存
   - 下次打开同一文件时恢复配置

## 配置文件示例

```json
{
  "Theme": { ... },
  "Visualization": { ... },
  "MidiFileConfigs": {
    "song.mid_12345": {
      "FileKey": "song.mid_12345",
      "FileName": "song.mid",
      "Tracks": [
        {
          "TrackNumber": 0,
          "TrackName": "Piano",
          "InstrumentNumber": 0,
          "InstrumentName": "Acoustic Grand Piano",
          "ColorHex": "#FFFF0000",
          "IsVisible": true,
          "IsMuted": false,
          "IsSolo": false
        },
        {
          "TrackNumber": 1,
          "TrackName": "Bass",
          "InstrumentNumber": 32,
          "InstrumentName": "Acoustic Bass",
          "ColorHex": "#FFFFA500",
          "IsVisible": true,
          "IsMuted": false,
          "IsSolo": false
        }
      ],
      "LastModified": "2026-05-19T10:30:00"
    }
  }
}
```

## 后续扩展建议

1. **可视化集成**: VisualizationService 可以被 MidiVisualization 页面使用，获取轨道颜色进行瀑布流渲染
2. **乐器选择**: 可以扩展为下拉列表选择乐器，并发送 Program Change MIDI消息
3. **轨道音量**: 添加音量滑块控制每个轨道的音量
4. **轨道重命名**: 允许用户自定义轨道名称
5. **预设配置**: 支持保存和加载颜色预设
