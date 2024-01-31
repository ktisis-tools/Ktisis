using System.Numerics;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Ik.Ccd;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton.Constraints;

public class CcdEndNode : IkEndNode, ICcdNode {
	public CcdGroup Group { get; }
	
	public CcdEndNode(
		ISceneManager scene,
		EntityPose pose,
		PartialBoneInfo bone,
		uint partialId,
		CcdGroup group
	) : base(scene, pose, bone, partialId) {
		this.Group = group;
	}
	
	// IK Transforms

	protected override bool IsOverride => this.IsEnabled;

	public override Transform GetTransformTarget(Transform offset, Transform world) {
		offset.Position += this.Group.TargetPosition.ModelToWorldPos(offset);
		offset.Rotation = world.Rotation;
		offset.Scale = world.Scale;
		return offset;
	}

	public unsafe override void SetTransformTarget(Transform target, Transform offset, Transform world) {
		var skeleton = this.Pose.GetSkeleton();
		if (skeleton == null) return;

		this.Group.TargetPosition = target.Position.WorldToModelPos(offset);
		world.Rotation = target.Rotation;
		world.Scale = target.Scale;
		this.SetTransformWorld(world);
	}
}
