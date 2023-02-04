using Ktisis.Posing;
using Ktisis.Services;

using Ktisis.Scene.Interfaces;
using Ktisis.Scene.Skeletons.Bones;

namespace Ktisis.Scene.Skeletons {
	public class ObjectBone : Manipulable, IVisibilityToggle {
		public ObjectBone(SkeletonObject skele, Bone bone) {
			Skeleton = skele;

			BoneName = bone.HkaBone.Name.String ?? "Unknown";
			Partial = bone.Partial;
			Index = bone.Index;
		}

		// Properties

		private SkeletonObject Skeleton;

		private string BoneName;
		internal int Partial;
		internal int Index;

		internal (int p, int i) Pair => (Partial, Index);
		internal string Key => $"{Pair.p},{Pair.i}";

		// Methods

		public bool IsBone(Bone bone) => bone.Partial == Partial && bone.Index == Index;

		public bool ShouldDraw() {
			var res = Visible;
			if (Parent is BoneGroup cat)
				res &= cat.ShouldDraw();
			return res;
		}

		// Manipulable

		public override string Name {
			get => LocaleService.GetBoneName(BoneName);
			set { }
		}

		public override void Context() {}

		// Visibility

		public bool Visible {
			get => Skeleton.VisibilityMap.TryGetValue(Key, out var value) ? value : false;
			set => Skeleton.VisibilityMap[Key] = value;
		}

		// Transformable

		public object? GetTransform() => null;

		public void SetTransform(object trans) { }
	}
}