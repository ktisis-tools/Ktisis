using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Interop.Unmanaged;

namespace Ktisis.Scene.Objects.Models; 

public abstract class ArmatureNode : SceneObject {
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
	
	// Armature access

	public Armature? GetArmature() => this.Parent switch {
		Armature arm => arm,
		ArmatureNode node when this != node => node.GetArmature(),
		_ => null
	};

	public Pointer<Skeleton>? GetSkeleton() => this.GetArmature()?.GetSkeleton();
}