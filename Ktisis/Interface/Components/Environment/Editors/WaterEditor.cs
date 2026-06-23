using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Services.Game;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Singleton]
public class WaterEditor : EditorBase, IDisposable {
	private GPoseService _gpose;
	private readonly IFramework _framework;
	private unsafe WaterRendererEx* _renderer;

	public bool Frozen = false;

	public unsafe WaterEditor(
		IFramework framework,
		GPoseService gpose
	) {
		this._framework = framework;
		this._gpose = gpose;

		var manager = Manager.Instance();
		this._renderer = (WaterRendererEx*)&manager->WaterRenderer;

		// delay registration by 1 tick to prevent enumeration error as this may be initialized during GPoseService's action
		this._framework.RunOnTick(() => {
			this._gpose.StateChanged += this.OnGPoseEvent;
			this._gpose.Subscribe();
		}, delayTicks:1);
	}

	public override string Name => Ktisis.Locale.Translate("env_edit.water.title");

	public override bool IsActivated(EnvOverride flags)
		=> this.Frozen;
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		return;
	}

	public unsafe void Draw() {
		ImGui.Checkbox(Ktisis.Locale.Translate("env_edit.enable"), ref this.Frozen);
		ImGui.Spacing();
		using var _ = ImRaii.Disabled(!this.Frozen);

		var scrub1 = this._renderer->Unk1;
		if (ImGui.DragFloat(Ktisis.Locale.Translate("env_edit.water.water_one"), ref scrub1))
			this._renderer->Unk1 = scrub1;
		var scrub2 = this._renderer->Unk2;
		if (ImGui.DragFloat(Ktisis.Locale.Translate("env_edit.water.water_two"), ref scrub2))
			this._renderer->Unk2 = scrub2;
		var scrub3 = this._renderer->Unk3;
		if (ImGui.DragFloat(Ktisis.Locale.Translate("env_edit.water.water_three"), ref scrub3))
			this._renderer->Unk3 = scrub3;
		var scrub4 = this._renderer->Unk4;
		if (ImGui.DragFloat(Ktisis.Locale.Translate("env_edit.water.water_four"), ref scrub4))
			this._renderer->Unk4 = scrub4;
	}

	private void OnGPoseEvent(object sender, bool active) {
		if (!active) this.Frozen = false;
	}

	public unsafe void Dispose() {
		this._renderer = null;
		this.Frozen = false;
		this._gpose.StateChanged -= this.OnGPoseEvent;
	}
}
