[简体中文](README.md) | [English](README_EN.md)

# Lin Editor Tools

A lightweight toolkit for Unity workflows across the Project window, Hierarchy, Scene View, Inspector, and main toolbar. Its only external dependency is `com.unity.nuget.newtonsoft-json`, which the `userData` container needs for JSON parsing. The settings page and package-owned UI support Chinese and English.

> Package Manager display name: **Lin Editor Tools**. The package ID and folder are `com.lin.editor-tools`.

## Installation

**Embedded package:** place the entire `com.lin.editor-tools` folder under the target project's `Packages/` directory. Unity will detect it automatically.

**Local package reference:** add this entry to `Packages/manifest.json`:

```json
"com.lin.editor-tools": "file:<path>/com.lin.editor-tools"
```

You can also use **Package Manager → Add package from git URL / disk**.

The minimum supported Unity version is 2021.3. The package was compiled in Unity 2021.3.45f2c1. Unity 6 toolbar and `EntityId` branches have not been compiled locally; see [Compatibility](#compatibility). Validate Tuanjie 1.x against the specific target version.

## Feature overview

| Feature | Includes |
|---|---|
| Project annotations | Titles, colors, descriptions, and tooltips for assets and folders; script descriptions extracted from configured `.cs` comment markers |
| Hierarchy tools | Scene object annotations and the `IHierarchyDrawable` extension for inline row drawing |
| Main toolbar extensions | `[ToolbarButton]`, `[ToolbarToggle]`, `IToolbarElement`, a built-in settings shortcut, and a play-mode TimeScale slider |
| Scene View attributes | `ShowInSceneGUI` fields, properties, and method buttons; `DrawInSceneGUI` custom drawing callbacks |
| Inspector shortcut | `[Name]` supplies a target GameObject name for the component context-menu command |
| Default asset Inspector | Inspector for folders and `DefaultImporter` assets: annotation details, custom links (URIs) add/edit/remove/open, and a browsable subtree of folder contents |
| Settings and localization | Annotation styles, script markers, click behavior, and Chinese/English switching through Project Settings and package menus |

### Asset and folder annotations

In the Project window, select an asset or folder, right-click, and choose **Edit Annotation** (or **修改注释**). Enter a title, color, and description. In list view, the colored title appears to the right of the asset name and is aligned to the item's right edge; in icon grid view, it appears right-aligned on a separate line above the name. Hover to read the description. Click the annotation to edit it; choose single- or double-click behavior in the settings page.

Annotations are **stored in each asset's own `.meta`** (`AssetImporter.userData`, shaped like `{"lin.annotation":{"title":"...","titleColor":"44AAFF","description":"...","createTime":"...","updateTime":"..."}}`):

- They are versioned with the asset: copying, moving, or switching projects keeps them, and there is no central file to conflict over.
- Deleting an asset deletes its annotation, so there are no orphan entries to clean up (the old **Clear Deleted Asset Annotations** menu is gone).
- `userData` is a shared string; content written by other tools is preserved verbatim, and this package only touches the `lin.annotation` key.
- The trade-off: saving an annotation dirties that asset's importer and triggers a reimport, and **annotating a `.cs` file also triggers a script recompile**. This is noticeable when you batch-annotate a folder.
- Also: non-ASCII titles are written into the `.meta` as `\uXXXX` escapes by Unity's YAML layer (only ASCII titles stay literal). What you gain from versioning them with the asset is diffability and mergeability, not human readability.

> Versions up to 0.2.0 kept annotations in `ProjectSettings/LinEditorAnnotation.json`. That file is no longer read and is **not migrated automatically**: re-enter the annotations, or move the `items` entries back into each asset's `.meta` by GUID yourself.

### Default asset Inspector and custom links

The package takes over the Inspector of **folders and assets imported by `DefaultImporter`** (things without a dedicated importer, such as `.dll` files) through `[CustomEditor(typeof(DefaultAsset), true)]`. Selecting one shows, in the header:

- the asset path, colored title, description, and created/updated timestamps — only when the asset has an annotation title;
- two buttons: **修改备注** (opens the annotation window described above) and **添加uri** (a two-field Title + URI input window);
- every stored link as its own row, with **修改** / **访问** (`Application.OpenURL`) / **移除** buttons.

Links live in the same `userData` container as annotations, under the `lin.uris` key, as a native JSON object:

```jsonc
{"lin.annotation":{"title":"batch-runtime", "...":"..."}, "lin.uris":{"design-doc":"https://example.com/a"}}
```

- Nothing is written until you press **Save** in the input window or click **移除**; each such write calls `SaveAndReimport`. Reading never writes.
- Removing the last link drops the `lin.uris` key entirely instead of leaving `"lin.uris":{}`.
- The reader also accepts the older double-encoded shape (the value stored as a serialized string), it just no longer produces it.

Selecting a **folder** additionally lists its whole subtree in the Inspector: `.meta` files are skipped, every entry shows its icon, name, and its own annotation title; clicking an entry pings it in the Project window, and clicking the same entry again opens it.

> The cost: that tree is built in `OnEnable` by recursively walking the file system with `Directory.GetFiles/GetDirectories`, one importer and cached-icon lookup per entry, and it is **rebuilt on every selection change**. Selecting a large directory such as `Assets/` will visibly stall the Inspector — this package offers neither a toggle nor a cache for it. The text in this part of the UI is fixed Chinese and is not covered by language switching.

### Script annotations

For each configured marker, the package extracts the first matching line in a `.cs` file and displays it before the title, using the configured color, bold, and italic settings. If multiple markers match, each can contribute a description. For example:

```csharp
// Description: Keeps the main camera follow logic in one place
public class CameraFollow : MonoBehaviour { }
```

The default markers are `Description: `, `Description：`, and `功能说明: ` (the first two use half-width and full-width colons respectively).

The search reads only the first **Max scanned lines per script** lines (settings page, default 100) and uses the first matching line; it is not limited to the comment block. A matching string inside a string literal can also be picked up, while a marker placed below the configured line limit is missed. Configure one marker per line on the settings page, without blank lines.

### Scene object annotations

Select an object in the Hierarchy, right-click, and choose **Edit Annotation** (or **修改注释**) from the GameObject menu. Data is stored in a `Description` component on the object and is saved with the scene. The component uses `HideFlags.DontSaveInBuild | HideFlags.HideInInspector`, so it is hidden in the Inspector and stripped from builds.

### Scene View attributes

Use `ShowInSceneGUI` and `DrawInSceneGUI` on static members or members of `MonoBehaviour` components on the selected object and its children:

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

`ShowInSceneGUI` supports fields, properties, and parameterless methods. Fields and properties display their current values; methods appear as buttons. Values are read on every Scene View GUI event, so property getters should be fast and side-effect free. `DrawInSceneGUI` must mark a parameterless method and runs for each Scene View GUI event (`void` is recommended). The package preserves IMGUI and Handles layout and interaction events, so keep these callbacks lightweight. Instance members come from the selected object and its children, including inactive objects; static members are shown globally. The static section label follows the package language.

### Settings page

Open **Project Settings → Lin Editor Tools**. The page contains asset annotation, scene object annotation, and script annotation settings. Font size is limited to 8–32; changes refresh the Project window automatically.

Click **English** or **中文** at the top right to switch languages. The settings, annotation windows, built-in toolbar text, and package menu commands update immediately. The language choice is saved in local `EditorPrefs` and remains after Unity restarts. Native Unity menus retain both language entries: the active language is enabled and the other is disabled. Open annotation windows update their labels without changing the annotation text being edited.

The nine annotation display settings live in the host's `ProjectSettings/LinEditorTools.asset`, which this package creates on first access (text-serialized, diffable, versioned, shared with the team). The UI language and main-toolbar toggle states are **local to the current user and machine** and stay in `EditorPrefs` under the `com.lin.editor-annotation/` key prefix — otherwise one person switching to English would switch it for everyone.

Upgrade note: the nine migrated settings are **not** copied over from `EditorPrefs`; after installing this version you will see the defaults again.

Toolbar buttons registered by consumers through `ToolbarButtonAttribute` or other extension APIs keep the text supplied by the consumer; the package does not translate custom extension labels.

### Main toolbar extensions

The package adds a **Note** button on the right side of the main toolbar to open the settings page. It also exposes extension points so consumers can add buttons without changing the package:

```csharp
using Lin.Editor.Annotation;
using Lin.Editor.Toolbar.Element;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MyToolbarButtons
{
    // Replace the icon path with an existing texture asset path.
    [ToolbarButton(EAlign.Right, EVisibleMode.Editor, "Assets/Editor/Icons/NewScene.png", "Add an empty scene")]
    private static void AddEmptyScene()
        => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

    // Signature must be static void(bool). The final key persists the toggle state in EditorPrefs.
    [ToolbarToggle(EAlign.Left, EVisibleMode.Both, "Assets/Editor/Icons/Pause.png", "Pause playback", "My.Pause")]
    private static void TogglePaused(bool on) => EditorApplication.isPaused = on;
}
```

Both `using` directives are required. Without `Lin.Editor.Toolbar.Element`, Unity's `ToolbarButton` type from `UnityEditor` takes precedence and the compiler reports that `ToolbarButton` is not an attribute class. The third attribute argument can also be a built-in icon name or button text. ASCII strings are treated as icon names; for a literal label, use non-ASCII text or a valid texture path.

The namespace deliberately sits under `Lin.Editor.Toolbar` rather than `Lin.Editor.Annotation.Toolbar`: call sites inside `Lin.Editor.*` can then spell the qualified form `Toolbar.Element.EAlign`. The compiler looks for `Toolbar` among the members of the enclosing namespaces before it considers types brought in by `using` — under `Annotation` the public `Toolbar` type from `UnityEditor.UIElements` (present in both 2021.3.45 and 2022.3.62t12) pushed Lin's namespace aside and the call site failed with `CS0117: 'Toolbar' does not contain a definition for 'Element'`.
Alternatively, implement `IToolbarElement` and return a `VisualElement`:

```csharp
using UnityEngine.UIElements;
using Lin.Editor.Toolbar.Element;

public class MySlider : IToolbarElement
{
    public EAlign align => EAlign.Middle;
    public EVisibleMode visibleMode => EVisibleMode.Runtime;
    public float width => 200;
    public VisualElement Create() => null;
}
```

`EVisibleMode` controls whether an element is shown in Edit mode (`Editor`), Play mode (`Runtime`), or both. The package rebuilds the three alignment containers when the play mode changes. `TimeScaleSlider` is included as an example and appears in the middle while playing.

Elements are discovered by reflection across assemblies whose names contain `Editor`; this is the extension point used by the examples above. An element whose constructor fails is skipped with an error log, without interrupting the rest of the toolbar.

Consumers can also implement `IHierarchyDrawable` to draw custom content inline in the Hierarchy:

```csharp
using Lin.Editor.Annotation;
using UnityEngine;

public class MyDrawer : IHierarchyDrawable
{
    public int drawPriority => 10;   // Higher values draw first.
    public float DrawInHierarchy(int instanceId, Rect drawablePoint)
    {
        // Draw at drawablePoint and return the occupied width.
        return 0f;
    }
}
```

### Rename a GameObject by class name

```csharp
using Lin.Editor.Annotation;

[Name("Player Controller")]
public class PlayerController : MonoBehaviour { }
```

Choose **Rename GameObject** (or **修改GameObject的名字**) from that component's Inspector context menu. The object is renamed to the value in `NameAttribute`; without the attribute, its class name is used. `NameAttribute` is in the Runtime assembly so it can be applied to consumer runtime scripts.

## Assemblies

| Assembly definition | Platforms | Contents |
|---|---|---|
| `Lin.Runtime.Annotation` | All | `Description`, `NameAttribute`, `ShowInSceneGUIAttribute`, `DrawInSceneGUIAttribute` |
| `Lin.Editor.Annotation` | Editor only | Scene View attribute drawer and other editor functionality |

To use package types from another assembly, add an explicit reference to the corresponding asmdef. `autoReferenced` is `true`, so the default `Assembly-CSharp` assembly can see the types.

The two Scene View attributes keep the `Lin.Runtime.Attribute` namespace, but are now provided by the `Lin.Runtime.Annotation` assembly. Custom asmdefs that previously referenced the framework's `Lin.Runtime` assembly must add a reference to this package Runtime asmdef.

## Rename compatibility

The package ID changed from `com.lin.editor-annotation` to `com.lin.editor-drawer`; projects using the old ID in a manifest or local package path must switch to the new ID. To preserve existing code, the C# namespaces `Lin.Editor.Annotation.*`, assembly names `Lin.Editor.Annotation` / `Lin.Runtime.Annotation`, and the `com.lin.editor-annotation/` EditorPrefs prefix remain unchanged. The annotation data file `ProjectSettings/LinEditorAnnotation.json` is the exception: it was retired in Unreleased (see above; no migration).

As of 2026-09-27 the package ID changed again to `com.lin.editor-tools` (drawer was never released; both hops collapse into one): projects still on either old ID must switch to the new one. The namespace, assembly-name, and EditorPrefs-prefix compatibility promises are unchanged; the package settings file is `ProjectSettings/LinEditorTools.asset`.

## Unity compatibility

The package declares Unity 2021.3 as its minimum version and has been compiled in Unity 2021.3.45f2c1. Conditional code for Unity 6's main toolbar (`MainToolbarWindow`) and `EntityId` has not been compiled locally. Confirm those branches in a Unity 6 project before relying on them. Tuanjie 1.x should be validated against the target installation.

## License

Not specified.
