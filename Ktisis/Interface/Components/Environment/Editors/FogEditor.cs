using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class FogEditor : EditorBase {
	public override string Name { get; } = "Fog";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Fog);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("Enable", EnvOverride.Fog, module);
		using var _ = this.Disable(module);

		ImGui.ColorEdit4("Color", ref state.Fog.Color);
		ImGui.SliderFloat("Distance", ref state.Fog.Distance, 0.0f, 1000.0f);
		ImGui.SliderFloat("Thickness", ref state.Fog.Thickness, 0.0f, 100.0f);
		ImGui.Spacing();
		ImGui.SliderFloat("Sky Opacity", ref state.Fog.Opacity, 0.0f, 1.0f);
		ImGui.SliderFloat("Brightness", ref state.Fog.Brightness, 0.0f, 1.0f);
	}
}
