using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class RainEditor : EditorBase {
	public override string Name => Ktisis.Locale.Translate("env_edit.rain.title");

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Rain);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox(Ktisis.Locale.Translate("env_edit.enable"), EnvOverride.Rain, module);
		using var _ = this.Disable(module);

		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.rain.intensity"), ref state.Rain.Intensity, 0.0f, 1.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.rain.thickness"), ref state.Rain.Size, 0.0f, 1.0f);
		ImGui.ColorEdit4(Ktisis.Locale.Translate("env_edit.rain.color"), ref state.Rain.Color);
		ImGui.Spacing();
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.rain.weight"), ref state.Rain.Weight, 0.0f, 10.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.rain.scattering"), ref state.Rain.Scatter, 0.0f, 10.0f);
		ImGui.Spacing();
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.rain.raindrops"), ref state.Rain.Raindrops, 0.0f, 1.0f);
	}
}
