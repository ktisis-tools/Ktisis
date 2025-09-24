using System;

using GLib.Popups.Context;

namespace Ktisis.Interface.Nodes;

public static class Extensions {
	public static ContextMenuBuilder CheckableAction(this ContextMenuBuilder builder, string name, bool state, Action handler) {
		return builder.AddNode(new CheckableNode(name, state, handler));
	}
}
