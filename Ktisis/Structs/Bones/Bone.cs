using System.Numerics;

using FFXIVClientStructs.Havok;

using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
	// Making this a class crashes the game for some reason?
	public struct Bone {
		public short Index;
		public short ParentIndex;
		public int _Partial; // Set for Skeleton.SelectedBone only.

		public hkaBone HkaBone;
		public hkQsTransformf Transform;

		public unsafe Vector3 GetWorldPos(ActorModel* model) => model->Position + Transform.Translation.Rotate(model->Rotation) * model->Height;

		public Category Category => Category.GetForBone(HkaBone.Name.String);
	}
}