using System.Linq;

using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Editor.Popup;

public class SpawnObjectPopup(IEditorContext ctx) : KtisisPopup("##SpawnObjectPopup", ImGuiWindowFlags.Modal) {
	private string ModelPath = "";

	protected override void OnDraw() {
		ImGui.Text(Ktisis.Locale.Translate("popups.spawn_obj.header"));
		using (ImRaii.Disabled()) {
			ImGui.Text(Ktisis.Locale.Translate("popups.spawn_obj.explain_1"));
			ImGui.Text(Ktisis.Locale.Translate("popups.spawn_obj.explain_2"));
		}

		ImGui.Spacing();
		ImGui.Text($"{Ktisis.Locale.Translate("popups.spawn_obj.path")}:");
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		ImGui.InputText("##spawnerinputtext", ref this.ModelPath);
		ImGui.Spacing();

		using (ImRaii.Disabled(this.ModelPath.IsNullOrEmpty()))
			if (ImGui.Button(Ktisis.Locale.Translate("popups.spawn_obj.spawn")))
				this.Confirm();
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button(Ktisis.Locale.Translate("popups.spawn_obj.close")))
			this.Close();
	}

	private unsafe void Confirm() {
		var ptr = ctx.Scene.World.BuildObject(this.ModelPath);
		if (ptr is null) {
			Ktisis.WarningNotification($"Could not create object with path: {this.ModelPath}");
			this.Close();
		}

		var name = this.ModelPath.Split("/").Last().Split(".").First();
		var entity = ctx.Scene.Factory
			.BuildObject()
			.SetName(name)
			.SetAddress(ptr)
			.Add();
		entity.SetTransform(new Transform(ctx.Scene.GetSceneOrigin()));
		this.Close();
	}
}
