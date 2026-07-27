using System.Linq;

using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Objects;

namespace Ktisis.Interface.Editor.Popup;

public class SpawnObjectPopup(IEditorContext ctx) : KtisisPopup("##SpawnObjectPopup", ImGuiWindowFlags.Modal) {
	private string ModelPath = "";

	protected override void OnDraw() {
		ImGui.Text("Spawn Object");
		using (ImRaii.Disabled()) {
			ImGui.Text("Enter a valid .mdl path below to spawn a new BgObject.");
			ImGui.TextWrapped("File paths can be sourced from Pathfinder, Textools, Brio, and Ktisis' world objects.");
		}

		ImGui.Spacing();
		ImGui.InputText("Path:", ref this.ModelPath);
		ImGui.Spacing();
		using (ImRaii.Disabled(this.ModelPath.IsNullOrEmpty()))
			if (ImGui.Button("Load"))
				this.Confirm();
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button("Close"))
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
