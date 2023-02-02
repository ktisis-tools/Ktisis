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

			var transObj = select.GetTransform();
			if (transObj is Transform trans) {
				if (Table.Draw(ref trans))
					select.SetTransform(trans);
			} else if (transObj is Vector3 vec) {
				// TODO
			} else Close();
		}
	}
}