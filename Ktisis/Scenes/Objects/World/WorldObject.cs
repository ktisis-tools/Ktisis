using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ModelType = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase.ModelType;

using Ktisis.Interop.Native;

namespace Ktisis.Scenes.Objects.World; 

// Wrapper around native Object types.

public class WorldObject : SceneObject {
	public nint Address { get; internal set; }
	private unsafe Object* Object => (Object*)Address;
	
	// Constructor

	public WorldObject(nint address) => Address = address;

	private unsafe static WorldObject FromObject(Pointer<Object> ptr) {
		var addr = ptr.Address;
		var objType = ptr.Data->GetObjectType();

		ModelType? modelType = null;
		if (objType is ObjectType.CharacterBase)
			modelType = ((CharacterBase*)addr)->GetModelType();

		return objType switch {
			ObjectType.CharacterBase when modelType is ModelType.Weapon => new Weapon(addr),
			ObjectType.CharacterBase => new Character(addr),
			_ => new WorldObject(addr)
		};
	}

	// Scene update
	
	internal unsafe override void Update() {
		if (this.Object == null || Children == null) return;

		var ownedList = Children
			.Where(x => x is WorldObject)
			.Cast<WorldObject>()
			.ToArray();

		var childObjects = GetChildObjects();
		foreach (var objectPtr in childObjects) {
			var owner = ownedList.FirstOrDefault(x => x!.Address == objectPtr.Address, null);
			if (owner == null) {
				owner = FromObject(objectPtr);
				Children.Add(owner);
			}
			owner.Update();
		}

		var addrList = childObjects.Select(ptr => ptr.Address).ToArray();
		Children.RemoveAll(x => x is WorldObject obj && !addrList.Contains(obj.Address));
	}

	// Native wrappers

	private unsafe PtrArray<Object> GetChildObjects() {
		var result = new PtrArray<Object>();
		
		var ptr = this.Object;
		if (ptr == null) return result;
		
		var child = ptr->ChildObject;
		while (child != null) {
			result.Add(child);
			child = child->NextSiblingObject;
			if (child == ptr->ChildObject) break;
		}

		return result;
	}
}