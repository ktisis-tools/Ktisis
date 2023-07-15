using System;
using System.Linq;

using Ktisis.Scenes;
using Ktisis.Scenes.Objects;
using Ktisis.Interface.Overlay;
using Ktisis.Scenes.Objects.Impl;

namespace Ktisis.Interface.SceneUi;

internal class SceneRender : IDisposable {
	// Dependency access

	private readonly GuiOverlay Overlay;
	private readonly SceneManager SceneManager;

	// Constructor

	internal SceneRender(GuiOverlay overlay, SceneManager scene) {
		Overlay = overlay;
		SceneManager = scene;

		Selected ??= FindPrimarySelect();
		SceneManager.OnSelectChanged += OnSelectChanged;
	}

	// Draw all objects in scene

	private Gizmo? Gizmo;

	private bool IsPrimaryUsed;

	internal void Draw(Gizmo? gizmo) {
		Gizmo = gizmo;

		IsPrimaryUsed = false;

		var scene = SceneManager.Scene;
		scene?.Children.ForEach(DrawItem);

		if (Selected != null && !IsPrimaryUsed) {
			var prev = Selected!;
			var value = FindPrimarySelect();
			Selected = value != prev ? value : null;
		}
	}

	private void DrawItem(SceneObject item) {
		if (item is IOverlay ov) {
			if (ov.CanDraw) return;
			ov.Draw();
		}

		if (item is IManipulable manip && Gizmo is Gizmo gizmo) {
			var primary = item.UiId == Selected;
			IsPrimaryUsed |= primary;
			if (item.Selected) {
				var compose = manip.ComposeMatrix();
				if (compose != null) {
					gizmo.Manipulate(
						compose.Value,
						manip.SetMatrix,
						primary
					);
				}
			} else if (primary) {
				Selected ??= FindPrimarySelect();
			}
		}

		item.Children.ForEach(DrawItem);
	}

	// Selection

	private string? Selected;

	private string? FindPrimarySelect() {
		var max = -1;
		return Selected ??= SceneManager.Scene?
			.GetAll()
			.Where(item => item is IManipulable)
			.LastOrDefault(item => {
				var index = SceneManager.SelectOrder.IndexOf(item!.UiId);
				var result = index > max;
				if (result) max = index;
				return result;
			}, null)?
			.UiId;
	}

	private void OnSelectChanged(SceneObject item, bool select) {
		if (select) {
			if (item is IManipulable)
				Selected = item.UiId;
		} else if (item.UiId == Selected) {
			Selected = null;
			FindPrimarySelect();
		}
	}

	// Disposal

	private bool IsDisposed;

	public void Dispose() {
		if (IsDisposed) return;
		SceneManager.OnSelectChanged -= OnSelectChanged;
		GC.SuppressFinalize(this);
		IsDisposed = true;
	}

	~SceneRender() => Dispose();
}
