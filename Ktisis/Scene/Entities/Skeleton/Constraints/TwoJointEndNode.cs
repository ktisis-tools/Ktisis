using System.Numerics;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Ik.TwoJoints;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton.Constraints;

public class TwoJointEndNode : IkEndNode, ITwoJointsNode {
	public TwoJointsGroup Group { get; }

	public TwoJointEndNode(
		ISceneManager scene,
		EntityPose pose,
		PartialBoneInfo bone,
		uint partialId,
		TwoJointsGroup group
	) : base(scene, pose, bone, partialId) {
		this.Group = group;
	}
	
	// ITransform
	
	protected override bool IsOverride => this.IsEnabled && this.Group.Mode == TwoJointsMode.Fixed;

	public override Transform GetTransformTarget(Transform offset, Transform world) {
		offset.Position += this.Group.TargetPosition.ModelToWorldPos(offset);
		offset.Rotation = Quaternion.Normalize(offset.Rotation * this.Group.TargetRotation);
		offset.Scale = world.Scale;
		return offset;
	}

	public unsafe override void SetTransformTarget(Transform transform, Transform offset, Transform world) {
		var skeleton = this.Pose.GetSkeleton();
		if (skeleton == null) return;
		
		var setWorld = false;

		if (this.Group.EnforcePosition) {
			this.Group.TargetPosition = transform.Position.WorldToModelPos(offset);
		} else {
			world.Position = transform.Position;
			setWorld = true;
		}

		if (this.Group.EnforceRotation) {
			this.Group.TargetRotation = Quaternion.Normalize(Quaternion.Inverse(offset.Rotation) * transform.Rotation);
		} else {
			world.Rotation = transform.Rotation;
			setWorld = true;
		}
		
		if (!world.Scale.Equals(transform.Scale)) {
			world.Scale = transform.Scale;
			setWorld = true;
		}
		
		if (setWorld) this.SetTransformWorld(world);
	}
}
