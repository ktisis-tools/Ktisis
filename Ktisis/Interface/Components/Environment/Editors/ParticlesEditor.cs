using System;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Scene.Modules;
using Ktisis.Structs.Env;

namespace Ktisis.Interface.Components.Environment.Editors;

[Transient]
public class ParticlesEditor : EditorBase {
	private readonly SetTextureSelect _texDust;
	
	public override string Name { get; } = "Particles";

	public ParticlesEditor(
		SetTextureSelect texDust
	) {
		this._texDust = texDust;
	}

	public override bool IsActivated(EnvOverride flags)
		=> flags.HasFlag(EnvOverride.Dust);
	
	public override void Draw(IEnvModule module, ref EnvState state) {
		this.DrawToggleCheckbox("Enable", EnvOverride.Dust, module);
		using var _ = this.Disable(module);

		ImGui.SliderFloat("Intensity", ref state.Dust.Intensity, 0.0f, 1.0f);
		ImGui.SliderFloat("Size", ref state.Dust.Size, 0.0f, 10.0f);
		ImGui.SliderFloat("Glow", ref state.Dust.Glow, 0.0f, 10.0f);
		ImGui.ColorEdit4("Color", ref state.Dust.Color);
		ImGui.Spacing();
		ImGui.SliderFloat("Weight", ref state.Dust.Weight, 0.0f, 10.0f);
		ImGui.Spacing();
		ImGui.SliderFloat("Spread", ref state.Dust.Spread, 0.0f, 10.0f);
		ImGui.SliderFloat("Speed", ref state.Dust.Speed, 0.0f, 1.0f);
		ImGui.SliderFloat("Spin", ref state.Dust.Spin, 0.05f, 5.0f);
		ImGui.Spacing();
		this._texDust.Draw("Texture", ref state.Dust.TextureId, this.ResolvePath);
	}

	private string ResolvePath(uint id) => id switch {
		1 => "bgcommon/nature/snow/texture/snow.tex",
		_ => $"bgcommon/nature/dust/texture/dust_{Math.Max(0, id - 2):D3}.tex",
	};
}
