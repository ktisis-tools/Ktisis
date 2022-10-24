using System.Numerics;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static FFXIVClientStructs.Havok.hkaPose;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
	public class Bone {
		public int Index;
		public unsafe hkaPose* Pose;

		public unsafe Bone(Skeleton* skeleton, int partialId, int boneId) {
			Index = boneId;

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

		public unsafe hkQsTransformf* AccessModelSpace(PropagateOrNot propagate) => Pose->AccessBoneModelSpace(Index, propagate);

		public unsafe Vector3 GetWorldPos(ActorModel* model) => model->Position + Transform.Translation.Rotate(model->Rotation) * model->Height;

		public Category Category => Category.GetForBone(HkaBone.Name.String);
	}
}