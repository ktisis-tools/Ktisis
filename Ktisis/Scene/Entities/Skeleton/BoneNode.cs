using System.Numerics;

using FFXIVClientStructs.Havok;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing.Types;
using Ktisis.Editor.Posing.Utility;
using Ktisis.Editor.Transforms;
using Ktisis.Scene.Decor;

using RenderSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class BoneNode : SkeletonNode, ITransform, IVisibility, IAttachTarget {
	public PartialBoneInfo Info;
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

	public Transform? GetTransform() {
		if (this.GetMatrix() is Matrix4x4 mx)
			return new Transform(mx);
		return null;
	}

	public void SetTransform(Transform transform) => this.SetMatrix(transform.ComposeMatrix());
	
	public unsafe Matrix4x4? GetMatrix() {
		var skeleton = this.GetSkeleton();
		var pose = skeleton != null ? this.GetPose() : null;
		if (pose == null) return null;

		var model = new Transform(skeleton->Transform);
		var matrix = HavokPoseUtil.GetMatrix(pose, this.Info.BoneIndex);
		matrix.Translation *= model.Scale;
		matrix = Matrix4x4.Transform(matrix, model.Rotation);
		matrix.Translation += model.Position;
		return matrix;
	}

	public unsafe void SetMatrix(Matrix4x4 matrix) {
		var skeleton = this.GetSkeleton();
		var pose = skeleton != null ? this.GetPose() : null;
		if (pose == null) return;

		var model = new Transform(skeleton->Transform);
		matrix.Translation -= model.Position;
		matrix = Matrix4x4.Transform(matrix, Quaternion.Inverse(model.Rotation));
		matrix.Translation /= model.Scale;
		HavokPoseUtil.SetMatrix(pose, this.Info.BoneIndex, matrix);
	}
	
	// Attach

	public unsafe void AcceptAttach(IAttachable child) {
		if (this.Info.PartialIndex > 0) return;
		
		var attach = child.GetAttach();
		var chara = child.GetCharacter();
		if (attach == null || chara == null) return;

		var parentSkeleton = this.GetSkeleton();
		var childSkeleton = chara->Skeleton;
		if (parentSkeleton == null || childSkeleton == null) return;

		AttachUtil.SetBoneAttachment(parentSkeleton, childSkeleton, attach, (ushort)this.Info.BoneIndex);
	}
}
