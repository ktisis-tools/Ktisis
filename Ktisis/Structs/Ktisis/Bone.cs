using System.Numerics;

using Ktisis.Structs.Havok;

namespace Ktisis.Structs.Ktisis {
	public class Bone {
		public HkaBone HkaBone;

		public short ParentId;
		public Transform Transform;

		public Bone(BoneList bones, int index) {
			ParentId = bones.Skeleton.ParentIndex[index];
			Transform = bones.Transforms[index];
		}

		public Vector3 Rotate(Quaternion quat) {
			var t = Transform.Translate;
			return Vector3.Transform(new Vector3(t.X, t.Y, t.Z), quat);
		}
	}
}
