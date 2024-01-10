using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class LightingEditor : EditorBase {
	public override string Name { get; } = "Light";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Lighting);

	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("Enable", EnvOverride.Lighting, module);
		using var _ = this.Disable(module);

		using var _disable = ImRaii.Disabled(!module.Override.HasFlag(EnvOverride.Lighting));
		ImGui.ColorEdit3("Sunlight", ref state.Lighting.SunLightColor);
		ImGui.ColorEdit3("Moonlight", ref state.Lighting.MoonLightColor);
		ImGui.ColorEdit3("Ambient", ref state.Lighting.Ambient);
		ImGui.SliderFloat("Unknown #1", ref state.Lighting._unk1, 0.0f, 10.0f);
		ImGui.SliderFloat("Saturation", ref state.Lighting.AmbientSaturation, 0.0f, 5.0f);
		ImGui.SliderFloat("Temperature", ref state.Lighting.Temperature, -2.5f, 2.5f);
		ImGui.SliderFloat("Unknown #2", ref state.Lighting._unk2, 0.0f, 100.0f);
		ImGui.SliderFloat("Unknown #3", ref state.Lighting._unk3, 0.0f, 100.0f);
		ImGui.SliderFloat("Unknown #4", ref state.Lighting._unk4, 0.0f, 1.0f);
	}
}