using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ModelType = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase.ModelType;

using Ktisis.Interop.Unmanaged;
using Ktisis.Scene.Objects.World;
using Weapon = Ktisis.Scene.Objects.World.Weapon;

namespace Ktisis.Scene.Handlers; 

public class ObjectHandler {
	// Ctor

	public ObjectHandler() {}
	
	// Object resolver

	public unsafe WorldObject? CreateObject(Pointer<Object> ptr) {
		if (ptr.IsNullPointer)
			return null;

		var objType = ptr.Data->GetObjectType();

		ModelType? modelType = null;
		if (objType is ObjectType.CharacterBase)
			modelType = ((CharacterBase*)ptr.Data)->GetModelType();

		return objType switch {
			ObjectType.CharacterBase when modelType is ModelType.Weapon => new Weapon(ptr.Address),
			ObjectType.CharacterBase => new Character(ptr.Address),
			_ => new WorldObject(ptr.Address)
		};
	}
}
