using System;
using System.Numerics;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
	public class Bone {
		public int Index;
		public int Partial;
		public unsafe hkaPose* Pose;

		public unsafe Bone(Skeleton* skeleton, int partialId, int boneId) {
			Index = boneId;
			Partial = partialId;

			var partial = skeleton->PartialSkeletons[partialId];
			var pose = partial.GetHavokPose(0);
			Pose = pose;
		}

		public unsafe hkaBone HkaBone => Pose->Skeleton->Bones[Index];
		public unsafe int ParentId => Pose->Skeleton->ParentIndices[Index];
		public unsafe hkQsTransformf Transform {
			get => Pose->ModelPose.Data[Index];
			set => Pose->ModelPose.Data[Index] = value;
		}

		public Category Category => Category.GetForBone(HkaBone.Name.String);

		public unsafe hkQsTransformf* AccessModelSpace(PropagateOrNot propagate) => Pose->AccessBoneModelSpace(Index, propagate);

		public unsafe Vector3 GetWorldPos(ActorModel* model) => model->Position + Transform.Translation.Rotate(model->Rotation) * model->Height;

		public unsafe short[] GetDescendents() {
			var size = Pose->Skeleton->Bones.Length;
			var result = new short[size];
			fixed (short* ptr = &result[0]) {
				var array = new hkArray<short>() {
					CapacityAndFlags = size,
					Data = ptr
				};
				Interop.PoseHooks.GetDescendentsFunc(Pose->Skeleton, (short)Index, &array, false);
				Array.Resize(ref result, array.Length);
			}
			return result;
		}
	}
}