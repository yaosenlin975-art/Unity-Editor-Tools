# Changelog

## Unreleased

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
