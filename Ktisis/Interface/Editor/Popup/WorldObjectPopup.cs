using System.Linq;

using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Objects;

namespace Ktisis.Interface.Editor.Popup;

public class WorldObjectPopup(WorldObject obj, float distance, IEditorContext ctx) : KtisisPopup("##WorldObjectPopup") {
	public WorldObject WorldObj;
	protected override void OnDraw() {
		this.WorldObj = obj;
		ImGui.Text(Ktisis.Locale.Translate("popups.world_obj.header"));
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		using (ImRaii.Disabled())
			ImGui.Text($"\t{Ktisis.Locale.Translate("popups.world_obj.addr")}: {obj.Address:X}");

		ImGui.Separator();
		ImGui.Text($"{Ktisis.Locale.Translate("popups.world_obj.path")}: {obj.Path}");
		ImGui.Text($"{Ktisis.Locale.Translate("popups.world_obj.dist")}: {distance:0.00}y");

		ImGui.Spacing();
		if (ImGui.Button(Ktisis.Locale.Translate("popups.world_obj.add")))
			this.Confirm();

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button(Ktisis.Locale.Translate("popups.world_obj.hide")))
			this.ConfirmAndHide();

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button(Ktisis.Locale.Translate("popups.world_obj.cancel")))
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
