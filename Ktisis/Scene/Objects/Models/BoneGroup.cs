using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Display;

namespace Ktisis.Scene.Objects.Models;

public class BoneGroup : ArmatureGroup {
	// Properties

	public override string Name => this.Category?.Name ?? "Unknown";

	public override ItemType ItemType => ItemType.BoneGroup;

	// Constructor

	public readonly BoneCategory? Category;

	public BoneGroup(BoneCategory category)
		=> this.Category = category;

	// Stale check

	public bool IsStale() => this.Children.Count == 0;
}
