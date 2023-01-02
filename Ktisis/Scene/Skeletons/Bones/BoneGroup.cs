using Ktisis.Interface;

namespace Ktisis.Scene.Skeletons.Bones {
	public class BoneGroup : Manipulable {
		// BoneGroup

		public BoneCategory? Category;

		// Manipulable

		public override uint Color => 0xFFFF8008;

		public override string GetName() => Category != null ? Category.Name : "INVALID";

		public override void Context() {}

		public override void Select() {}

		// Overrides

		public override unsafe bool PreDraw() {
			if (Category != null && Category.IsNsfw)
				return !Ktisis.Configuration.CensorNsfw;
			return true;
		}
	}
}