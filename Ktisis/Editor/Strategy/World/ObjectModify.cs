using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Strategy.Decor;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Editor.Strategy.World;

public class ObjectModify : BaseModify, ITransform, IVisibility {
	protected WorldEntity Entity { get; }
	
	public bool Visible { get; set; }
	
	public ObjectModify(
		WorldEntity entity
	) {
		this.Entity = entity;
	}

	private unsafe Object* Object => this.Entity.IsValid ? this.Entity.GetObject() : null;
	
	// Transform

	public unsafe Transform? GetTransform() {
		var ptr = this.Object;
		if (ptr == null) return null;
		return new Transform(
			ptr->Position,
			ptr->Rotation,
			ptr->Scale
		);
	}

	public unsafe void SetTransform(Transform trans) {
		var ptr = this.Object;
		if (ptr == null) return;
		ptr->Position = trans.Position;
		ptr->Rotation = trans.Rotation;
		ptr->Scale = trans.Scale;
	}
}
