using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Properties;

public class PresetPropertyList(IEditorContext ctx, LocaleManager locale) : ObjectPropertyList {

	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity.Root is not ActorEntity actor) return;
		
		builder.AddHeader(locale.Translate("preset_edit.title"), () => DrawPresets(actor), priority: 100);
	}

	private string _name = ""; 
	
	private void DrawPresets(ActorEntity actor) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		foreach (var (name, currentState) in actor.GetPresets()) {
			var enabled = currentState;
			ImGui.Checkbox(name, ref enabled);

			if (enabled != currentState) {
				actor.TogglePreset(name, enabled); 
			}
		}
		
		ImGui.Separator();
		ImGui.Text(locale.Translate("preset_edit.add.title"));
		ImGui.InputText(locale.Translate("preset_edit.add.label"), ref _name);
		var isValid = _name.Length > 0 && !ctx.Config.Presets.Presets.ContainsKey(_name);

		if (isValid && ImGui.IsKeyPressed(ImGuiKey.Enter) && ImGui.IsItemDeactivated()) {
			SavePreset(actor);
		}

		using (var _ = ImRaii.Disabled(!isValid)) {
			if (ImGui.Button(locale.Translate("preset_edit.add.save"))) {
				SavePreset(actor);
			}
		}
	}
	private void SavePreset(ActorEntity actor) {
		actor.SavePreset(_name);
		_name = "";
	}
}
