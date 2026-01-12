using System.Linq;

using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Objects;

namespace Ktisis.Interface.Editor.Popup;

public class WorldObjectPopup(WorldObject obj, float distance, IEditorContext ctx) : KtisisPopup("##WorldObjectPopup") {
	public WorldObject WorldObj;
	protected override void OnDraw() {
		this.WorldObj = obj;
		ImGui.Text($"Object Details");

		ImGui.Separator();
		ImGui.Text($"Model path: {obj.Path}");
		ImGui.Text($"Distance: {distance:0.00}y");

		ImGui.Spacing();
		if (ImGui.Button("Confirm"))
			this.Confirm();

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button("Hide"))
			this.ConfirmAndHide();

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button("Cancel"))
			this.Close();
	}

	private void Confirm() {
		ctx.Scene.Factory
			.BuildObject()
			.SetName($"Object {ctx.Scene.Children.OfType<ObjectEntity>().Count() + 1}")
			.SetAddress(obj.Address)
			.Add();

		this.Close();
	}

	private void ConfirmAndHide() {
		var ent = ctx.Scene.Factory
			.BuildObject()
			.SetName($"Object {ctx.Scene.Children.OfType<ObjectEntity>().Count() + 1}")
			.SetAddress(obj.Address)
			.Add();
		if (ent is ObjectEntity objEntity) {
			objEntity.ToggleHidden();
			objEntity.Visible = false;
		}

		this.Close();
	}
}
