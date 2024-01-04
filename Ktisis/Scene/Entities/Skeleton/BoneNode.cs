using FFXIVClientStructs.Havok;

using Ktisis.Editor.Posing.Partials;
using Ktisis.Editor.Strategy;
using Ktisis.Editor.Strategy.Bones;

using RenderSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class BoneNode : SkeletonNode {
	public readonly PartialBoneInfo Info;
	public uint PartialId;
	
	new public BoneEditor Edit() => (BoneEditor)this.Strategy;

	public BoneNode(
		ISceneManager scene,
		EntityPose pose,
		PartialBoneInfo bone,
		uint partialId
	) : base(scene) {
		this.Strategy = new BoneEditor(this);
		this.Type = EntityType.BoneNode;
		this.Pose = pose;
		this.Info = bone;
		this.PartialId = partialId;
	}
	
	public unsafe hkaPose* GetPose() => this.Pose.GetPose(this.Info.PartialIndex);
	public unsafe RenderSkeleton* GetSkeleton() => this.Pose.GetSkeleton();

	public bool MatchesId(int pId, int bId) => this.Info.PartialIndex == pId && this.Info.BoneIndex == bId;
}
