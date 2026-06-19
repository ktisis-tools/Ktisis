using Dalamud.Interface.Utility.Raii;

using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class LightingEditor : EditorBase {
	public override string Name { get; } = Ktisis.Locale.Translate("env_edit.lighting.title");

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Lighting);

	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox(Ktisis.Locale.Translate("env_edit.enable"), EnvOverride.Lighting, module);
		using var _ = this.Disable(module);

		using var _disable = ImRaii.Disabled(!module.Override.HasFlag(EnvOverride.Lighting));
		ImGui.ColorEdit3(Ktisis.Locale.Translate("env_edit.lighting.sunlight"), ref state.Lighting.SunLightColor);
		ImGui.ColorEdit3(Ktisis.Locale.Translate("env_edit.lighting.moonlight"), ref state.Lighting.MoonLightColor);
		ImGui.ColorEdit3(Ktisis.Locale.Translate("env_edit.lighting.ambient"), ref state.Lighting.Ambient);
		ImGui.SliderFloat(Ktisis.Locale.Translate("common.unknown") + " #1", ref state.Lighting._unk1, 0.0f, 10.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.lighting.saturation"), ref state.Lighting.AmbientSaturation, 0.0f, 5.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.lighting.temperature"), ref state.Lighting.Temperature, -2.5f, 2.5f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("common.unknown") + " #2", ref state.Lighting._unk2, 0.0f, 100.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("common.unknown") + " #3", ref state.Lighting._unk3, 0.0f, 100.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("common.unknown") + " #4", ref state.Lighting._unk4, 0.0f, 1.0f);
	}
}