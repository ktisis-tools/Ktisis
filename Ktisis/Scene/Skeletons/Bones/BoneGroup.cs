using Ktisis.Posing;
using Ktisis.Scene.Interfaces;

namespace Ktisis.Scene.Skeletons.Bones {
	public class BoneGroup : Manipulable, IVisibilityToggle {
		public BoneGroup(SkeletonObject skele) {
			Skeleton = skele;
		}

		// BoneGroup

		private SkeletonObject Skeleton;

		public BoneCategory? Category;

		public bool ShouldDraw() => !(Category != null && Category.IsNsfw && Ktisis.Configuration.CensorNsfw);

		// Manipulable

		public override uint Color => 0xFFFF8008;

		public override string Name {
			get => Category != null ? Category.Name : "INVALID";
			set { }
		}

		public override void Context() {}

		public override void Select() {}

		// Visibility

		public bool Visible {
			get => Skeleton.VisibilityMap.TryGetValue(Name, out var value) ? value : false;
			set => Skeleton.VisibilityMap[Name] = value;
		}

		// Overrides

		public override unsafe bool PreDraw() {
			if (!ShouldDraw())
				return false;
			return true;
		}
	}
}