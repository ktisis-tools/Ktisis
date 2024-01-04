using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Strategy.Types;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Strategy;

public class EditBone : EditEntity, ITransform, IVisibility {
	private readonly BoneNode Bone;
	
	public bool Visible { get; set; }

	public EditBone(
		BoneNode bone
	) {
		this.Bone = bone;
	}
	
	// Transforms

	public unsafe Transform? GetTransform() {
		var skeleton = this.Bone.GetSkeleton();
		var pose = this.Bone.GetPose();
		return pose != null ? HavokPoseUtil.GetWorldTransform(skeleton, pose, this.Bone.Info.BoneIndex) : null;
	}

	public unsafe void SetTransform(Transform transform) {
		var skeleton = this.Bone.GetSkeleton();
		var pose = this.Bone.GetPose();
		if (pose != null)
			HavokPoseUtil.SetWorldTransform(skeleton, pose, this.Bone.Info.BoneIndex, transform);
	}
}
