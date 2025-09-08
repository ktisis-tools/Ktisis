using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class StarsEditor : EditorBase {
	public override string Name { get; } = "Stars";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Stars);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("Enable", EnvOverride.Stars, module);
		using var _ = this.Disable(module);

		ImGui.SliderFloat("Stars", ref state.Stars.Stars, 0.0f, 20.0f);
		ImGui.SliderFloat("Intensity##1", ref state.Stars.StarIntensity, 0.0f, 2.5f);
		ImGui.Spacing();
		ImGui.SliderFloat("Constellations", ref state.Stars.Constellations, 0.0f, 10.0f);
		ImGui.SliderFloat("Intensity##2", ref state.Stars.ConstellationIntensity, 0.0f, 2.5f);
		ImGui.Spacing();
		ImGui.SliderFloat("Galaxy Intensity", ref state.Stars.GalaxyIntensity, 0.0f, 10.0f);
		ImGui.Spacing();
		ImGui.ColorEdit4("Moon Color", ref state.Stars.MoonColor);
		ImGui.SliderFloat("Moon Brightness", ref state.Stars.MoonBrightness, 0.0f, 1.0f);
	}
}
