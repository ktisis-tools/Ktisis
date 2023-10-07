namespace Ktisis.Interface.Gui.Menus;

public class ContextMenuFactory : ContextNodeFactoryBase<ContextMenuFactory> {
    private readonly string Name;
	
	public delegate void OnFinalizedEvent(ContextMenu result);
	private event OnFinalizedEvent? OnFinalized;
	
	protected override ContextMenuFactory FactoryMethod() => this;
	
	public ContextMenuFactory(string id, OnFinalizedEvent callback) {
		this.Name = id;
		this.OnFinalized = callback;
	}

	public ContextMenu Create() {
		var result = new ContextMenu(this.Name, this.Nodes);
		this.OnFinalized?.Invoke(result);
		return result;
	}
}
