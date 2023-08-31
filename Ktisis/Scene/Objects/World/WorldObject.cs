using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scene.Impl;
using Ktisis.Common.Utility;

namespace Ktisis.Scene.Objects.World; 

public class WorldObject : SceneObject, IManipulable {
	// Unmanaged
	
	public nint Address { get; protected set; }

	private unsafe Object* Object => (Object*)this.Address;
	
	// Constructor

	protected WorldObject(nint address)
		=> this.Address = address;
	
	// IManipulable

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