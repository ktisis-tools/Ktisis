using System.Numerics;

using ImGuizmoNET;

using Ktisis.Structs.Havok;

namespace Ktisis.Structs.Ktisis {
	public class Bone {
		public int Index;
		public short ParentId;
		public Transform Transform;

		public HkaBone HkaBone;

		public SharpDX.Matrix Matrix;

		public Bone(BoneList bones, int index) {
			Index = index;
			ParentId = bones.Skeleton.ParentIndex[index];
			Transform = bones.Transforms[index];

			HkaBone = bones.Skeleton.Bones[index];

			var t = Transform;
			ImGuizmo.RecomposeMatrixFromComponents(ref t.Translate.X, ref t.Rotate.X, ref t.Scale.X, ref Matrix.M11);
		}

		public Vector3 Rotate(Quaternion quat) {
			var t = Transform.Translate;
			return Vector3.Transform(new Vector3(t.X, t.Y, t.Z), quat);
		}

		public void TransformChildren(BoneList bones, Transform transform) {
			var children = bones.GetChildren(this);
			foreach (var child in children) {
				// TODO: +=
				child.Transform.Translate += transform.Translate;
				//child.Transform.Rotate += transform.Rotate;
				//child.Transform.Scale += transform.Scale;
				bones.Transforms[child.Index] = child.Transform;

				child.TransformChildren(bones, transform);
			}
		}
	}
}
