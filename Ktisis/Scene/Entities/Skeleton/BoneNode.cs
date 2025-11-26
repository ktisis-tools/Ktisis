using System.Numerics;

using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

using RenderSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

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
	
	// Bone transforms

	public unsafe Matrix4x4? GetMatrixModel() {
		var pose = this.GetPose();
		return pose != null ? HavokPosing.GetMatrix(pose, this.Info.BoneIndex) : null;
	}

	protected unsafe Matrix4x4? CalcMatrixWorld() {
		var skeleton = this.GetSkeleton();
		if (skeleton == null || this.GetMatrixModel() is not Matrix4x4 matrix)
			return null;

		var model = new Transform(skeleton->Transform);
		matrix.Translation *= model.Scale;
		matrix = Matrix4x4.Transform(matrix, model.Rotation);
		matrix.Translation += model.Position;
		return matrix;
	}

	protected unsafe void SetMatrixWorld(Matrix4x4 matrix) {
		var skeleton = this.GetSkeleton();
		var pose = skeleton != null ? this.GetPose() : null;
		if (pose == null) return;
		if(this.GetTransformModel() is not Transform transform) return;

		var model = new Transform(skeleton->Transform);
		matrix.Translation -= model.Position;
		matrix = Matrix4x4.Transform(matrix, Quaternion.Inverse(model.Rotation));
		matrix.Translation /= model.Scale;
		HavokPosing.SetModelTransform(pose, this.Info.BoneIndex, new Transform(matrix, transform));
	}

	protected void SetTransformWorld(Transform transform)
		=> this.SetMatrixWorld(transform.ComposeMatrix());

	public Transform? CalcTransformWorld() {
		var matrix = this.CalcMatrixWorld();
		if (matrix is null || this.GetTransformModel() is not Transform transform) return null;
		return new Transform(matrix.Value, transform);
	}

	public Transform? CalcTransformOverlay() {
		var transform = this.CalcTransformWorld();
		if (transform is null) return null;

		var offset = this.Scene.Context.Config.Offsets.GetOffset(this);
		if (offset is not null) {
			var offsetTransformed = Vector3.Transform((Vector3)offset, transform.Rotation);
			transform.Position += offsetTransformed;
		}

		return transform;
	}

	public unsafe Transform? GetTransformModel() {
		var pose = this.GetPose();
		return pose != null ? HavokPosing.GetModelTransform(pose, this.Info.BoneIndex) : null;
	}

	// Bone chain

	public unsafe bool IsBoneChildOf(BoneNode node) {
		if (this.Pose != node.Pose)
			return false;

		var skele = this.GetSkeleton();
		if (skele == null || skele->PartialSkeletons == null) return false;

		if (this.Info.PartialIndex == node.Info.PartialIndex)
			return this.Info.ParentIndex == node.Info.BoneIndex;
		
		if (node.Info.PartialIndex != 0) return false;
		
		var partial = skele->PartialSkeletons[this.Info.PartialIndex];
		return this.Info.BoneIndex == partial.ConnectedBoneIndex && node.Info.BoneIndex == partial.ConnectedParentBoneIndex;
	}

	public unsafe bool IsBoneDescendantOf(BoneNode node) {
		if (this.Pose != node.Pose)
			return false;

		var skele = this.GetSkeleton();
		if (skele == null || skele->PartialSkeletons == null) return false;

		var partial = skele->PartialSkeletons[this.Info.PartialIndex];
		hkaPose* pose;
		int boneId;
		int parentId;

		switch (this.Info.PartialIndex, node.Info.PartialIndex) {
			case var (a, b) when a == b:
				pose = partial.GetHavokPose(0);
				boneId = this.Info.BoneIndex;
				parentId = node.Info.BoneIndex;
				break;
			case (not 0, 0):
				var rootPartial = skele->PartialSkeletons[0];
				pose = rootPartial.GetHavokPose(0);
				boneId = partial.ConnectedParentBoneIndex;
				parentId = node.Info.BoneIndex;
				if (boneId == parentId) return true;
				break;
			default:
				return false;
		}
		
		return pose != null && pose->Skeleton != null && HavokPosing.IsBoneDescendantOf(
			pose->Skeleton->ParentIndices,
			boneId,
			parentId
		);
	}
	
	// Viera ear handling

	public bool IsVieraEarBone() => this.Info.Name.Length >= 7
		&& this.Info.Name.StartsWith("j_zer")
		&& this.Info.Name[6] == '_';
	
	// ITransform

	public virtual Transform? GetTransform() => this.CalcTransformWorld();
	public virtual void SetTransform(Transform transform) => this.SetTransformWorld(transform);

	public virtual Matrix4x4? GetMatrix() => this.CalcMatrixWorld();
	public virtual void SetMatrix(Matrix4x4 matrix) => this.SetMatrixWorld(matrix);
	
	// Attach

	public unsafe bool TryAcceptAttach(IAttachable child) {
		if (this.Info.PartialIndex > 0) return false;
		
		var attach = child.GetAttach();
		var chara = child.GetCharacter();
		if (attach == null || chara == null) return false;

		var parentSkeleton = this.GetSkeleton();
		var childSkeleton = chara->Skeleton;
		if (parentSkeleton == null || childSkeleton == null) return false;

		AttachUtility.SetBoneAttachment(parentSkeleton, childSkeleton, attach, (ushort)this.Info.BoneIndex);

		return true;
	}
}
