using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Interface.Types;
using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Editor.Popup;

public class EntityRenameModal(SceneEntity entity) : KtisisPopup("##EntityRename", ImGuiWindowFlags.Modal) {
	private bool _isFirstDraw = true;
	
	private string Name = entity.Name;
	
	protected override void OnDraw() {
		ImGui.Text($"Rename '{entity.Name}':");
		
		ImGui.InputText("##NameInput", ref this.Name, 100);
		
		var isValid = this.Name.Length > 0;
		if (isValid && ImGui.IsKeyPressed(ImGuiKey.Enter) && ImGui.IsItemDeactivated())
			this.Confirm();

		if (this._isFirstDraw) {
			this._isFirstDraw = false;
			ImGui.SetKeyboardFocusHere(-1);
		}
		
		ImGui.Spacing();
		
		using (var _ = ImRaii.Disabled(!isValid))
			if (ImGui.Button("Confirm"))
				this.Confirm();
		
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (ImGui.Button("Cancel"))
			this.Close();
	}

	private void Confirm() {
		entity.Name = this.Name;
		this.Close();
	}
}
