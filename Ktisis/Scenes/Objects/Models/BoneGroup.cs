using Ktisis.Config.Bones;

namespace Ktisis.Scenes.Objects.Models;

public class BoneGroup : SceneObject {
	// Trees

	public override uint Color { get; init; } = 0xFFFF9F68;

	// Category

	public readonly BoneCategory? Category;

	public BoneGroup(BoneCategory category)
		=> Category = category;
}
