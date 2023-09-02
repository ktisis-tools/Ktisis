using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Display;

namespace Ktisis.Scene.Objects.Models; 

public class BoneGroup : ArmatureGroup {
	// Properties

	public override string Name => this.Category?.Name ?? "Unknown";

	public override ItemType ItemType => ItemType.BoneGroup;
	
	// Constructor

	private readonly Armature Armature;

	public readonly BoneCategory? Category;

	public BoneGroup(Armature armature, BoneCategory category) {
		this.Armature = armature;
		this.Category = category;
	}
	
	// Armature access
	
	public override Armature GetArmature() => this.Armature;

	// Stale check

	public bool IsStale() => this.Children.Count == 0;
}