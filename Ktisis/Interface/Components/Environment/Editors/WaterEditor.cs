using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Singleton]
public class WaterEditor : EditorBase, IDisposable {
	public bool Frozen = false;
	private unsafe WaterRendererEx* _renderer;

	public unsafe WaterEditor() {
		var manager = Manager.Instance();
		this._renderer = (WaterRendererEx*)&manager->WaterRenderer;
	}

	public override string Name { get; } = "Water";

	public override bool IsActivated(EnvOverride flags)
		=> this.Frozen;
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		return;
	}

	public unsafe void Draw() {
		ImGui.Checkbox("Enable", ref this.Frozen);
		ImGui.Spacing();
		using var _ = ImRaii.Disabled(!this.Frozen);

		var scrub1 = this._renderer->Unk1;
		if (ImGui.DragFloat("Water One", ref scrub1))
			this._renderer->Unk1 = scrub1;
		var scrub2 = this._renderer->Unk2;
		if (ImGui.DragFloat("Water Two", ref scrub2))
			this._renderer->Unk2 = scrub2;
		var scrub3 = this._renderer->Unk3;
		if (ImGui.DragFloat("Water Three", ref scrub3))
			this._renderer->Unk3 = scrub3;
		var scrub4 = this._renderer->Unk4;
		if (ImGui.DragFloat("Water Four", ref scrub4))
			this._renderer->Unk4 = scrub4;
	}

	public unsafe void Dispose() {
		this._renderer = null;
		this.Frozen = false;
	}
}
