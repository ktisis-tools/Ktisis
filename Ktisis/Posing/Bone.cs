using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Services;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Bones;
using Ktisis.Library.Extensions;

namespace Ktisis.Posing {
	// This should only be used as a wrapper for bones that are guaranteed to exist in the current context.
	// DO NOT USE THIS IN CASES WHERE IT'S POSSIBLE FOR A SKELETON TO CHANGE OR UNLOAD. IT WILL CRASH.
	public class Bone {
		public int Index;
		public int Partial;
		public unsafe hkaPose* Pose;
		public unsafe Skeleton* Skeleton;

		public unsafe Bone(Skeleton* skeleton, int partialId, int boneId) {
			Index = boneId;
			Partial = partialId;

			var partial = skeleton->PartialSkeletons[partialId];
			var pose = partial.GetHavokPose(0);
			Pose = pose;
			Skeleton = skeleton;
		}

		public unsafe hkaBone HkaBone => Pose->Skeleton->Bones[Index];
		public unsafe int ParentId => Pose->Skeleton->ParentIndices[Index];
		public unsafe hkQsTransformf Transform {
			get => Pose->ModelPose.Data[Index];
			set => Pose->ModelPose.Data[Index] = value;
		}

		public string LocaleName => LocaleService.GetBoneName(HkaBone.Name.String ?? "");

		public string UniqueId => $"{Partial}_{Index}";
		public string UniqueName => $"{LocaleName}##{UniqueId}";

		public BoneCategory GetCategory() => CategoryService.GetBoneCategory(HkaBone.Name.String ?? "");

		public List<Category> Categories => Category.GetForBone(HkaBone.Name.String); // TODO: Remove

		public unsafe hkQsTransformf* AccessModelSpace(PropagateOrNot propagate = PropagateOrNot.DontPropagate) => Pose->AccessBoneModelSpace(Index, propagate);
		public unsafe hkQsTransformf* AccessLocalSpace() => Pose->AccessBoneLocalSpace(Index);

		public unsafe Vector3 GetWorldPos(ActorModel* model)
			=> model->Position + GetOffset(model) + Vector3.Transform(Transform.Translation.ToVector3() * model->Scale, model->Rotation) * model->Height;

		private unsafe Vector3 GetOffset(ActorModel* model) => CustomOffset.CalculateWorldOffset(model, this);

		public unsafe List<Bone> GetChildren(bool includePartials = true, bool usePartialRoot = false) {
			var result = new List<Bone>();

			if (Pose == null || Pose->Skeleton == null)
				return result;

			// Add child bones from same partial
			for (var i = Index + 1; i < Pose->Skeleton->ParentIndices.Length; i++) {
				var child = new Bone(Skeleton, Partial, i);
				if (child.ParentId != Index) continue;
				result.Add(child);
			}
			// Add child bones from connected partials
			if (includePartials && Partial == 0) {
				for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
					if (p == Partial) continue;
					var partial = Skeleton->PartialSkeletons[p];
					if (partial.ConnectedParentBoneIndex == Index) {
						var partialRoot = new Bone(Skeleton, p, partial.ConnectedBoneIndex);
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
			var name = HkaBone.Name.String;
			if (name == null) return null;

			var prefix = name[..^2];

			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				var partial = Skeleton->PartialSkeletons[p];
				var pose = partial.GetHavokPose(0);
				if (pose == null) continue;

				var poseSkeleton = pose->Skeleton;
				for (var i = 1; i < poseSkeleton->Bones.Length; i++) {
					var potentialBone = new Bone(Skeleton, p, i);
					if (potentialBone == null) continue;
					var pBName = potentialBone.HkaBone.Name.String;
					if (pBName != null && pBName[..^2] == prefix && pBName != name)
						return potentialBone;
				}
			}
			return null;
		}

		public unsafe bool IsValid() {
			if (Skeleton == null) return false;
			if (Skeleton->PartialSkeletons == null) return false;

			var partial = Skeleton->PartialSkeletons[Partial];
			if (partial.GetHavokPose(0) == null) return false;

			return !IsBusted();
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
				var access = child.AccessModelSpace(PropagateOrNot.DontPropagate);

				var offset = access->Translation.ToVector3() - sourcePos;
				offset = Vector3.Transform(offset, deltaRot);

				var matrix = InteropService.GetMatrix(access);
				matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
				matrix.Translation = deltaPos + sourcePos + offset;
				InteropService.SetMatrix(access, matrix);
			}
		}

		public unsafe void PropagateSibling(Quaternion deltaRot, SiblingLink mode = SiblingLink.Rotation) {
			if (mode == SiblingLink.None) return;

			var access = AccessModelSpace(PropagateOrNot.DontPropagate);
			var offset = access->Translation.ToVector3();

			if (mode == SiblingLink.RotationMirrorX)
				deltaRot = new(-deltaRot.X, deltaRot.Y, deltaRot.Z, -deltaRot.W);

			var matrix = InteropService.GetMatrix(access);
			matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
			matrix.Translation = offset;

			var initialRot = access->Rotation.ToQuat();
			var initialPos = access->Translation.ToVector3();
			InteropService.SetMatrix(access, matrix);

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