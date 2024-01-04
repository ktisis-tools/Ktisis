using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Strategy.Types;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Editor.Strategy;

public class EditObject : EditEntity, ITransform, IVisibility {
	protected readonly WorldEntity WorldEntity;
	
	public bool Visible { get; set; }
	
	public EditObject(
		WorldEntity entity
	) {
		this.WorldEntity = entity;
	}

	private unsafe Object* Object => this.WorldEntity.IsValid ? this.WorldEntity.GetObject() : null;
	
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
