using Ktisis.Services;
using Ktisis.Structs.Bones;

namespace Ktisis.Scene.Skeletons {
	public class ObjectBone : Manipulable, Transformable {
		public ObjectBone(Bone bone) {
			BoneName = bone.HkaBone.Name.String ?? "Unknown";
			Partial = bone.Partial;
			Index = bone.Index;
		}

		// Properties

		private string BoneName;
		internal int Partial;
		internal int Index;

		// Methods

		public bool IsBone(Bone bone) => bone.Partial == Partial && bone.Index == Index;

		// Manipulable

		public override string Name {
			get => LocaleService.GetBoneName(BoneName);
			set { }
		}

		public override void Context() {}

		public override void Select() {}

		// Transformable

		public object? GetTransform() => null;

		public void SetTransform(object trans) { }
	}
}