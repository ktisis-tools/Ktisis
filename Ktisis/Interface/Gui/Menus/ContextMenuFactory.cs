namespace Ktisis.Interface.Gui.Menus;

public class ContextMenuFactory : ContextNodeFactoryBase<ContextMenuFactory> {
    private readonly string Name;
	
	protected override ContextMenuFactory FactoryMethod() => this;
	
	public ContextMenuFactory(string id) => this.Name = id;

	public ContextMenu Create() => new(this.Name, this.Nodes);
}
