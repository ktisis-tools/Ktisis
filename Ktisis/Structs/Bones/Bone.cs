using System.Numerics;

using FFXIVClientStructs.Havok;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
	public struct Bone {
		public short Index;
		public short ParentIndex;

		public hkaBone HkaBone;
		public hkQsTransformf Transform;

		public unsafe Vector3 GetWorldPos(ActorModel* model) => model->Position + Transform.Translation.Rotate(model->Rotation) * model->Height;
	}
}