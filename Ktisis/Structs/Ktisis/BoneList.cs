using System;
using System.Collections;

using Ktisis.Structs.Havok;
using Ktisis.Structs.Ktisis;

namespace Ktisis.Structs {
	public unsafe class BoneList : IEnumerable {
		public int Id = -1;

		public HkaSkeleton Skeleton;
		public ShitVecReversed<Transform> Transforms;

		public BoneList(HkaPose* pose) {
			Skeleton = *pose->Skeleton;
			Transforms = pose->Transforms;
		}

		public BoneList(int id, HkaPose* pose) : this(pose) {
			Id = id;
		}

		public Bone GetParentOf(Bone bone) {
			return this[bone.ParentId];
		}

		public Bone this[int index] {
			get => new Bone(this, index);
			set => new NotImplementedException();
		}

		public IEnumerator GetEnumerator() {
			for (int i = 0; i < Skeleton.Bones.Count; i++)
				yield return this[i];
		}
	}
}
