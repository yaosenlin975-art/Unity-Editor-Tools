# Changelog

## Unreleased

- 变更：设置页改**分级结构**并改名——主页路径 `Project/Lin Editor Tools` → `Project/Lin Framework`、标题 `Lin 编辑器工具 / Lin Editor Tools` → **`Lin Framework`**，主页内容（语言切换 + 全部分段）不变；另注册 6 个子页（资源注释 / 场景物体注释 / 脚本注释 / 导入策略 / SceneView 叠加 / TODO 收集）挂在主页节点下作快速跳转，点进去只画对应那一段。语言切换会同步刷新所有（子）页标题。菜单入口改为 `Lin/Lin Framework - 设置界面`。
- 新增：设置页所有「文件夹列表」（导入策略的 BGM / 语音文件夹、TODO 收集的忽略文件夹）的**添加**改为系统文件夹选择窗（只收工程内目录，转 `Assets/...` 相对路径）；「忽略的程序集」的**添加**改为可搜索、可移动缩放的工具窗（`Editor/Settings/ListPickerWindow.cs`，列出 `Assets` 下参与编译的 asmdef 名）。不再手打字符串。
- 变更：**导入策略「平台分组」四项改为包内固定默认**（`Editor/Asset/Importer/ImportPolicyDefaults.cs`）——覆盖平台 `{Standalone, Android, iPhone}`、跟随构建默认 `{WebGL, WeixinMiniGame}`、全局纹理上限 `2048`，PC 判定由 `pcPlatform` 字符串改为按 `BuildTargetGroup` 推导（`IsPCPlatform`）。依据 Unity 文档：桌面 BCn、移动 ASTC，WebGL 覆盖会破坏桌面浏览器的 BC 分支。`ToolsSettings_SO` 删除 `overriddenPlatforms` / `followBuildDefaultPlatforms` / `globalMaxTextureSize` / `pcPlatform` 四个字段，设置页删除「平台分组」段。**破坏性变更**：这四项不再可配，`PlatformTextureFormat.Get` 去掉 settings 形参。
- 变更：**音频「关键字」改为「文件夹」识别**（`bgmKeywords` / `voiceKeywords` → `bgmFolders` / `voiceFolders`，默认 `Assets/Arts/Audio/BGM`、`Assets/Arts/Audio/Voice`）。`AudioImportPolicy.Classify` 改为按资源路径前缀落在哪个文件夹下判定，`Resolve` 去掉 settings 形参。设置页「命名约定」段改为「文件夹识别」。`CurrentVersion` 4 → 5；v4 → v5 迁移只补文件夹默认，老关键字值不搬迁。


- 新增：**宏管理窗口**（`Lin/Macros`，`Editor/Macro/MacroManager.cs`）与**脚本 TODO 收集**（`Lin/脚本/收集TODO` + `[DidReloadScripts]` 自动收集，`Editor/ScriptTool/TodoCollector.cs`）从 Learn 移植进包。`MacroManager` 去掉 `#if NEWTONSOFT_JSON`（ADR-002）与 ZString；`TodoCollector` 的 `Log`/`IOHelper`/ZString 换成包内等价物（`Debug` / `Directory.CreateDirectory` / 插值 + `StringBuilder`）。两者均按原样保留副作用：`MacroManager` 写项目根 `<TargetGroup>-Macros.json` 并改 `PlayerSettings` define；`TodoCollector` 全量扫 `Assets/` 的 `.cs` 并把 `Todos.md` 写到项目根。
- 变更：**TODO 设置的三个字段（`docsOutputFolderName` / `todoIgnoreFolders` / `todoIgnoreAssemblies`）从宿主 `EditorSettings_SO` 并级进 `ToolsSettings_SO`（`CurrentVersion` 3 → v4，`Migrate` 只补新字段）**；门面 `EditorAnnotationSettings` 增 `DocsOutputFolderName` / `TodoIgnoreFolders` / `TodoIgnoreAssemblies` / `SaveTodoSettings()`，设置页增「TODO 收集」段（中英双语）。Learn 的 `EditorSettings_SO`、`EditorSettingsPage` 已删除这三个字段与绘制段，`ScriptTodoWindow` 改读包门面。**破坏性变更**：旧 `Assets/Settings/LinEditorSettings.asset` 里的忽略表 / 输出目录名不搬迁，升级后回默认，需到 `Project → Lin Editor Tools → TODO 收集` 重填（口径同 ADR-007 的 SceneView 叠加）。决策见 `docs/adr/0010`。
- 新增：**TextAsset Inspector 与编码导入器**从 Learn 的 `Lin.Editor/Asset` 移植进包（`Editor/Asset/TextAssetEditor.cs`、`Editor/Asset/Importer/ChineseTextAssetImporter.cs`）。`TextAssetEditor` 是四合一（`Assets/Create/TextAsset` 创建、`Lin/批量转换文件编码为UTF-8`、`Assets/Json格式化`、`[CustomEditor(typeof(TextAsset))]` 文本查看/编辑/保存），命名空间由 `Lin.Editor` 改到 `Lin.Editor.Asset`（与目录其余文件一致，调用方 `ChineseTextAssetImporter` 无需 using）；`ChineseTextAssetImporter` 原样。两文件按 ADR-002 去掉 `#if NEWTONSOFT_JSON` 守卫，直接 `using Newtonsoft.Json`。**破坏性变更（装包即生效）**：① 本包自此接管宿主**所有** `TextAsset`（含 `.json/.csv/.bytes`）的 Inspector；② `ChineseTextAssetImporter` 无条件把非 UTF-8 的 `.cs/.txt/.lua/.json` 就地改写为**带 BOM** 的 UTF-8 并触发重导入——这是对 ADR-003「导入副作用默认关」的知情例外。完整决策与代价见 `docs/adr/0009`。
- 新增：**脚本模板与自动获取组件**从 Learn 的 `Lin.Editor/ScriptTemplate` 移植进包（`Editor/ScriptTemplate/`）：15 个 `.cs.txt` 模板、`AutoCheckScriptTemplatesExist`（源目录改由 `PackageInfo.FindForAssembly(...).resolvedPath` 解析，embedded / file / git / registry 四种装法都能找到；找不到源目录时报错返回而不是静默复制 0 个文件）、`AutoGetEditor`（保留 `[DidReloadScripts]` 每次重编译扫场景自动填 `[Get]` 字段的行为；宿主 `ComponentExtensions.GetOrAddComponent(Type)` 就地内联，包不引宿主框架）。配套的 `GetAttribute` / `GetInChildAttribute` / `GetInParentAttribute` 三个**运行时**特性随 `AutoGetEditor` 从 Learn `Lin.Runtime/Attribute` 移进包 `Runtime/Attribute/`，沿用 `Lin.Runtime.Attribute` 命名空间。**破坏性变更**：宿主运行时程序集 `Lin.Runtime` 需引用 `Lin.Runtime.ForEditorTools` 才能继续使用这三个特性（Learn 已接好）；每次脚本重载的自动扫描是对装包工程编辑器环境的副作用，请知情。
- 新增：`Editor/Helper/` 三个通用编辑器助手从 Learn 的 `Lin.Editor/Helper` 移植进包，命名空间 `Lin.Editor.Helper` 不变：`EditorGUIHelper.GetButtonStyle`（着色按钮样式）、`PathHelper`（`GetSelectedPathOrFallback` / 按类型找 SO `FindObject<T>` / `GetMonoScript` / `RunProcess`）、`ScriptableObjectHelper.Find<T>`。
- 变更：Learn 的 `ImporterHelper` **不整份移植**——它的 `userData`/tag 两段上一轮已被 `ImporterUserData` + `ImporterTags` 吸收（且与容器语义冲突）。本次只补缺口：`ImporterUserData.GetUserData(this AssetImporter, string)`（原始串，无值给空串）、`ImporterUserData.FromGUID(string)`、`ImporterTagExtensions.GetUserTags(this AssetImporter)`（无标记返回空表而非 Learn 旧版的 null）。Learn 侧 `Generator`/`SpriteAtlasHelper` 两处调用点改指向包内扩展，`saveAndReimport: true` 的语义改为写入后显式 `SaveAndReimport()`（tag 写入仍先于重导入）。`SetUserData` / `ASSET_DESCRIPTION_KEY` 未搬（无调用点）。**破坏性变更**：外部代码若在用 Learn 的 `ImporterHelper` 需改指向包内扩展。
- 新增：**SceneView Renderer 信息叠加**（`SceneGUI/SceneRendererInfoDrawer`）从宿主工程 `Lin.Editor/Scene` 移植进包。选中带 Renderer 的物体时绘制包围盒并统计顶点/面数；开关与颜色并级为包设置 v3（`sceneRendererInfoEnabled` / `sceneRendererInfoColor`，v2→v3 只补空字段、alpha 0 视为未设置），设置页新增「SceneView 叠加」段。包程序集不引用宿主 `Lin.Editor` / `Lin.Runtime`：`CalculateObjectBounds` / `TryGetComponentInChildren` 改为包内私有实现，ZString 改普通插值（维持零外部依赖约定）。**破坏性变更**：存储由宿主 `Assets/Settings/LinEditorSettings.asset` 迁到 `ProjectSettings/LinEditorTools.asset`，叠加默认关——原工程里开着的话需到设置页重新勾选，颜色回默认不透明灰。
- 测试：`AssetImportSettingsTests` 补 v3 级两例（新字段默认、v2→v3 真值保留/空颜色补默认）；v0 收敛断言 2 → 3；`AssetImportFixtureTests.D1c` 的"版本号高于现行"从写死 3 改为 `CurrentVersion + 1` 自适应。
- 改名：包 ID 与展示名更新为 **Lin Editor Tools / `com.lin.editor-tools`**（本周期 annotation → drawer → tools 两连跳，均未发版，无需迁移）；保留既有命名空间 `Lin.Editor.Annotation.*`、程序集名及 EditorPrefs 前缀 `com.lin.editor-annotation/` 以兼容外部代码，设置文件定为 `ProjectSettings/LinEditorTools.asset`。GitHub remote 不在本次范围（仓库所有者另行操作）。
- 修复：调用点写全限定形式 `[ToolbarButton(Toolbar.Element.EAlign...)]` 时报 `CS0117: 'Toolbar' does not contain a definition for 'Element'`——`using UnityEditor.UIElements;` 带进来的公开类型 `Toolbar` 挤掉了 Lin 的 `Toolbar` 命名空间（2021.3.45 与 2022.3.62t12 实测都存在，不是版本差异）。工具栏命名空间由 `Lin.Editor.Annotation.Toolbar[.Element]` 改为 `Lin.Editor.Toolbar[.Element]`，`Lin.Editor.*` 里的调用点因此按外层命名空间解析，两个编辑器版本都通过。**破坏性变更**：外部代码需把 `using Lin.Editor.Annotation.Toolbar.Element;` 改成 `using Lin.Editor.Toolbar.Element;`。
- 修复：Project 窄分栏列表被误判为图标网格，导致注释下移到下一行。
- 调整：Project 注释绘制样式同步配置字号，统一富文本标签与样式的字号度量。
- 优化：Project 资源注释贴齐条目右侧；字号设置改为带预览的滑动条，描述标识改为可编辑列表。
- 新增：设置页「每个脚本扫描最大行数」（默认 100），脚本注释只读文件开头这么多行，避免把第三方包里的几千行脚本整份读进 Project 绘制。
- 修复：Project 窗口放大图标后，资源注释因仍按列表行计算坐标而被裁切；图标网格改在名称附近单独显示注释。
- 变更：**资源/文件夹注释不再存 `ProjectSettings/LinEditorAnnotation.json`，改存各资产 `.meta` 的 `userData`（key `lin.annotation`）**。注释随资产进版本库、随资产删除，"清除已删除资源的注释"菜单随之移除。**破坏性变更**：旧中央档不迁移、也不再读取，老注释需重新填写（或自行按 GUID 把 `items` 搬回各资产 `.meta`）。
- 变更：**包设置从 `EditorPrefs` 迁到 `ProjectSettings/LinEditorTools.asset`**（`AnnotationSettings` ScriptableObject，首次访问自动生成，文本序列化）。界面语言与主工具栏开关态仍留在 `EditorPrefs`。**破坏性变更**：9 个注释显示设置不做搬迁，升级后回到默认值；SO 里缺字段会被反序列化成 0/空，由 `settingsVersion` + `Migrate()` 归位。设置页每项的悬停提示改为指向该 `.asset`。
- 新增：`ImporterUserData` —— `userData` 的公开读写容器（`GetUserData<T>/SetUserData<T>/RemoveUserData`、`Get/Set/RemoveAnnotation`）。未知 key 逐字保留、读路径绝不写盘、删空最后一个 key 后整串置空。新增 `Tests/Editor`（EditMode，10 条）覆盖容器、编解码与设置默认值/迁移，含一条断言锁住与 ADR-004 冻结格式（`{"TAGS_KEY":"[\"ImporterModified\"]"}`）的字节一致性。**破坏性变更**：包自此依赖 `com.unity.nuget.newtonsoft-json`（下限 3.0.2），不再是零依赖包。
- 新增：**默认资源 Inspector** —— 从 Learn 移植 `DefaultAssetInspector` 与 `AssetMessageInputWindow`，补上 0.1.0 记的「未移植」欠账（决策见工作区 `docs/adr/0007`）。包自此接管文件夹与走 `DefaultImporter` 的资产的 Inspector：头部显示注释详情、**修改备注** / **添加uri** 与每条链接的修改/访问/移除，选中文件夹时递归浏览子资源树（点一条 ping，再点同一条打开）。框架的 `Factory<T>` 对象池改用 Unity 自带的 `UnityEngine.Pool.ObjectPool<T>`（CoreModule 提供，零新增依赖），`AssetSummaryArchiver` 的两处读取改走 `importer.GetAnnotation()`。**破坏性变更**：装了本包的工程，`DefaultAsset` 的 Inspector 归本包所有。**已知代价**：那棵子树在 `OnEnable` 里递归扫文件系统且每次换选中重算，选中大目录会明显卡顿，不提供开关与缓存。
- 新增：`ImporterUserData` 的 URI 层——`GetUris/SetUri/RemoveUri`，key 为 `lin.uris`，值是原生 JSON 对象；表删空时连 key 一起摘掉，读路径另兼容「值被序列化成字符串」的旧双层形状。`Tests/Editor` 补 U1–U3（三线共存字节断言、往返、旧形状可读）。Learn 全仓 `.meta` 实测没有 URI 存量，故不沿用框架侧的 `"ASSET_URIS_KEY"` 字面量与双层编码。
- 修正（随移植一并带上）：树节点归还对象池时清掉 `isSelected`，否则上一条选中留下的灰底会随池漂到别的资源上；`Directory.GetFiles/GetDirectories` 产出的 Windows 反斜杠路径统一成正斜杠后才交给 `AssetImporter.GetAtPath` / `AssetDatabase.GetCachedIcon` / `PingObject`；**移除**一条链接后立刻刷新，不再要等下次重开 Inspector；`AssetMessageInputWindow` 删掉从不赋值的 `stringSave` 分支，按钮文案 `SaveAsync` 改为 `Save`（它本来就是同步保存）。原样保留的两处 GUI 副作用没动：这段绘制里 `GUI.enabled = true`（Inspector 锁定时树仍可点）与 `EditorGUIUtility.SetIconSize(16)` 事后不还原。
- 修复：`AssetModifier` 纹理/音频两处报 `CS0103: The name 'Available' does not exist in the current context`——把内联的「平台不可用则告警跳过」判断收拢成 `Available(...)` 调用时漏了定义该辅助方法。补上私有静态实现（逐平台过 `PlatformTextureFormat.IsPlatformAvailable`，不可用的 `Warn` 后跳过），警告文案与原内联版本逐字一致。
- 修复：`AssetImportSettingsTests` 仍引用已删的 `importPolicyVersion` / `AllPlatforms` 报 CS1061 ×4——断言同步到现行形状：默认值测试锁九个并入字段、v1 迁移测试不再断言已删的第二个版本字段、「两组互斥且齐全」改用 `OverriddenPlatformNames` + `FollowBuildDefaultPlatformNames` 两个视图枚举；`AssetImportPoliciesTests` 类注释的成员清单一并同步。

## 0.2.0 (2026-09-22)

**改名**：`com.lin.editor-toolbox` → `com.lin.editor-annotation`，命名空间 → `Lin.Editor.Annotation.*`。旧版本未被任何工程引用，无迁移成本。

- 新增：Project 窗口资源与文件夹注释，右键 **修改注释** 编辑，标题按所选颜色显示在名字右侧。
- 新增：脚本注释——从 `.cs` 源码里按可配置的标识（默认 `Description:` 等三项）提取一句说明并着色显示。
- 新增：**Project Settings → Lin Editor Annotation** 配置页，分资源注释 / 场景物体注释 / 脚本注释三段；主工具栏右侧加「注释」按钮直达。
- 新增：注释数据落在宿主工程 `ProjectSettings/LinEditorAnnotation.json`，用 `JsonUtility` 读写。
- 修复：清理已删除资源注释的菜单原本用 `File.Exists` 判断资源是否还在，而它对目录恒为 false——跑一次会把所有文件夹注释删掉。现在同时判目录。
- 变更：脚本注释现在也吃 `注释字体大小`（原先只有导入器标题吃），一个旋钮同时管两处。
- 变更：两个注释窗口共用一份 `Editor/UI/AnnotationWindow.uss`（原先两份逐字节相同）。

## 0.1.0 (2026-09-22)

首个版本，从 `Learn` 工程的 `Assets/Plugins/Lin/Editor/{Inspector,Toolbar,Hierarchy}` 移植而来。

- 新增：主工具栏挂载点，支持 `[ToolbarButton]` / `[ToolbarToggle]` 特性与 `IToolbarElement` 两种扩展方式。
- 新增：Hierarchy 行内物体注释、注释编辑窗口，与 `IHierarchyDrawable` 扩展接口。
- 新增：`CONTEXT/MonoBehaviour → 修改GameObject的名字`，配合 `[Name]` 特性使用。
- 零外部依赖：移除对 ZLinq、Cysharp.Text 及项目私有 `Lin.*` 程序集的全部引用。
- 未移植：`InitializerEditor`（绑定项目框架的运行时引导体）；`DefaultAssetInspector`（依赖对象池与 URI 编辑，后者需要 Newtonsoft.Json）。
