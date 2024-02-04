using Ktisis.Editor.Posing.Types;
using Ktisis.Structs.Attachment;
using Ktisis.Structs.Characters;

namespace Ktisis.Scene.Decor;

public interface IAttachable : ICharacter {
	public bool IsAttached();

	public unsafe Attach* GetAttach();
	public PartialBoneInfo? GetParentBone();

	public void Detach();
}
