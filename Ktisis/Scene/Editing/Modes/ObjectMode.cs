using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.World;
using Ktisis.Scene.Editing.Attributes;
using Ktisis.Interface.Overlay.Render;
using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;

namespace Ktisis.Scene.Editing.Modes;

[ObjectMode(EditMode.Object, Renderer = typeof(ObjectRenderer))]
public class ObjectMode : ModeHandler {
	public ObjectMode(SceneManager mgr) : base(mgr) {}
	
	// Object enumeration

	public override IEnumerable<SceneObject> GetEnumerator() {
		if (this.Manager.Scene is not SceneGraph scene)
			yield break;

		foreach (var item in FindObjects(scene.GetChildren()))
			yield return item;
	}

	private IEnumerable<WorldObject> FindObjects(IEnumerable<SceneObject> objects) {
		foreach (var item in objects) {
			if (item is WorldObject worldObj)
				yield return worldObj;
			
			foreach (var child in FindObjects(item.GetChildren()))
				yield return child;
		}
	}
	
	// Selection
	
	private IEnumerable<IManipulable> GetSelected()
		=> this.Manager.Editor.Selection
			.GetSelected()
			.Where(item => item is WorldObject)
			.Cast<IManipulable>();
	
	// Object transform

	public override Transform? GetTransform() {
		foreach (var item in GetSelected()) {
			if (item.GetTransform() is Transform trans)
				return trans;
		}
		
		return null;
	}

	public override void Manipulate(Matrix4x4 target, Matrix4x4 delta) {
		var isPrimary = true;
		foreach (var item in GetSelected()) {
			var matrix = item.ComposeMatrix();
			if (matrix is null) continue;

			if (isPrimary) {
				item.SetMatrix(target);
				isPrimary = false;
			} else {
				item.SetMatrix(matrix.Value.ApplyDelta(delta, target));
			}
		}
	}
}
