using Ktisis.Posing;
using Ktisis.Posing.Bones;
using Ktisis.Scene.Impl;
using Ktisis.Common.Utility;
using Ktisis.Data.Config.Display;

namespace Ktisis.Scene.Objects.Models;

public class Bone : ArmatureNode, IManipulable {
	// Properties

	public override ItemType ItemType => ItemType.BoneNode;

	// Constructor

	public readonly BoneData Data;

	public uint PartialId;

	public Bone(BoneData bone, uint pId) {
		this.Name = bone.Name;
		this.Data = bone;
		this.PartialId = pId;
	}

	// IManipulable

	public Transform? GetTransform() {
		var skeleton = this.GetSkeleton();
		if (skeleton is null) return null;

		return new PoseEditor(skeleton)
			.SetBone(this.Data)
			.GetWorldTransform();
	}

	public void SetTransform(Transform trans, TransformFlags flags) {
		var skeleton = this.GetSkeleton();
		if (skeleton is null) return;

		var editor = new PoseEditor(skeleton)
			.SetBone(this.Data);

		var initial = editor.GetTransform();
		editor.SetWorldTransform(trans);

		if (flags.HasFlag(TransformFlags.Propagate) && initial is not null)
			editor.Propagate(trans, initial);
	}
}
