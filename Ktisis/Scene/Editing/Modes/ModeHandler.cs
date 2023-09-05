using System.Collections.Generic;
using System.Numerics;

using Ktisis.Common.Utility;
using Ktisis.Scene.Objects;

namespace Ktisis.Scene.Editing.Modes;

public abstract class ModeHandler {
	// Constructor
    
	protected readonly SceneManager Manager;

	public ModeHandler(SceneManager mgr) {
		this.Manager = mgr;
	}
	
	// Enumeration

	public abstract IEnumerable<SceneObject> GetEnumerator();
	
	// Transforms
    
	public abstract Transform? GetTransform();

	public abstract void Manipulate(Matrix4x4 matrix, Matrix4x4 delta);
}