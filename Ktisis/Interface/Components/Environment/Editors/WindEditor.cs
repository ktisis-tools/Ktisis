using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class WindEditor : EditorBase {
	public override string Name { get; } = "Wind";

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Wind);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("Enable", EnvOverride.Wind, module);
		using var _ = this.Disable(module);
		
		this.DrawAngle("Direction", ref state.Wind.Direction, 0.0f, 360.0f);
		this.DrawAngle("Angle", ref state.Wind.Angle, 0.0f, 180.0f);
		ImGui.SliderFloat("Speed", ref state.Wind.Speed, 0.0f, 1.5f);
	}

	private void DrawAngle(string label, ref float angle, float min, float max) {
		var rad = angle * MathHelpers.Deg2Rad;
		var changed = ImGui.SliderAngle(label, ref rad, min, max);
		if (changed) angle = rad * MathHelpers.Rad2Deg;
	}
}
