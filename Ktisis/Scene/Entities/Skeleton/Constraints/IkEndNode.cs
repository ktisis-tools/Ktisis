using System.Numerics;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton.Constraints;

public abstract class IkEndNode : BoneNode, IIkNode {
	private new IkNodeGroupBase? Parent => base.Parent as IkNodeGroupBase;

	protected IkEndNode(
		ISceneManager scene,
		EntityPose pose,
		PartialBoneInfo bone,
		uint partialId
	) : base(scene, pose, bone, partialId) { }
	
	// Group wrappers

	public virtual bool IsEnabled => this.Parent?.IsEnabled ?? false;

	public unsafe virtual void Enable() {
		var skeleton = this.GetSkeleton();
		if (skeleton == null) return;

		var offset = new Transform(skeleton->Transform);
		var world = this.CalcTransformWorld();
		if (world != null)
			this.SetTransformTarget(world, offset, world);
		
		this.Parent?.Enable();
	}
	
	public virtual void Disable() => this.Parent?.Disable();

	public virtual void Toggle() {
		if (this.IsEnabled)
			this.Disable();
		else
			this.Enable();
	}
	
	// Target transform
	
	protected abstract bool IsOverride { get; }

	public abstract Transform GetTransformTarget(Transform offset, Transform world);
	public abstract void SetTransformTarget(Transform target, Transform offset, Transform world);
	
	// ITransform

	public unsafe override Transform? GetTransform() {
		var skeleton = this.Pose.GetSkeleton();
		if (skeleton == null) return null;
		
		var offset = new Transform(skeleton->Transform);
		var world = this.CalcTransformWorld();
		if (!this.IsOverride || world == null) return world;
		
		return this.GetTransformTarget(offset, world);
	}

	public unsafe override void SetTransform(Transform transform) {
		var skeleton = this.Pose.GetSkeleton();
		if (skeleton == null) return;

		var offset = new Transform(skeleton->Transform);
		var world = this.CalcTransformWorld();
		
		if (this.IsOverride && world != null)
			this.SetTransformTarget(transform, offset, world);
		else
			this.SetTransformWorld(transform);
	}

	public override Matrix4x4? GetMatrix()
		=> this.IsOverride ? this.GetTransform()?.ComposeMatrix() : this.CalcMatrixWorld();

	public override void SetMatrix(Matrix4x4 matrix) {
		if (this.IsOverride)
			if (this.GetTransform() is { } transform)
				this.SetTransform(new Transform(matrix, transform));
			else
				this.SetTransform(new Transform(matrix));
		else
			this.SetMatrixWorld(matrix);
	}
}
