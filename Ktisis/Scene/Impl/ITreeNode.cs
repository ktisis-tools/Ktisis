namespace Ktisis.Scene.Impl;

public enum TreeNodeType {
	None
}

public interface ITreeNode {
	public string UiId { get; init; }
	
	public TreeNodeType NodeType { get; init; }
}