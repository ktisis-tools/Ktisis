using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

public abstract class EditorBase {
	public abstract string Name { get; }

	public abstract bool IsActivated(EnvOverride flags);

	public abstract void Draw(IEnvModule module, ref EnvState state);

	protected ImRaii.IEndObject Disable(IEnvModule module)
		=> ImRaii.Disabled(!this.IsActivated(module.Override));

	protected void DrawToggleCheckbox(string label, EnvOverride flag, IEnvModule module) {
		var active = module.Override.HasFlag(flag);
		if (ImGui.Checkbox(label, ref active))
			module.Override ^= flag;
		ImGui.Spacing();
	}
}
