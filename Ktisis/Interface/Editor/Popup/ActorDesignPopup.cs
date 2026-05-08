using System;
using System.Collections.Generic;
using System.Linq;

using GLib.Lists;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interop.Ipc;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Popup;

public class ActorDesignPopup : KtisisPopup {
	private readonly IEditorContext _ctx;
	private readonly ActorEntity _entity;
	private readonly GlamourerIpcProvider _ipc;
	private readonly ListBox<KeyValuePair<Guid, string>> _list;
	
	public ActorDesignPopup(
		IEditorContext ctx,
		ActorEntity entity
	) : base("##ActorDesignPopup") {
		this._ctx = ctx;
		this._entity = entity;
		this._ipc = ctx.Plugin.Ipc.GetGlamourerIpc();
		this._list = new ListBox<KeyValuePair<Guid, string>>("##DesignList", this.DrawItem);
	}

    // todo: even possible to keep a current record of applied design? glamourer ipc doesnt surface
	private (Guid Id, string Name) _current = (Guid.Empty, string.Empty);

	protected override void OnDraw() {
		if (!this._entity.IsValid || !this._ctx.Plugin.Ipc.IsGlamourerActive) {
			this.Close();
			Ktisis.Log.Info("Stale, closing.");
			return;
		}

		ImGui.Text($"Apply design for {this._entity.Name}");

		var list = this._ipc.GetDesignList().OrderBy(x => x.Value).ToList();
		if (this._list.Draw(list, out var selected)) {
			if (this._ipc.ApplyDesignToObject(this._entity.Actor, selected.Key))
				this._entity.Redraw();
		}
	}

	private bool DrawItem(KeyValuePair<Guid, string> item, bool _) => ImGui.Selectable(item.Value, item.Key == this._current.Id);
}
