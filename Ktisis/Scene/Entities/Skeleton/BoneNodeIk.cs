using System.Numerics;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Ik;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class BoneNodeIk : BoneNode, INodeIk {
	public readonly TwoJointsGroup Group;
	
	public bool IsEnabled => this.Group.IsEnabled;

	private bool IsFixed => this.IsEnabled && this.Group.Mode == TwoJointsMode.Fixed;

	public BoneNodeIk(
		ISceneManager scene,
		EntityPose pose,
		PartialBoneInfo bone,
		uint partialId,
		TwoJointsGroup group
	) : base(scene, pose, bone, partialId) {
		this.Group = group;
	}
	
	// INodeIk

	public void Enable() {
		this.Group.IsEnabled = true;

		var transform = this.GetTransformModel();
		this.Group.TargetPosition = transform?.Position ?? Vector3.Zero;
		this.Group.TargetRotation = transform?.Rotation ?? Quaternion.Identity;
	}

	public void Disable() {
		this.Group.IsEnabled = false;
	}

	public void Toggle() {
		if (this.IsEnabled)
			this.Disable();
		else
			this.Enable();
	}
	
	// ITransform

	public unsafe override Transform? GetTransform() {
		if (!this.IsFixed)
			return this.CalcTransformWorld();

		var skeleton = this.Pose.GetSkeleton();
		if (skeleton == null) return null;
		
		var transform = new Transform(skeleton->Transform);
		transform.Position += Vector3.Transform(this.Group.TargetPosition, transform.Rotation) * transform.Scale;
		transform.Rotation *= this.Group.TargetRotation;
		transform.Scale = this.CalcTransformWorld()?.Scale ?? Vector3.One;
		return transform;
	}

	public unsafe override void SetTransform(Transform transform) {
		if (!this.IsFixed) {
			this.SetTransformWorld(transform);
			return;
		}
		
		var skeleton = this.Pose.GetSkeleton();
		if (skeleton == null) return;
		
		var model = new Transform(skeleton->Transform);
		this.Group.TargetPosition = Vector3.Transform(
			transform.Position - model.Position,
			Quaternion.Inverse(model.Rotation)
		) / model.Scale;

		var world = this.CalcTransformWorld();
		if (world != null && !world.Scale.Equals(transform.Scale)) {
			world.Scale = transform.Scale;
			this.SetTransformWorld(world);
		}
	}

	public override Matrix4x4? GetMatrix()
		=> this.IsFixed ? this.GetTransform()?.ComposeMatrix() : this.CalcMatrixWorld();

	public override void SetMatrix(Matrix4x4 matrix) {
		if (this.IsFixed)
			this.SetTransform(new Transform(matrix));
		else
			this.SetMatrixWorld(matrix);
	}
}
