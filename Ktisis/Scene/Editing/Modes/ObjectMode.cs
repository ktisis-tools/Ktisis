using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.World;
using Ktisis.Scene.Editing.Attributes;
using Ktisis.Interface.Overlay.Render;
using Ktisis.Common.Extensions;
using Ktisis.Data.Config;

namespace Ktisis.Scene.Editing.Modes;

[ObjectMode(EditMode.Object, Renderer = typeof(ObjectRenderer))]
public class ObjectMode : ModeHandler {
	// Constructor

	private readonly ConfigService _cfg;

	public ObjectMode(SceneManager mgr, ConfigService _cfg) : base(mgr) {
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

	private IEnumerable<ITransform> GetSelected()
		=> this.Manager.Editor.Selection
			.GetSelected()
			.Where(item => item is WorldObject)
			.Cast<ITransform>();

	public override ITransform? GetTransformTarget()
		=> (WorldObject?)GetSelected().FirstOrDefault();

	// Object transform

	public override void Manipulate(ITransform target, Matrix4x4 targetMx, Matrix4x4 deltaMx) {
		if (this._cfg.Config.Editor_Flags.HasFlag(EditFlags.Mirror))
			Matrix4x4.Invert(deltaMx, out deltaMx);

		foreach (var item in GetSelected()) {
			if (item == target)
				item.SetMatrix(targetMx);
			else if (item.GetMatrix() is Matrix4x4 matrix)
				item.SetMatrix(matrix.ApplyDelta(deltaMx, targetMx));
		}
	}
}
