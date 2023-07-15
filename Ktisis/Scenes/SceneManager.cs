using System.Collections.Generic;

using JetBrains.Annotations;

using Dalamud.Game;
using Dalamud.Logging;

using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Game.Engine;
using Ktisis.Core.Singletons;
using Ktisis.Scenes.Objects;
using Ktisis.Scenes.Objects.Impl;

namespace Ktisis.Scenes; 

public class SceneManager : Singleton, IEventClient {
	// Scene

	public Scene? Scene;

	// GPose Event

	[UsedImplicitly]
	[Listener<GPoseEvent>]
	public void OnEnterGPose(object sender, bool isActive) {
		SelectCursor = null;
		SelectOrder.Clear();
		
		if (isActive) {
			PluginLog.Verbose("Entering gpose, setting up scene...");
			Scene = new Scene();
		} else {
			PluginLog.Verbose("Leaving gpose, cleaning up scene...");
			Scene = null;
		}
	}

	[UsedImplicitly]
	[Listener<FrameworkEvent>]
	public void OnFrameworkUpdate(Framework _) {
		Scene?.Update();
	}
	
	// Selection
	
	internal delegate void OnSelectChangedDelegate(SceneObject item, bool select);
	internal OnSelectChangedDelegate? OnSelectChanged;
	
	internal string? SelectCursor;

	internal readonly List<string> SelectOrder = new();

	internal void UserSelect(SceneObject item, SelectFlags flags) {
		var isRange = flags.HasFlag(SelectFlags.Range);
		if (isRange && SelectCursor != null) {
			// Handle item range selection
			AddSelection(item);
		} else {
			// Handle individual item selection
			if (flags.HasFlag(SelectFlags.Multiple)) {
				SetSelection(item, !item.Selected, false);
			} else {
				var select = item.Selected;
				var unselectMul = UnselectAll() > 1;
				SetSelection(item, unselectMul || !select, true);
			}
		}
	}

	internal bool AddSelection(SceneObject item, bool select = true) {
		item.SetSelected(select);
		select = item.Selected;

		var id = item.UiId;
		SelectOrder.Remove(id);
		if (select) {
			SelectOrder.Add(id);
		} else {
			var ct = SelectOrder.Count;
			SelectCursor = ct == 0 ? null : SelectOrder[ct - 1];
		}

		OnSelectChanged?.Invoke(item, select);

		return select;
	}

	internal bool SetSelection(SceneObject item, bool select = true, bool cull = true) {
		if (cull) UnselectAll();
		select = AddSelection(item, select);
		if (select)
			SelectCursor = item.UiId;
		return select;
	}

	internal int UnselectAll() {
		SelectOrder.Clear();
		SelectCursor = null;

		return Scene?.Iterate((item, val) => {
			if (item.Selected) val++;
			item.Unselect();
			return val;
		}) ?? 0;
	}
}