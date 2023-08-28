using Ktisis.Data.Config.Bones;

namespace Ktisis.Scene.Objects.Models;

public class BoneGroup : ArmatureGroup {
	// Properties

	public override string Name => this.Category?.Name ?? "Unknown";

	// Constructor

	public readonly BoneCategory? Category;

	public BoneGroup(BoneCategory category)
		=> this.Category = category;

	// Stale check

	public bool IsStale() => this.Children.Count == 0;
}
