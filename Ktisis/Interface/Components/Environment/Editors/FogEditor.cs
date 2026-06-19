using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class FogEditor : EditorBase {
	public override string Name => Ktisis.Locale.Translate("env_edit.fog.title");

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Fog);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox(Ktisis.Locale.Translate("env_edit.enable"), EnvOverride.Fog, module);
		using var _ = this.Disable(module);

		ImGui.ColorEdit4(Ktisis.Locale.Translate("env_edit.fog.color"), ref state.Fog.Color);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.fog.distance"), ref state.Fog.Distance, 0.0f, 1000.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.fog.thickness"), ref state.Fog.Thickness, 0.0f, 100.0f);
		ImGui.Spacing();
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.fog.opacity"), ref state.Fog.Opacity, 0.0f, 1.0f);
		ImGui.SliderFloat(Ktisis.Locale.Translate("env_edit.fog.sky_vis"), ref state.Fog.SkyVisibility, 0.0f, 1.0f);
	}
}
