using System;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Transforms;

public static class AttachUtil {
	public unsafe static void SetBoneAttachment(Skeleton* parent, Skeleton* child, Attach* attach, ushort parentBoneId, ushort childBoneId = 0) {
		if (parent == child)
			throw new Exception("Attempting to parent attachment point to itself.");
		
		var isActive = attach->Count != 0;
		attach->Type = AttachType.BoneIndex;
		attach->Count = 1;
		attach->Parent = parent;
		attach->Child = child;
		attach->Param->ParentId = parentBoneId;
		attach->Param->ChildId = childBoneId;
		if (!isActive)
			attach->Param->Transform = new Transform();
	}

	public unsafe static void SetTransformRelative(Attach* attach, Transform target, Transform source) {
		var pSkele = attach->GetParentSkeleton();
		if (pSkele == null
			|| pSkele->PartialSkeletons == null
			|| pSkele->PartialSkeletons->HavokPoses == null
		) return;
		
		var parentId = attach->Param->ParentId;
		if (attach->Type == AttachType.ElementId && !((SkeletonEx*)pSkele)->TryGetBoneIndexForElementId(parentId, out parentId))
			return;
		
		var pPose = pSkele->PartialSkeletons[0].GetHavokPose(0);
		if (pPose == null) return;
		
		var pModel = HavokPoseUtil.GetWorldTransform(pSkele, pPose, parentId)!;
		
		var inverseRot = Quaternion.Inverse(pModel.Rotation);
		var transform = new Transform(attach->Param->Transform);
		transform.Position += Vector3.Transform(target.Position - source.Position, inverseRot);
		transform.Rotation = inverseRot * target.Rotation;
		transform.Scale += Vector3.Transform(target.Scale - source.Scale, inverseRot);
		attach->Param->Transform = transform;
	}

	public unsafe static void Detach(Attach* attach) {
		attach->Type = AttachType.None;
		attach->Count = 0;
		attach->Parent = null;
		attach->Child = null;
		if (attach->Param == null) return;
		attach->Param->ParentId = ushort.MaxValue;
		attach->Param->ChildId = ushort.MaxValue;
	}
}
