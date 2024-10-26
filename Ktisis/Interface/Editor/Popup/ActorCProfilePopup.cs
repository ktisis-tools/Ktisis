using System;
using System.Collections.Generic;
using System.Linq;

using GLib.Lists;

using ImGuiNET;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interop.Ipc;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Popup;

public class ActorCProfilePopup : KtisisPopup {
	private readonly IEditorContext _ctx;
	private readonly ActorEntity _entity;
	private readonly CustomizeIpcProvider _ipc;
	private readonly ListBox<IPCProfileDataTuple> _list;

	public ActorCProfilePopup(
		IEditorContext ctx,
		ActorEntity entity
	) : base("##ActorCProfilePopup") {
		this._ctx = ctx;
		this._entity = entity;
		this._ipc = ctx.Plugin.Ipc.GetCustomizeIpc();
		this._list = new ListBox<IPCProfileDataTuple>("##CProfileList", this.DrawItem);
	}

	private bool _isOpening = true;
	private List<IPCProfileDataTuple> _profiles = [];
	private (Guid Id, string Name) _current = (Guid.Empty, string.Empty);

	protected override void OnDraw() {
		if (!this._entity.IsValid || !this._ctx.Plugin.Ipc.IsCustomizeActive) {
			this.Close();
			return;
		}
		
		if (this._isOpening) {
			this._isOpening = false;
			this._profiles = this._ipc.GetProfileList().ToList();
			Ktisis.Log.Info($"Fetched {this._profiles.Count} profiles");
		}

		var currentId = this._ipc.GetActiveProfileId(this._entity.Actor.ObjectIndex).Id;
		if (currentId != null) {
			foreach (var profile in this._profiles) {
				if (profile.UniqueId != currentId) continue;
				this._current = (profile.UniqueId, profile.Name);
			}
		}
		
		ImGui.Text($"Assigning collection for {this._entity.Name}");
		ImGui.TextDisabled($"Currently set to: {this._current.Name}");
		
		if (this._list.Draw(this._profiles, out var selected)) {
			var profile = this._ipc.GetProfileByUniqueId(selected.UniqueId);
			if (profile.Data != null) this.SetProfile(profile.Data);
		}
	}

	private void SetProfile(string data) {
		var actorId = this._entity.Actor.ObjectIndex;
		this._ipc.DeleteTemporaryProfile(actorId);
		this._ipc.SetTemporaryProfile(actorId, data);
		if (!this._ctx.Posing.IsEnabled) this._entity.Redraw();
	}
	
	private bool DrawItem(IPCProfileDataTuple item, bool _) => ImGui.Selectable(item.Name, item.UniqueId == this._current.Id);
}
