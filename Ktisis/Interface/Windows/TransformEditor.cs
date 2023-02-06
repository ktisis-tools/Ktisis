using System.Numerics;

using ImGuiNET;

using Ktisis.Posing;
using Ktisis.Services;
using Ktisis.Scene.Interfaces;
using Ktisis.Interface.Components;

namespace Ktisis.Interface.Windows {
	public class TransformEditor : KtisisWindow {
		private TransformTable Table = new();

		public TransformEditor() : base(
			"Transform Editor",
			ImGuiWindowFlags.AlwaysAutoResize
		) { /* heehoo */ }

		public override void Draw() {
			var select = (ITransformable?)EditorService.Selections.Find(i => i is ITransformable);
			if (select == null) {
				Close();
				return;
			}

			var trans = select.GetTransform();
			if (trans != null && Table.Draw(ref trans))
				select.SetTransform(trans);
		}
	}
}