using System;
using System.Collections.Generic;

namespace Ktisis.Interface.Gui.Menus;

public interface IContextNodeFactoryBase<out T> {
	public T AddAction(string name, Action callback);
	public T AddAction(string name, Action callback, string shortcut);

	public T AddActionGroup(Action<ContextNodeFactory> builder);

	public T AddSubMenu(string name, Action<ContextNodeFactory> builder);

	public T AddSeparator();
}

public abstract class ContextNodeFactoryBase<T> : IContextNodeFactoryBase<T> {
	protected readonly List<IContextMenuNode> Nodes = new();

	private IContextMenuNode? LastNode;

	protected abstract T FactoryMethod();
	
	protected T AddNode(IContextMenuNode node) {
		switch (this.LastNode) {
			case not null when node is ContextMenu.ActionGroup:
			case ContextMenu.ActionGroup when node is not ContextMenu.Separator:
				AddSeparator();
				break;
		}

		this.Nodes.Add(node);
		this.LastNode = node;

		return FactoryMethod();
	}
	
	public T AddAction(string name, Action callback) {
		var node = new ContextMenu.ActionItem(name, callback);
		return AddNode(node);
	}
	
	public T AddAction(string name, Action callback, string shortcut) {
		var node = new ContextMenu.ActionItem(name, callback, shortcut);
		return AddNode(node);
	}
	
	public T AddActionGroup(Action<ContextNodeFactory> builder) {
		var factory = new ContextNodeFactory();
		builder.Invoke(factory);
		var node = new ContextMenu.ActionGroup(factory.GetNodes());
		return AddNode(node);
	}

	public T AddSubMenu(string name, Action<ContextNodeFactory> builder) {
		var factory = new ContextNodeFactory();
		builder.Invoke(factory);
		var node = new ContextMenu.ActionSubMenu(name, factory.GetNodes());
		return AddNode(node);
	}

	public T AddSeparator() => AddNode(new ContextMenu.Separator());
}

public class ContextNodeFactory : ContextNodeFactoryBase<ContextNodeFactory> {
	protected override ContextNodeFactory FactoryMethod() => this;

	public List<IContextMenuNode> GetNodes() => this.Nodes;
}
