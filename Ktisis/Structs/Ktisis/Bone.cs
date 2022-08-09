using System.Numerics;

using ImGuizmoNET;

using Ktisis.Structs.Havok;

namespace Ktisis.Structs.Ktisis {
	public class Bone {
		public HkaBone HkaBone;

		public int Index;
		public short ParentId;
		public Transform Transform;

		public SharpDX.Matrix Matrix;

		public Bone(BoneList bones, int index) {
			Index = index;
			ParentId = bones.Skeleton.ParentIndex[index];
			Transform = bones.Transforms[index];

			var t = Transform;
			ImGuizmo.RecomposeMatrixFromComponents(ref t.Translate.X, ref t.Rotate.X, ref t.Scale.X, ref Matrix.M11);
		}

		public Vector3 Rotate(Quaternion quat) {
			var t = Transform.Translate;
			return Vector3.Transform(new Vector3(t.X, t.Y, t.Z), quat);
		}
	}
}
