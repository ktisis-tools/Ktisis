using FFXIVClientStructs.Havok;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Partials;
using Ktisis.Scene.Decor;

using RenderSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class BoneNode : SkeletonNode, ITransform, IVisibility {
	public readonly PartialBoneInfo Info;
	public uint PartialId;
	
	public bool Visible { get; set; }

	public BoneNode(
		ISceneManager scene,
		EntityPose pose,
		PartialBoneInfo bone,
		uint partialId
	) : base(scene) {
		this.Type = EntityType.BoneNode;
		this.Pose = pose;
		this.Info = bone;
		this.PartialId = partialId;
	}
	
	public unsafe hkaPose* GetPose() => this.Pose.GetPose(this.Info.PartialIndex);
	public unsafe RenderSkeleton* GetSkeleton() => this.Pose.GetSkeleton();

	public bool MatchesId(int pId, int bId) => this.Info.PartialIndex == pId && this.Info.BoneIndex == bId;
	
	// Transform

	public unsafe Transform? GetTransform() {
		var skeleton = this.GetSkeleton();
		var pose = skeleton != null ? this.GetPose() : null;
		return pose != null ? HavokPoseUtil.GetWorldTransform(skeleton, pose, this.Info.BoneIndex) : null;
	}

	public unsafe void SetTransform(Transform transform) {
		var skeleton = this.GetSkeleton();
		var pose = skeleton != null ? this.GetPose() : null;
		if (pose == null) return;
		HavokPoseUtil.SetWorldTransform(skeleton, pose, this.Info.BoneIndex, transform);
	}
}
