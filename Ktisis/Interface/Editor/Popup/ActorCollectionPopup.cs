using System.Linq;

using GLib.Lists;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Interface.Types;
using Ktisis.Interop.Ipc;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Popup;

public class ActorCollectionPopup : KtisisPopup {
	private readonly IEditorContext _context;
	private readonly ActorEntity _entity;
	private readonly PenumbraIpcProvider _ipc;
	private readonly ListBox<string> _list;
	
	public ActorCollectionPopup(
		IEditorContext context,
		ActorEntity entity
	) : base("##ActorCollectionPopup") {
		this._context = context;
		this._entity = entity;
		this._ipc = context.Ipc.GetPenumbraIpc();
		this._list = new ListBox<string>("##CollectionList", this.DrawItem);
	}

	private string _current = string.Empty;

	protected override void OnDraw() {
		if (!this._entity.IsValid || !this._context.Ipc.IsPenumbraActive) {
			this.Close();
			return;
		}

		this._current = this._ipc.GetCollectionForObject(this._entity.Actor);
		ImGui.Text($"Assigning collection for {this._entity.Name}");
		ImGui.TextDisabled($"Currently set to: {this._current}");

		var list = this._ipc.GetCollections().ToList();
		if (this._list.Draw(list, out var selected)) {
			if (this._ipc.SetCollectionForObject(this._entity.Actor, selected!))
				this._entity.Redraw();
		}
	}

	private bool DrawItem(string name, bool _) => ImGui.Selectable(name, name == this._current);
}
