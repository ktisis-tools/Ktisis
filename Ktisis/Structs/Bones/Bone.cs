﻿using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Localization;
using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
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

		public string LocaleName => Locale.GetBoneName(HkaBone.Name.String);

		public string UniqueId => $"{Partial}_{Index}";
		public string UniqueName => $"{LocaleName}##{UniqueId}";

		public Category Category => Category.GetForBone(HkaBone.Name.String);

		public unsafe hkQsTransformf* AccessModelSpace(PropagateOrNot propagate) => Pose->AccessBoneModelSpace(Index, propagate);

		public unsafe Vector3 GetWorldPos(ActorModel* model) => model->Position + Transform.Translation.Rotate(model->Rotation) * model->Height * model->Scale;

		public unsafe List<Bone> GetChildren() {
			var result = new List<Bone>();
			// Add child bones from same partial
			for (var i = Index + 1; i < Pose->Skeleton->ParentIndices.Length; i++) {
				var child = new Bone(Skeleton, Partial, i);
				if (child.ParentId != Index) continue;
				result.Add(child);
			}
			// Add child bones from connected partials
			for (var p = 0; p < Skeleton->PartialSkeletonCount; p++) {
				if (p == Partial) continue;
				var partial = Skeleton->PartialSkeletons[p];
				if (partial.ConnectedParentBoneIndex == Index) {
					var partialRoot = new Bone(Skeleton, p, partial.ConnectedBoneIndex);
					var children = partialRoot.GetChildren();
					foreach (var child in children)
						result.Add(child);
				}
			}
			return result;
		}

		public List<Bone> GetDescendants() {
			var list = GetChildren();
			for (var i = 0; i < list.Count; i++)
				list.AddRange(list[i].GetChildren());
			return list;
		}

		public bool IsBusted() =>
			float.IsNaN(Transform.Translation.X)
			|| float.IsNaN(Transform.Translation.Y)
			|| float.IsNaN(Transform.Translation.Z)
			|| Transform.Rotation.W == 0;
	}
}