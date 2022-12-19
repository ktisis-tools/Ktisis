using System.Numerics;
using System.Collections.Generic;

namespace Ktisis.Interface.Editor {
	public abstract class Manipulable {
		public static Vector4 RootObjectCol = new Vector4(102, 226, 110, 255) / 255f;
		public static Vector4 SubCategoryCol = new Vector4(8, 128, 255, 255) / 255f;

		public List<Manipulable> Children = new();

		public abstract void Select();
		public abstract void Options();

		internal abstract void DrawWorldNode();
		internal abstract void DrawTreeNode();
	}
}