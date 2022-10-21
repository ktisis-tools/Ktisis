using System.Collections;

using FFXIVClientStructs.Havok;

namespace Ktisis.Structs.Bones {
	public struct BoneIterator : IEnumerable {
		public hkaPose Pose;

		public BoneIterator(hkaPose pose) => Pose = pose;

		public unsafe Bone this[short i] {
			get => new Bone() {
				Index = i,
				ParentIndex = Pose.Skeleton->ParentIndices[i],
				HkaBone = Pose.Skeleton->Bones[i],
				Transform = Pose.ModelPose[i]
			};
		}

		public IEnumerator GetEnumerator() {
			for (short i = 0; i < Pose.ModelPose.Length; i++)
				yield return this[i];
		}
	}
}