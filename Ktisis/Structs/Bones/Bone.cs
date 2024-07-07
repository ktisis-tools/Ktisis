using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;

using Ktisis.Localization;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Extensions;

namespace Ktisis.Structs.Bones {
	public class Bone {
		public int Index;
		public int Partial;
		public unsafe hkaPose* Pose;
		public unsafe Skeleton* Skeleton;
		internal nint PoseAddress;
		internal bool IsChildModel;

		public unsafe Bone(Skeleton* skeleton, int partialId, int boneId, bool isChild = false) {
			Index = boneId;
			Partial = partialId;

			var partial = skeleton->PartialSkeletons[partialId];
			var pose = partial.GetHavokPose(0);
			Pose = pose;
			Skeleton = skeleton;
			PoseAddress = (nint)pose;
			IsChildModel = isChild;
		}

		public unsafe hkaBone HkaBone => Pose->Skeleton->Bones[Index];
		public unsafe int ParentId => Pose->Skeleton->ParentIndices[Index];
		public unsafe hkQsTransformf Transform {
			get => Pose->ModelPose.Data[Index];
			set => Pose->ModelPose.Data[Index] = value;
		}

		public string LocaleName => Locale.GetBoneName(IsChildModel && Index == 1 ? "Prop" : HkaBone.Name.String ?? "Unknown");

		public string UniqueId => $"{PoseAddress:X}_{Partial}_{Index}";
		public string UniqueName => $"{LocaleName}##{UniqueId}";

		// stupid hack until this gets rewritten
		internal List<Category>? _setCategory = null;
		public List<Category> Categories => _setCategory ?? Category.GetForBone(HkaBone.Name.String);

		public unsafe hkQsTransformf* AccessModelSpace(hkaPose.PropagateOrNot propagate = hkaPose.PropagateOrNot.DontPropagate) => Pose->AccessBoneModelSpace(Index, propagate);
		public unsafe hkQsTransformf* AccessLocalSpace() => Pose->AccessBoneLocalSpace(Index);

		public unsafe Vector3 GetWorldPos(ActorModel* model, ActorModel* parent = null) {
			var pos = model->Position + GetOffset(model);
			
			var translate = Vector3.Transform(Transform.Translation.ToVector3() * model->Scale, model->Rotation);

			var scale = model->Height;
			if (parent != null)
				scale *= parent->Height;
			
			if (model->Attach.Count == 1 && model->Attach.Type == 4) {
				var boneAttach = model->Attach.BoneAttach;
				if (boneAttach != null) scale *= boneAttach->Scale;
			}

			return pos + translate * scale;
		}

		private unsafe Vector3 GetOffset(ActorModel* model) => CustomOffset.CalculateWorldOffset(model, this);

		public unsafe List<Bone> GetChildren(bool includePartials = true, bool usePartialRoot = false) {
			var result = new List<Bone>();

			if (Pose == null || Pose->Skeleton == null)
				return result;

			// Add child bones from same partial
			for (var i = Index + 1; i < Pose->Skeleton->ParentIndices.Length; i++) {
				var child = new Bone(Skeleton, Partial, i, IsChildModel);
				if (child.ParentId != Index) continue;
				result.Add(child);
			}
			// Add child bones from connected partials
			if (includePartials && Partial == 0) {
				for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
					if (p == Partial) continue;
					var partial = Skeleton->PartialSkeletons[p];
					if (partial.ConnectedParentBoneIndex == Index) {
						var partialRoot = new Bone(Skeleton, p, partial.ConnectedBoneIndex, IsChildModel);
						if (usePartialRoot) {
							result.Add(partialRoot);
						} else {
							var children = partialRoot.GetChildren();
							foreach (var child in children)
								result.Add(child);
						}
					}
				}
			}
			return result;
		}

		public List<Bone> GetDescendants(bool includePartials = true, bool usePartialRoot = false) {
			var list = GetChildren(includePartials, usePartialRoot);
			for (var i = 0; i < list.Count; i++)
				list.AddRange(list[i].GetChildren(includePartials, usePartialRoot));
			return list;
		}

		public unsafe Bone? GetMirrorSibling() {
			var name = HkaBone.Name.String ?? "";
			var prefix = name[..^2];

			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				var partial = Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var poseSkeleton = pose->Skeleton;
				for (var i = 1; i < poseSkeleton->Bones.Length; i++) {
					var potentialBone = new Bone(Skeleton, p, i, IsChildModel);
					if (potentialBone == null) continue;
					var pBName = potentialBone.HkaBone.Name.String ?? "";
					if (pBName[..^2] == prefix && pBName != name)
						return potentialBone;
				}
			}
			return null;
		}

		public bool IsBusted() => !Transform.Translation.X.IsValid()
			|| !Transform.Translation.Y.IsValid()
			|| !Transform.Translation.Z.IsValid();

		public unsafe void PropagateChildren(hkQsTransformf* transform, Vector3 initialPos, Quaternion initialRot, bool includePartials = true) {
			// Bone parenting
			// Adapted from Anamnesis Studio code shared by Yuki - thank you!

			var sourcePos = transform->Translation.ToVector3();
			var deltaRot = transform->Rotation.ToQuat() / initialRot;
			var deltaPos = sourcePos - initialPos;

			var descendants = GetDescendants(includePartials, true);
			foreach (var child in descendants) {
				var access = child.AccessModelSpace(hkaPose.PropagateOrNot.DontPropagate);

				var offset = access->Translation.ToVector3() - sourcePos;
				offset = Vector3.Transform(offset, deltaRot);

				var matrix = Interop.Alloc.GetMatrix(access);
				matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
				matrix.Translation = deltaPos + sourcePos + offset;
				Interop.Alloc.SetMatrix(access, matrix);
			}
		}

		public unsafe void PropagateSibling(Quaternion deltaRot, SiblingLink mode = SiblingLink.Rotation) {
			if (mode == SiblingLink.None) return;

			var access = AccessModelSpace(hkaPose.PropagateOrNot.DontPropagate);
			var offset = access->Translation.ToVector3();

			if (mode == SiblingLink.RotationMirrorX)
				deltaRot = new(-deltaRot.X, deltaRot.Y, deltaRot.Z, -deltaRot.W);

			var matrix = Interop.Alloc.GetMatrix(access);
			matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = offset;

			var initialRot = access->Rotation.ToQuat();
			var initialPos = access->Translation.ToVector3();
			Interop.Alloc.SetMatrix(access, matrix);

			if (Ktisis.Configuration.EnableParenting)
				PropagateChildren(access, initialPos, initialRot);
		}
	}

	public enum SiblingLink {
		None,
		Rotation,
		RotationMirrorX
	}
}
