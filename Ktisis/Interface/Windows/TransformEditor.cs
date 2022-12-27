using System.Numerics;

using ImGuiNET;

using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows {
	public class TransformEditor : KtisisWindow {
		private TransformTable Table = new();

		public TransformEditor() : base(
			"Transform Editor",
			ImGuiWindowFlags.AlwaysAutoResize
		) { /* heehoo */ }

		public override void Draw() {
			Table.Draw(new Matrix4x4()); // TODO
		}
	}
}