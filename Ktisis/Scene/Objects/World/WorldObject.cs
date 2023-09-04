using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scene.Impl;
using Ktisis.Scene.Editing;
using Ktisis.Scene.Handlers;
using Ktisis.Common.Utility;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Scene.Objects.World; 

public class WorldObject : SceneObject, IManipulable, IEditMode, IVisibility {
	// Unmanaged
	
	public nint Address { get; protected set; }

	private unsafe Object* Object => (Object*)this.Address;
	
	// Constructor

	public WorldObject(nint address)
		=> this.Address = address;
	
	// Update handling

	public unsafe override void Update(SceneManager manager, SceneContext ctx) {
		if (this.Object == null) return;

		var ownedList = this.Children
			.Where(x => x is WorldObject)
			.Cast<WorldObject>()
			.ToList();

		var objects = GetChildObjects();
		foreach (var objectPtr in objects) {
			var owner = ownedList.Find(x => x.Address == objectPtr.Address);
			if (owner is not null) continue;
			
			var create = manager.GetHandler<ObjectHandler>()
				.CreateObject(objectPtr);
			if (create is not null)
				AddChild(create);
		}

		var addrList = objects.Select(ptr => ptr.Address);
		this.Children.RemoveAll(x => x is WorldObject obj && !addrList.Contains(obj.Address));
		
		base.Update(manager, ctx);
	}
	
	// Object access

	private unsafe PtrArray<Object> GetChildObjects() {
		var result = new PtrArray<Object>();

		var ptr = this.Object;
		if (ptr == null)
			return result;

		var child = ptr->ChildObject;
		while (child != null) {
			result.Add(child);
			child = child->NextSiblingObject;
			if (child == ptr->ChildObject)
				break;
		}
        
		return result;
	}
	
	// IEditMode

	public bool Visible { get; set; }

	public EditMode EditMode { get; init; } = EditMode.Object;
	
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

	public unsafe void SetTransform(Transform trans, TransformFlags _flags) {
		var ptr = this.Object;
		if (ptr == null) return;

		ptr->Position = trans.Position;
		ptr->Rotation = trans.Rotation;
		ptr->Scale = trans.Scale;
	}
}