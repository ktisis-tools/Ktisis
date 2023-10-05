using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Data.Config;
using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.World;
using Ktisis.Editing.Attributes;
using Ktisis.Interface.Gui.Overlay.Render;

namespace Ktisis.Editing.Modes;

[ObjectMode(EditMode.Object, Renderer = typeof(ObjectRenderer))]
public class ObjectMode : ModeHandler {
	// Constructor

	private readonly ConfigService _cfg;
	
	public ObjectMode(SceneManager mgr, EditorService editor, ConfigService _cfg) : base(mgr, editor) {
		this._cfg = _cfg;
	}

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
	
	private IEnumerable<ITransform> FilterObjects(IEnumerable<SceneObject> objects)
		=> objects.Where(item => item is WorldObject).Cast<ITransform>();

	public override ITransform? GetTransformTarget(IEnumerable<SceneObject> objects)
		=> (ITransform?)objects.FirstOrDefault(x => x is WorldObject);
	
	// Object transform

	public override void Manipulate(ITransform target, Matrix4x4 final, Matrix4x4 initial, IEnumerable<SceneObject> objects) {
		Matrix4x4 deltaMx;
		if (Matrix4x4.Invert(initial, out var initialInverse))
			deltaMx = initialInverse * final;
		else return;
		
		if (this._cfg.Config.Editor_Flags.HasFlag(EditFlags.Mirror))
			Matrix4x4.Invert(deltaMx, out deltaMx);
			
		foreach (var item in FilterObjects(objects)) {
			if (item == target)
				item.SetMatrix(final);
			else if (item.GetMatrix() is Matrix4x4 matrix)
				item.SetMatrix(matrix * deltaMx);
		}
	}
}
