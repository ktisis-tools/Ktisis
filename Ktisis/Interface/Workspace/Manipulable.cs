using System.Numerics;
using System.Collections.Generic;

using Ktisis.Services;

namespace Ktisis.Interface.Workspace {
	public abstract class Manipulable {
		// Static

		public static Vector4 RootObjectCol = new Vector4(102, 226, 110, 255) / 255f;
		public static Vector4 SubCategoryCol = new Vector4(8, 128, 255, 255) / 255f;

		// Properties

		public List<Manipulable> Children = new();

		// Base methods

		public bool IsSelected() => EditorService.IsSelected(this);

		// Abstract methods

		public abstract void Select();
		public abstract void Context();

		internal abstract void DrawTreeNode();
	}

	public interface Transformable {
		// TODO: Unified class for transform types?
		public abstract object? GetTransform();
		public abstract void SetTransform(object trans);
	}
}