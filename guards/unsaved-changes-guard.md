# Unsaved Changes Guard

## Overview

`UnsavedChangesGuard` is an abstract base class that prevents navigation away from a page when there are unsaved changes, giving the user a chance to confirm whether to discard them.

---

## Class Declaration

```csharp
public abstract class UnsavedChangesGuard : INavigationGuard
```

---

## Abstract Members to Implement

### HasUnsavedChangesAsync

Returns whether the current page or ViewModel has unsaved changes:

```csharp
protected abstract Task<bool> HasUnsavedChangesAsync(CancellationToken cancellationToken);
```

### ConfirmDiscardAsync

Presents a confirmation prompt asking the user whether to discard unsaved changes. Return `true` to allow navigation (discard changes), or `false` to cancel navigation (keep changes):

```csharp
protected abstract Task<bool> ConfirmDiscardAsync(
	NavigationGuardContext context,
	CancellationToken cancellationToken);
```

---

## Pipeline Behavior

1. `HasUnsavedChangesAsync` is called.
2. If there are no unsaved changes → `NavigationGuardResult.Allow()`.
3. If there are unsaved changes → `ConfirmDiscardAsync` is called.
4. If the user confirms → `NavigationGuardResult.Allow()`.
5. If the user cancels → `NavigationGuardResult.Reject("Navigation cancelled: unsaved changes present.")`.

---

## Example Implementation

```csharp
// Guards/EditProfileUnsavedChangesGuard.cs
public sealed class EditProfileUnsavedChangesGuard : UnsavedChangesGuard
{
	private readonly ICurrentPageProvider _currentPage;

	public EditProfileUnsavedChangesGuard(ICurrentPageProvider currentPage)
		=> _currentPage = currentPage;

	protected override Task<bool> HasUnsavedChangesAsync(CancellationToken cancellationToken)
	{
		// Check if the current ViewModel has unsaved changes
		var vm = _currentPage.Current?.BindingContext as IUnsavedChangesSource;
		return Task.FromResult(vm?.HasUnsavedChanges == true);
	}

	protected override async Task<bool> ConfirmDiscardAsync(
		NavigationGuardContext context,
		CancellationToken cancellationToken)
	{
		return await Shell.Current.DisplayAlert(
			"Unsaved Changes",
			"You have unsaved changes. Discard them and leave?",
			"Discard",
			"Stay");
	}
}
```

Register globally — the guard short-circuits when the current ViewModel does not implement `IUnsavedChangesSource`:

```csharp
builder.Services.AddSingleton<INavigationGuard, EditProfileUnsavedChangesGuard>();
```

---

## IBackAware Alternative

For simpler scenarios where you only need to intercept the hardware back button, implement `IBackAware` on the ViewModel instead:

```csharp
public sealed partial class EditProfileViewModel : IBackAware
{
	private bool _hasUnsavedChanges;

	public async Task<bool> CanGoBackAsync()
	{
		if (!_hasUnsavedChanges)
			return true;

		return await Shell.Current.DisplayAlert(
			"Unsaved Changes",
			"Discard changes and go back?",
			"Discard", "Stay");
	}

	public Task OnBackAsync() => Task.CompletedTask;
}
```

`IBackAware` is evaluated by `GoBackAsync()` only, while `UnsavedChangesGuard` fires for **all** navigation away from the page (forward and back).

---

## Related Pages

- [Guards Overview](overview.md)
- [Typed Navigation — IBackAware](../navigation/typed-navigation.md#ibackaware--intercepting-back-navigation)
- [Custom Guards](custom-guards.md)
