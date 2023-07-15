using Dalamud.Interface;

namespace Ktisis.Scenes.Objects.Impl;

// TODO: Revisit this later, it's probably not the greatest way to do this.

public interface ITreeNode {
	internal string UiId { get; set; }

	public uint Color { get; protected init; }
	public FontAwesomeIcon Icon { get; protected init; }

	// Selection

	public void Select();

	public void Unselect();

	public void ToggleSelected();
}
