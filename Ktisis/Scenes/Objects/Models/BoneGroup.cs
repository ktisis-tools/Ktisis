using Ktisis.Config.Bones;

namespace Ktisis.Scenes.Objects.Models;

public class BoneGroup : SceneObject {
	// Trees

	public override uint Color { get; init; } = 0xFFFF9F68;

	// Category

	public readonly BoneCategory? Category;

	// BoneGroup

	private readonly Armature Owner;

	public BoneGroup(Armature armature, BoneCategory category) {
		Owner = armature;
		Category = category;
	}
}
