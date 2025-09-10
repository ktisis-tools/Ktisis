using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

using GLib.Popups.Context;
using GLib.Widgets;

namespace Ktisis.Interface.Nodes;

public class CheckableNode : IContextMenuNode {
	private readonly string _name;
	private readonly bool _state;
	private readonly Action _handler;
	
	public string? Shortcut;
	
	public CheckableNode(string name, bool state, Action handler) {
		_name = name;
		_state = state;
		_handler = handler;

	}
	
	public void Draw() {
		var invoke = this.Shortcut switch {
			not null => ImGui.MenuItem(this._name, this.Shortcut, _state),
			_ => ImGui.MenuItem(this._name, _state)
		};
			
		if (invoke) this._handler.Invoke();
	}
}
