using Ktisis.Editor.Posing.Partials;

namespace Ktisis.Scene.Decor;

public interface IAttachable {
	public bool IsAttached();

	public PartialBoneInfo? GetParentBone();

	public void Detach();
}
