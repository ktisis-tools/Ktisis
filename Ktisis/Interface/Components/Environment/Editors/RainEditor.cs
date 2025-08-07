using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class RainEditor : EditorBase {
	public override string Name { get; } = "Rain";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Rain);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("Enable", EnvOverride.Rain, module);
		using var _ = this.Disable(module);

		ImGui.SliderFloat("Intensity", ref state.Rain.Intensity, 0.0f, 1.0f);
		ImGui.SliderFloat("Thickness", ref state.Rain.Size, 0.0f, 1.0f);
		ImGui.ColorEdit4("Color", ref state.Rain.Color);
		ImGui.Spacing();
		ImGui.SliderFloat("Weight", ref state.Rain.Weight, 0.0f, 10.0f);
		ImGui.SliderFloat("Scattering", ref state.Rain.Scatter, 0.0f, 10.0f);
		ImGui.Spacing();
		ImGui.SliderFloat("Raindrops", ref state.Rain.Raindrops, 0.0f, 1.0f);
	}
}
