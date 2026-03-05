# PurrUI

A Unity UI framework by the PurrNet team featuring procedural UI rendering, view management, and UI pooling.

<img width="1034" height="491" alt="image" src="https://github.com/user-attachments/assets/d3abc18b-0a5f-4752-9092-c6c233bfbf2d" />

## Features

- **Procedural UI** - SDF-based rendering with `RectangleGraphic` and `GlowGraphic` (for glow/shadows)
- **View Management** - `ViewStack` and `ViewCollection` for managing UI views with transition support

## Installation

### Unity Package Manager (Git URL)

**Latest release:**
```
https://github.com/PurrNet/PurrUI.git?path=Assets/PurrUI#release
```

**Latest development:**
```
https://github.com/PurrNet/PurrUI.git?path=Assets/PurrUI#dev
```

# Procedural UI

<img width="553" height="718" alt="image" src="https://github.com/user-attachments/assets/fcb56d3e-8791-4d87-800b-6373a0d6d4f9" />

<img width="542" height="222" alt="image" src="https://github.com/user-attachments/assets/e02617f5-d104-4122-86c6-eaeb97382f1d" />

# View System Documentation

PurrUI provides a stack-based view navigation system for Unity. Views are managed through a `ViewStack` that handles pushing, popping, ordering, visibility, and animated transitions.

All classes live in the `PurrNet.UI` namespace.

## Core Concepts

| Class | Type | Purpose |
|---|---|---|
| `ViewStack` | MonoBehaviour | Navigation controller that manages a stack of views |
| `MonoView` | MonoBehaviour | Base class for all views |
| `ViewCollection` | ScriptableObject | Asset that holds references to view prefabs |
| `ViewTransitions` | Static class | Built-in transition animations (fade, slide, etc.) |

## Setup

### 1. Create a ViewCollection

Right-click in your Project window and select **Create > PurrNet > View Collection**.

A `ViewCollection` is a ScriptableObject that stores references to your view prefabs. It has two modes:

- **Auto Generate** (default): Assign folders to watch and the collection automatically discovers all `MonoView` prefabs inside them. It refreshes whenever assets change.
- **Manual**: Disable `autoGenerate` and drag prefabs into the `views` array yourself.

<img width="551" height="353" alt="image" src="https://github.com/user-attachments/assets/74ae1e77-79d3-4248-bc1f-7f54b3332b05" />

### 2. Create View Prefabs

Each view is a prefab with a `MonoView` (or subclass) component on its root GameObject. `MonoView` automatically requires a `Canvas` and `CanvasGroup`, so those are added for you.

<img width="553" height="567" alt="image" src="https://github.com/user-attachments/assets/62c9f210-9c3e-4415-b665-43528ced7622" />

### 3. Add a ViewStack to Your Scene

Add a `ViewStack` component to a GameObject. In the inspector, configure:

- **Parent**: The transform where instantiated views will be parented (defaults to the ViewStack's own transform).
- **Prefabs**: Your `ViewCollection` asset.
- **Push On Start** (optional): A view to automatically push when the scene starts.
- **Order Offset**: An offset applied to canvas sorting order values.

<img width="552" height="133" alt="image" src="https://github.com/user-attachments/assets/cc3e6703-4860-4a38-9dd9-8f0c37cca6ae" />

## Creating Custom Views

Subclass `MonoView` to create your own views:

```csharp
using PurrNet.UI;
using UnityEngine;

public class ProfileView : MonoView
{
    [SerializeField] private TMPro.TMP_Text _username;
    [SerializeField] private TMPro.TMP_Text _bio;

    public void Setup(string username, string bio)
    {
        _username.text = username;
        _bio.text = bio;
    }
}
```

### The `Setup` Convention

We follow a convention of adding a `Setup` method to views that need initialization data. This is **not enforced** by the base class, it's simply a pattern. The idea is that `Setup` is called immediately after pushing:

```csharp
_stack.Push<ProfileView>().Setup("Jane", "Hello world!");
```

All built-in views provided by PurrUI follow this pattern. You are encouraged to do the same for consistency, but it is not required.

## Pushing and Popping Views

### Pushing

There are two ways to push a view onto the stack:

```csharp
// Generic push: looks up the prefab by type from the ViewCollection
var view = _stack.Push<DialogView>();

// Direct prefab push
var view = _stack.Push(someViewPrefab);
```

When a view is pushed:
1. The current top view (if any) is moved to the background (raycasts disabled).
2. The prefab is instantiated under the configured parent transform.
3. The view is initialized and assigned a sorting order.
4. If the view defines an enter transition, it plays (raycasts are blocked until it completes).

### Popping

```csharp
// Pop the top view
_stack.Pop();

// Pop a specific view instance (regardless of position in the stack)
_stack.Pop(viewInstance);

// From inside a view, call CloseMe() (can be wired to a button in the inspector)
CloseMe();
```

When a view is popped:
1. If the view defines an exit transition, it plays before the view is destroyed.
2. The new top view is moved to the foreground (raycasts re-enabled, `OnBecomeForeground` called).

### Other Stack Operations

```csharp
// Remove all views from the stack (exit transitions play for each)
_stack.Clear();

// Move an existing view to the top of the stack
_stack.MoveToTop(viewInstance);
_stack.MoveToTop<DialogView>();
```

## Transitions

Override `OnEnterTransition` and `OnExitTransition` in your `MonoView` subclass to define animated transitions. They return `IEnumerator` coroutines.

```csharp
public class MyView : MonoView
{
    [SerializeField] private RectTransform _content;

    protected override IEnumerator OnEnterTransition()
    {
        return ViewTransitions.FadeIn(this);
    }

    protected override IEnumerator OnExitTransition()
    {
        return ViewTransitions.FadeOut(this);
    }
}
```

### Built-in Transitions

`ViewTransitions` provides ready-to-use animations. All use smoothstep easing and unscaled time (unaffected by `Time.timeScale`). Default duration is `0.2s`.

**Fade:**
```csharp
ViewTransitions.FadeIn(view, duration)
ViewTransitions.FadeOut(view, duration)
ViewTransitions.Fade(canvasGroup, fromAlpha, toAlpha, duration)
```

**Slide:**
```csharp
ViewTransitions.SlideFromLeft(content, duration)
ViewTransitions.SlideToLeft(content, duration)
ViewTransitions.SlideFromRight(content, duration)
ViewTransitions.SlideToRight(content, duration)
ViewTransitions.SlideFromTop(content, duration)
ViewTransitions.SlideToTop(content, duration)
ViewTransitions.SlideFromBottom(content, duration)
ViewTransitions.SlideToBottom(content, duration)
```

Slide methods operate on a `RectTransform` (typically a child content container, not the root view), using `anchoredPosition` and the rect's own width/height to calculate offsets.

### Combining Transitions

Use `ViewTransitions.Parallel` to run multiple transitions at the same time:

```csharp
protected override IEnumerator OnEnterTransition()
{
    var fade = ViewTransitions.FadeIn(this);
    var slide = ViewTransitions.SlideFromLeft(_content);
    return ViewTransitions.Parallel(fade, slide);
}

protected override IEnumerator OnExitTransition()
{
    var fade = ViewTransitions.FadeOut(this);
    var slide = ViewTransitions.SlideToRight(_content);
    return ViewTransitions.Parallel(fade, slide);
}
```

## View Lifecycle & Visibility

- **Sorting Order**: Each view in the stack is assigned an incrementing sorting order (plus `_orderOffset`), so views higher in the stack render on top.
- **Raycasts**: Only the top view receives raycasts. Views below the top have raycasts disabled.
- **Cull Windows Behind**: If a view has `cullWindowsBehind` enabled, all views below it in the stack have their `Canvas` disabled entirely. Useful for fullscreen views where nothing behind them is visible.
- **OnBecomeForeground**: Override this virtual method in your `MonoView` subclass to react when the view returns to the top of the stack (e.g., after the view above it is popped).
