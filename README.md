[简体中文](README.md) | [English](README_EN.md)

# Lin Editor Tools

面向 Unity 工作流的轻量编辑器工具集，覆盖 Project、Hierarchy、SceneView、Inspector 和主工具栏。外部只依赖 `com.unity.nuget.newtonsoft-json`（`userData` 容器要用它做 JSON 树解析），设置页及包自带界面支持中英文切换。

> Package Manager 展示名：**Lin Editor Tools**。包 ID 与目录均为 `com.lin.editor-tools`。

## 安装

**内嵌**：把 `com.lin.editor-tools` 整个目录放进目标工程的 `Packages/`，Unity 自动识别。

**外部引用**：`Packages/manifest.json` 里加一条

```json
"com.lin.editor-tools": "file:<路径>/com.lin.editor-tools"
```

或在 Package Manager 里 *Add package from git URL / disk*。

最低要求 Unity 2021.3。本包已在 Unity 2021.3.45f2c1 编译验证；Unity 6 与团结 1.x 请按目标版本自行验证。

## 功能总览

| 功能 | 内容与入口 |
|---|---|
| Project 注释 | 资源和文件夹标题、颜色、说明与悬停提示；从 `.cs` 注释标识提取脚本描述 |
| Hierarchy 工具 | 场景物体注释；通过 `IHierarchyDrawable` 扩展 Hierarchy 行内绘制 |
| 主工具栏扩展 | `[ToolbarButton]`、`[ToolbarToggle]`、`IToolbarElement`，并内置设置入口与播放时的 TimeScale 滑条 |
| SceneView 标注 | `ShowInSceneGUI` 展示字段、属性和按钮；`DrawInSceneGUI` 执行自定义绘制回调 |
| Inspector 快捷操作 | `[Name]` 为组件右键菜单中的 GameObject 重命名提供目标名称 |
| 默认资源 Inspector | 文件夹与 `DefaultImporter` 资产的 Inspector：注释详情、自定义链接（URI）增删改与打开、文件夹子资源树浏览 |
| TextAsset 工具 | `[CustomEditor(typeof(TextAsset))]` 文本查看/编辑/保存；另含创建 TextAsset、批量转 UTF-8、Json 格式化三个菜单 |
| 文本编码导入 | `ChineseTextAssetImporter` 在导入时把非 UTF-8 的 `.cs/.txt/.lua/.json` 自动转成带 BOM 的 UTF-8（无开关） |
| 脚本模板 | 15 个带项目头注释/分区的 `新建脚本` 模板，首次装载自动复制到宿主 `Assets/ScriptTemplates`，菜单 `Lin/脚本模板` 可手动补 |
| 组件自动获取 | `[Get]` / `[GetInChild]` / `[GetInParent]` 标注的字段在脚本重载时自动填组件，菜单 `Lin/获取组件` 可手动触发 |
| 编辑器助手 | `Lin.Editor.Helper` 下的 `EditorGUIHelper` / `PathHelper` / `ScriptableObjectHelper` 三个静态助手 |
| 宏管理 | `Lin/Macros` 窗口：按 `BuildTargetGroup` 增删脚本宏（scripting define symbols），并落一份 `<平台>-Macros.json` 到项目根 |
| 脚本 TODO 收集 | `Lin/脚本/收集TODO` 扫描 `Assets/` 下的 `.cs` 生成 `Todos.md`；亦在每次脚本重载时自动收集 |
| 设置与本地化 | 注释样式、脚本描述标识、点击行为、导入策略、SceneView 叠加、TODO 收集和中英文切换；Project Settings 与包自带菜单均提供入口 |

### 资源与文件夹注释

在 Project 窗口里选中资源或文件夹 → 右键 **修改注释**，填标题/颜色/说明。列表视图把标题放在名称右侧并贴齐资源项右边缘；图标网格视图把标题放在名称上方一行并贴右。鼠标悬停可查看说明。再次点击标题即可编辑（单击还是双击由配置页决定）。

注释**存在资产自己的 `.meta` 里**（`AssetImporter.userData`，形如 `{"lin.annotation":{"title":"…","titleColor":"44AAFF","description":"…","createTime":"…","updateTime":"…"}}`）：

- 注释随资产一起进版本库：复制、移动、换工程都不会丢，也不再有一个中央文件当合并热点；
- 删除资产时注释一起消失，不需要清理孤儿条目（原先的「清除已删除资源的注释」菜单已随之移除）；
- `userData` 是共享字符串，别的工具写过的内容本包会原样保留，只动 `lin.annotation` 这一个 key；
- 代价：存注释会脏化该资产的导入器并触发一次重导入，**给 `.cs` 存注释还会引发一次脚本重编译**。批量给整个文件夹补注释时这点会很明显。
- 另外：中文标题在 `.meta` 里会被 YAML 写成 `\uXXXX` 转义（ASCII 标题才是字面量）。也就是随资产进版本库换来的是**可 diff、可合并**，不是肉眼可读。

> 旧版本（≤0.2.0）把注释集中在 `ProjectSettings/LinEditorAnnotation.json`。该文件已停用且不再读取，**不做自动迁移**：老注释需要重新填写，或自行按 GUID 把里面的 `items` 搬回各资产的 `.meta`。

### 默认资源 Inspector 与自定义链接

包用 `[CustomEditor(typeof(DefaultAsset), true)]` 接管了**文件夹和走 `DefaultImporter` 的资产**（`.dll` 这类没有专用导入器的东西）的 Inspector。选中它们时，Inspector 头部会给出：

- 有注释标题时，显示资产路径、着色标题、说明与创建/更新时间；
- 两枚按钮：**修改备注**（打开上文那个注释窗口）与**添加uri**（弹一个「标题 + URI」两行输入窗）；
- 已有的每条链接各占一段，配 **修改** / **访问**（`Application.OpenURL`）/ **移除** 三个按钮。

链接与注释住在同一个 `userData` 容器里，key 为 `lin.uris`，值是原生对象：

```jsonc
{"lin.annotation":{"title":"运行时合批", "...":"..."}, "lin.uris":{"设计稿":"https://example.com/a"}}
```

- 只有点输入窗的 **Save** 或按**移除**才写盘（`SaveAndReimport`），读的时候绝不写；
- 删掉最后一条链接会连 `lin.uris` 这个 key 一起摘掉，不留 `"lin.uris":{}`；
- 读路径也认「值是序列化好的字符串」这种双层写法（源自框架侧的旧实现），只是本包不再产出它。

选中**文件夹**时，Inspector 还会递归列出整棵子树：`.meta` 被跳过，每条显示图标、名字与该条自己的注释标题；点一条在 Project 窗口里 ping 它，再点同一条即打开该资源。

> 代价：这棵树是在 `OnEnable` 里用 `Directory.GetFiles/GetDirectories` 递归扫文件系统建起来的，每条还要查一次 importer 与缓存图标，并且**每次换选中都重算**。选中 `Assets/` 这类大目录会明显卡顿。要快就别在 Inspector 里停在大目录上（本包不为它提供开关或缓存）。这一段界面的文字是固定的中文，不在中英切换范围内。

### TextAsset Inspector 与文本编码导入

本包用 `[CustomEditor(typeof(TextAsset))]` 接管了**所有** `TextAsset`（`.txt`/`.json`/`.csv`/`.bytes` 等）的 Inspector：直接显示文件内容，可点 **修改** 进文本框编辑、**保存** 写回（保持原编码，会触发一次 `AssetDatabase.Refresh`）。另有三个菜单：

- `Assets/Create/TextAsset`：在当前选中的 Project 文件夹里新建一个空 `.txt`；
- `Lin/批量转换文件编码为UTF-8`：从 `Application.dataPath` 递归扫描所有 `.cs`/`.txt`（跳过 `Library`/`Temp`/`Logs` 与点开头目录），非 UTF-8 的就地转为带 BOM 的 UTF-8，带进度条可取消；
- `Assets/Json格式化`：把选中的 TextAsset 逐个按 JSON 解析并缩进写回（保持原编码，非 JSON 会记失败）。

> **破坏性变更**：装了本包的工程，所有 `TextAsset` 的 Inspector 归本包所有；与别的 TextAsset Inspector 包共存时的优先级由 Unity 的 CustomEditor 解析规则决定。

同时，`ChineseTextAssetImporter`（`AssetPostprocessor`）会在**每次资源导入**时检测 `.cs` / `.txt` / `.lua` / `.json`：非 UTF-8 的就地改写为**带 BOM** 的 UTF-8 并重新导入。

> **破坏性变更**：这是**无条件、无开关**的自动改写，会脏化源文件、可能引发脚本重编译。`.bytes` 视为二进制容器，不参与转换。决策与代价见 `docs/adr/0009`。

### 脚本注释

对 `.cs` 文件，会按配置页里的「描述标识」逐行提取一句说明，用脚本注释的颜色/粗体/斜体显示在标题前面，例如：

```csharp
// Description: 负责把主相机跟随逻辑收敛到这里
public class CameraFollow : MonoBehaviour { }
```

默认标识为 `Description: `、`Description：`、`功能说明: `（注意前两个分别是半角和全角冒号）。

**它只读文件开头的前 N 行（设置页「每个脚本扫描最大行数」，默认 100），并在其中找第一个匹配行，并不限定在文件头**——所以 N 行之内任何位置出现的 `// Description: ...`、甚至字符串字面量里的该文本都可能被抓出来；写在第 N 行之后的描述不会被读到。标识列表在配置页一行一个，不要留空行。

### 场景物体注释

Hierarchy 里选中物体 → 右键 **修改注释**（GameObject 菜单下）。数据存在该物体上的 `Description` 组件里，随场景保存、切场景不残留；`Description` 设了 `HideFlags.DontSaveInBuild`，不进构建产物。

### SceneView 标注特性

在静态成员或当前选中物体及其子物体的 `MonoBehaviour` 成员上使用 `ShowInSceneGUI` 和 `DrawInSceneGUI`：

```csharp
using Lin.Runtime.Attribute;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneViewHints : MonoBehaviour
{
    [ShowInSceneGUI]
    [SerializeField] private Vector3 point;

    [ShowInSceneGUI]
    private void ResetPoint() => point = transform.position;

    [DrawInSceneGUI]
    private void DrawHint()
    {
#if UNITY_EDITOR
        Handles.Label(transform.position, point.ToString());
#endif
    }
}
```

`ShowInSceneGUI` 支持字段、属性和无参方法：字段/属性显示当前值，无参方法显示为按钮。值会随每次 SceneView GUI 事件读取，属性 getter 应保持快速且无副作用。`DrawInSceneGUI` 标注无参方法，并在每次 SceneView GUI 事件调用（推荐返回 `void`）；保留 IMGUI/Handles 的布局与交互事件，因此回调应保持轻量。实例成员来自当前选中物体及其子物体（包含 inactive），静态成员会全局显示；静态分组标题随插件语言切换。

### 配置页

**Project Settings → Lin Editor Tools**，分段：资源注释、场景物体注释、脚本注释（含描述标识）、导入策略、SceneView 叠加、TODO 收集。字号限定 8–32；注释类改动会自动重绘 Project 窗口，导入策略 / SceneView 叠加 / TODO 收集三段改动后立即写盘。

9 个注释显示设置存在宿主的 `ProjectSettings/LinEditorTools.asset`（本包首次读到就自动生成，文本序列化、可 diff、进版本库、团队共享）。界面语言与主工具栏开关态是**本机本用户**偏好，仍存 `EditorPrefs`（key 前缀 `com.lin.editor-annotation/`），不进版本库——否则一个人切英文会替全组切。

升级注意：从 `EditorPrefs` 迁来的这 9 项**不做搬迁**，装了新版会看到默认值；同一份 `.asset` 以后新增字段时，老工程里缺的字段会被反序列化成 0/空而非默认值，包用 `settingsVersion` + `Migrate()` 归位。

### 主工具栏

包在主工具栏右侧放了一个「注释」按钮直达配置页。同时本包对外开放工具栏挂载点，使用方加自己的按钮不需要改包内代码：

```csharp
using Lin.Editor.Annotation;
using Lin.Editor.Toolbar.Element;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MyToolbarButtons
{
    // 第二个参数三选一：纹理资产路径、EditorGUIUtility 的内置图标名、或直接写按钮文字。
    // 内置图标名全是 ASCII，所以含中文的取值会被直接当作文字，不会去查图标（避免刷一条加载失败的警告）。
    [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "新建", "追加一个空场景")]
    private static void AddEmptyScene()
        => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

    // 方法签名必须是 static void(bool)；末位 key 用于把开关状态持久化到 EditorPrefs
    [ToolbarToggle(EAlign.Left, EVisibleMode.Both, "暂停", "挂起播放", "My.Pause")]
    private static void TogglePaused(bool on) => EditorApplication.isPaused = on;
}
```

> 两个 `using` 都不能省。少了 `Lin.Editor.Toolbar.Element`，`ToolbarButton` 会被 `using UnityEditor;` 里的同名 Unity 类型吃掉，报"'ToolbarButton' 不是特性类"。
>
> 命名空间刻意挂在 `Lin.Editor.Toolbar` 下而不是 `Lin.Editor.Annotation.Toolbar`：`Lin.Editor.*` 里的调用点可以写全限定形式 `Toolbar.Element.EAlign`。编译器先在**外层命名空间的成员**里找 `Toolbar`，才轮到 `using` 导进来的类型——放在 `Annotation` 下面时，`using UnityEditor.UIElements;` 带进来的公开类型 `Toolbar`（2021.3.45 与 2022.3.62t12 实测都存在）会把这个命名空间挤掉，报 `CS0117: 'Toolbar' 不包含 'Element' 的定义`。

或者实现 `IToolbarElement` 自行返回一个 `VisualElement`：

```csharp
using Lin.Editor.Toolbar.Element;

public class MySlider : IToolbarElement
{
    public EAlign align => EAlign.Middle;
    public EVisibleMode visibleMode => EVisibleMode.Runtime;
    public float width => 200;
    public VisualElement Create() => null;
}
```

`EVisibleMode` 决定元素在编辑态（`Editor`）、播放态（`Runtime`）还是两者都显示；切换播放模式时三个对齐容器会整体重建。包自带 `TimeScaleSlider` 作为该接口的示例，播放时出现在工具栏中部。

> 元素发现靠反射扫描所有名字含 `Editor` 的程序集，这是本包对外的扩展点，收紧成只扫本包会让上面两种写法失效。某个元素构造失败只会打一条错误日志并跳过它，不中断整个工具栏。

自研 Hierarchy 行内绘制则实现 `IHierarchyDrawable`：

```csharp
using Lin.Editor.Annotation;
using UnityEngine;

public class MyDrawer : IHierarchyDrawable
{
    public int drawPriority => 10;   // 越大越先绘制
    public float DrawInHierarchy(int instanceId, Rect drawablePoint)
    {
        // 在 drawablePoint 处绘制，返回占用的宽度以便下一个绘制器左移
        return 0f;
    }
}
```

### 按类名重命名 GameObject

```csharp
using Lin.Editor.Annotation;

[Name("玩家控制器")]
public class PlayerController : MonoBehaviour { }
```

之后在该组件的右键菜单里选 **修改GameObject的名字 / Rename GameObject**，物体名字改成特性里的值（未标特性则改成类名）。`NameAttribute` 位于 Runtime 程序集，因为使用方要把它标在自己的运行时脚本上。

### 脚本模板与组件自动获取

包自带 15 个 `新建脚本` 模板（MonoBehaviour / ScriptableObject / Class / Struct / Interface / 两种单例 / 若干 DOTS 类型），首次加载时复制到宿主 `Assets/ScriptTemplates`，重启编辑器后出现在 Project 右键 **Create → Scripting** 菜单；菜单 `Lin/脚本模板` 可手动触发同一复制（已存在的文件不覆盖）。**注意**：这是对宿主 `Assets/` 的写行为，且复制完成后会弹窗询问是否立即重启；模板生成的是**宿主**脚本，其中单例 / DOTS 模板引用了 `Lin.Runtime.*`，只对使用该框架的工程有意义。

`[Get]` / `[GetInChild("路径")]` / `[GetInParent]` 三个特性标注 MonoBehaviour 的字段后，本包会在**每次脚本重载**时扫描已加载场景里所有 MonoBehaviour，把为空的字段按标注自动填上组件；也可用菜单 `Lin/获取组件` 手动触发。字段非 public 时必须带 `[SerializeField]`，填不上会打一条错误日志。代价：这项扫描在每个装了本包的工程、每次重载都会跑一遍。

> 三个特性位于包 Runtime 程序集 `Lin.Runtime.ForEditorTools`（命名空间 `Lin.Runtime.Attribute`）。要标在自己运行时脚本上的工程，其程序集需引用该 asmdef（`autoReferenced: true`，Assembly-CSharp 默认可见）。

### 编辑器助手

`Editor/Helper/` 提供三个纯静态类，命名空间 `Lin.Editor.Helper`：

- `EditorGUIHelper.GetButtonStyle(Color)`：取一个着色按钮 `GUIStyle`（每次调用会新建一张 1×1 纹理，请勿在 `OnGUI` 每帧无节制调用）。
- `PathHelper`：`GetSelectedPathOrFallback()` 取当前 Project 选中文件夹路径；`FindObject<T>(name, includePackages)` 按类型（可选名字）找 ScriptableObject，找不到抛异常；`GetMonoScript(this Type)`；`RunProcess(path)` 用资源管理器打开路径。
- `ScriptableObjectHelper.Find<T>(includePackage)`：按类型找 ScriptableObject，找不到抛异常。

### 宏管理

`Lin/Macros` 打开宏管理器：顶部选 `BuildTargetGroup`，中间增删/勾选脚本宏。勾选结果写进该平台的 `PlayerSettings` scripting define symbols，同时在**项目根**落一份 `<平台>-Macros.json` 作为本包的记录（内置宏 `TEST` / `UNITY_2D` 会自动补进列表）。切换平台会立即以当前 define 为准合并列表并回写。

> 包依赖宏（HybridCLR / Luban / YooAsset / UniTask 等）已改由各 asmdef 的 `versionDefines` 随包自动增删，不走本窗口。

### 脚本 TODO 收集

`Lin/脚本/收集TODO`（以及每次脚本重载自动触发）扫描 `Assets/` 下所有 `.cs`，把 `TODO` 注释抽出来生成 `<项目根>/<输出目录名>/Todos.md`（默认目录名 `Markdown`，可在设置页「TODO 收集」段改）。忽略哪些文件夹前缀 / 程序集，同样在该段配置。

> **破坏性变更（相对 Learn 旧版）**：忽略表与输出目录名从宿主 `Assets/Settings/LinEditorSettings.asset` 换到 `ProjectSettings/LinEditorTools.asset`，旧值不搬迁，升级后回默认，需重填。Learn 的 Script Todo 窗口（`Lin/脚本/Script Todo List`）已改为读写同一份包设置。

### 中英文界面

在设置页右上角点击 **English** 或 **中文**。设置项、注释窗口、包自带工具栏文字和菜单项会切换语言，语言选择保存在本机 `EditorPrefs`，重启 Unity 后仍然保留。菜单会同时显示中英文项，当前语言项可用、另一项置灰。已打开的注释窗口会即时更新文字，正在编辑的注释内容不会被改写。

通过 `ToolbarButtonAttribute` 等扩展接口注册的使用方自定义按钮由使用方提供文字，不会被本包自动翻译。

## 程序集

| asmdef 名 | 平台 | 内容 |
|---|---|---|
| `Lin.Runtime.ForEditorTools` | 全平台 | `Description`、`NameAttribute`、`ShowInSceneGUIAttribute`、`DrawInSceneGUIAttribute`、`GetAttribute`、`GetInChildAttribute`、`GetInParentAttribute` |
| `Lin.Editor.Tools` | 仅编辑器 | SceneView 标注绘制器、Resource / Hierarchy / Toolbar / Inspector、脚本模板与组件自动获取、`Lin.Editor.Helper` 助手及其余全部编辑器功能 |

要在别的程序集里使用本包的类型，需要在该 asmdef 中显式引用对应 asmdef；`autoReferenced` 为 `true`，故 Assembly-CSharp 默认可见。

Scene GUI 两个 Attribute 保留 `Lin.Runtime.Attribute` 命名空间，但现在由 `Lin.Runtime.ForEditorTools` 程序集提供。原本显式依赖框架 `Lin.Runtime` 的自定义 asmdef 需要添加对此 package Runtime asmdef 的引用。（`Get` 系列三个 Attribute 同理：它们从宿主 `Lin.Runtime` 移入本包 Runtime 后，宿主的 `Lin.Runtime` 程序集需引用 `Lin.Runtime.ForEditorTools`。）

## 改名与兼容性说明

本次将包 ID 从 `com.lin.editor-annotation` 改为 `com.lin.editor-drawer`。安装路径或 manifest 使用旧 ID 的工程需要改用新 ID。为保留现有代码，C# 命名空间 `Lin.Editor.Annotation.*`、程序集名 `Lin.Editor.Annotation` / `Lin.Runtime.Annotation`、EditorPrefs 前缀 `com.lin.editor-annotation/` 均继续沿用旧标识。注释数据文件 `ProjectSettings/LinEditorAnnotation.json` 是例外：它自 Unreleased 起停用（见上文，不迁移）。

2026-09-27 起包 ID 再次更名为 `com.lin.editor-tools`（drawer 从未发版，两跳并作一跳）：使用 annotation 或 drawer 旧 ID 的工程统一改用新 ID。命名空间、程序集名与 EditorPrefs 前缀的兼容承诺不变；包设置文件为 `ProjectSettings/LinEditorTools.asset`。

## Unity 版本兼容性

代码保留了 Unity 6 的主工具栏（`MainToolbarWindow`）与 `EntityId` 分支。**该分支未在本地编译验证**——验证用的引用集来自团结 1.10.0（2022.3.62t12 基线），其中不存在 `UnityEngine.EntityId`。装进 Unity 6 工程前请自行确认这两个分支。

## 许可

未指定。
