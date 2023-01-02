using Ktisis.Structs.Bones;
using Ktisis.Interface.Localization;

namespace Ktisis.Scene.Skeletons {
	public class ObjectBone : Manipulable, Transformable {
		public ObjectBone(Bone bone) {
			Name = bone.HkaBone.Name.String;
			Partial = bone.Partial;
			Index = bone.Index;
		}

		// Properties

		private string Name;
		internal int Partial;
		internal int Index;

		// Methods

		public bool IsBone(Bone bone) => bone.Partial == Partial && bone.Index == Index;

		// Manipulable

		public unsafe override string GetName()
			=> Locale.GetBoneName(Name);

		public override void Context() {}

		public override void Select() {}

		// Transformable

		public object? GetTransform() => null;

		public void SetTransform(object trans) { }
	}
}