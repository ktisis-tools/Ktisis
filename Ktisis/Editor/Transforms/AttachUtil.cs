using System;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Structs.Animation;
using Ktisis.Structs.Attachment;
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

	public unsafe static bool TryGetParentBoneIndex(Attach* attach, out ushort index) {
		index = attach->Param->ParentId;
		return attach->Type switch {
			AttachType.BoneIndex => true,
			AttachType.ElementId => ((SkeletonEx*)attach->GetParentSkeleton())->TryGetBoneIndexForElementId(index, out index),
			_ => false
		};
	}
	
	public unsafe static void SetTransformRelative(Attach* attach, Transform target, Transform source) {
		var pSkele = attach->GetParentSkeleton();
		if (pSkele == null
			|| pSkele->PartialSkeletons == null
			|| pSkele->PartialSkeletons->HavokPoses == null
		) return;
		
		var pPose = pSkele->PartialSkeletons[0].GetHavokPose(0);
		if (pPose == null) return;
		
		if (!TryGetParentBoneIndex(attach, out var parentId)) return;
		
		// Resolve rotation offset from element for fashion accessories
		var eRotate = Quaternion.Identity;
		if (attach->Type == AttachType.ElementId) {
			var ex = (SkeletonEx*)pSkele;
			for (var x = 0; x < ex->ElementCount; x++) {
				var element = ex->ElementParam + x;
				if ((ushort)element->ElementId != attach->Param->ParentId) continue;
				eRotate = (element->Rotation * MathHelpers.Rad2Deg).EulerAnglesToQuaternion();
			}
		}
		
		// worldPos = rootPos + ((modelPos + (elementPos + attachPos * elementRot) * modelRot) * rootRot) * rootScale
		
		var pModel = HavokPosing.GetModelTransform(pPose, parentId)!;
		var worldRot = (Quaternion)pSkele->Transform.Rotation * pModel.Rotation * eRotate;
		var inverseRot = Quaternion.Inverse(worldRot);
		
		var offset = new Transform(attach->Param->Transform);
		offset.Position += Vector3.Transform(target.Position - source.Position, inverseRot);
		offset.Rotation = inverseRot * target.Rotation;
		attach->Param->Transform = offset;
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
