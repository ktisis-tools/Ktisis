using Ktisis.Config.Display;

namespace Ktisis.Scene.Impl;

public interface ITreeNode {
	public string UiId { get; init; }
	
	public ItemType ItemType { get; init; }
}
