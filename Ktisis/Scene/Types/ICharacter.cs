using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Scene.Types;

public interface ICharacter {
	public unsafe CharacterBase* GetCharacter();
}
