using System.Collections.Generic;

using Dalamud.Interface;

using Ktisis.Interface;
using Ktisis.Interface.Items;

namespace Ktisis.Scenes.Objects; 

// Used to define wrappers around objects that may be added to the workspace tree.

public abstract class SceneObject : ITreeNode {
	// Data
	
	public string Name { get; set; }
	
	// Tree node properties
	// TODO: This probably doesn't belong here, revisit it later.
	
	public string UiId { get; set; }
	public virtual uint Color { get; init; } = 0xFFFFFFFF;
	public virtual FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.None;
	
	// Object

	public virtual List<SceneObject>? Children { get; init; } = new();

	// Constructor

	protected SceneObject() {
		Name = GetType().Name;
		UiId = Gui.GenerateId(this);
	}

	// Update

	internal virtual void Update()
		=> Children?.ForEach(child => child.Update());
}