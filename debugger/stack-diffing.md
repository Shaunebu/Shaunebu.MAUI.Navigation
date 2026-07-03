# Stack Diffing

## Overview

The `NavigationStackDiffEngine` computes a before/after diff of the navigation and modal stacks for each recorded operation. Diffs are attached to session records and replay frames, making it easy to see exactly how each navigation mutated the stack.

---

## INavigationStackDiffEngine

```csharp
public interface INavigationStackDiffEngine
{
	NavigationStackDiff Diff(
		NavigationStackSnapshot before,
		NavigationStackSnapshot after);
}
```

---

## NavigationStackDiff

A diff contains the added, removed, and preserved routes:

```csharp
public sealed class NavigationStackDiff
{
	public IReadOnlyList<string> Added    { get; init; }  // routes pushed
	public IReadOnlyList<string> Removed  { get; init; }  // routes popped
	public IReadOnlyList<string> Preserved { get; init; } // routes unchanged
	public bool                  IsEmpty  { get; }        // no changes
}
```

---

## Enabling Stack Diffing

```csharp
builder.Services.UseNavigationDebugger(opts =>
{
	opts.EnableStackDiffing = true; // default: true
});
```

When enabled, the session recorder captures a stack snapshot before and after each operation and uses the diff engine to compute the change.

---

## Reading Diffs From Replay Frames

Diffs are available on each `NavigationReplayFrame`:

```csharp
var frame = await _replayer.StepForwardAsync();
var diff  = frame.Diff;

Console.WriteLine($"Added:    {string.Join(", ", diff.Added)}");
Console.WriteLine($"Removed:  {string.Join(", ", diff.Removed)}");
```

---

## Using the Diff Engine Directly

In tests or custom diagnostics, resolve `INavigationStackDiffEngine` and compute diffs manually:

```csharp
var before = new NavigationStackSnapshot
{
	NavigationStack = ["auth/login"],
	ModalStack      = [],
	CurrentRoute    = "auth/login",
	CurrentFlow     = "Auth"
};

var after = new NavigationStackSnapshot
{
	NavigationStack = ["main/home"],
	ModalStack      = [],
	CurrentRoute    = "main/home",
	CurrentFlow     = "Main"
};

var diff = _diffEngine.Diff(before, after);

Assert.Equal(["main/home"], diff.Added);
Assert.Equal(["auth/login"], diff.Removed);
Assert.Empty(diff.Preserved);
```

---

## Related Pages

- [Debugger Overview](overview.md)
- [Session Recording](session-recording.md)
- [Timeline Replay](timeline-replay.md)
