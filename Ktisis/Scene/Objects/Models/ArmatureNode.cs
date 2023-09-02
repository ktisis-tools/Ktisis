using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Interop.Unmanaged;

namespace Ktisis.Scene.Objects.Models;

public abstract class ArmatureNode : SceneObject {
	// Armature access

	public abstract Armature GetArmature();

	public Pointer<Skeleton> GetSkeleton() => this.GetArmature().GetSkeleton();

	// Sort priority

	public int SortPriority;

	public void OrderByPriority() {
		this.Children.Sort((_a, _b) => (_a, _b) switch {
			(not ArmatureGroup, ArmatureGroup) => 1,
			(ArmatureGroup, not ArmatureGroup) => -1,
			(ArmatureNode a, ArmatureNode b) => a.SortPriority - b.SortPriority,
			(_, _) => 0
		});
	}
}
