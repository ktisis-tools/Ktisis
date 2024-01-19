using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Structs.Characters;

namespace Ktisis.Scene.Decor;

public interface ICharacter {
	public unsafe CharacterBase* GetCharacter();
}
