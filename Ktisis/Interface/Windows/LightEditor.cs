using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.Editor;
using Ktisis.Interface.Types;
using Ktisis.Interop.Structs.Lights;
using Ktisis.Scene.Modules;

namespace Ktisis.Interface.Windows;

public class LightEditor : KtisisWindow {
	private readonly ContextManager _editor;

	private uint Index;

	public LightEditor(
		ContextManager editor
	) : base("Light Editor") {
		this._editor = editor;
	}
	
	public override void Draw() {
		var lightModule = this._editor.Context?.Scene.GetModule<LightModule>();
		if (lightModule != null)
			this.Draw(lightModule);
	}

	private unsafe void Draw(LightModule module) {
		var gpose = module.GetGPoseState();
		if (gpose == null) return;

		var light = gpose->GetLight(this.Index);
		if (ImGui.BeginCombo("##LightSelect", $"{(nint)light:X}")) {
			var lights = gpose->GetLights().ToArray();
			for (uint i = 0; i < lights.Length; i++) {
				if (ImGui.Selectable($"{(nint)lights[i].Value:X}", this.Index == i))
					this.Index = i;
			}
			ImGui.EndCombo();
		}
		
		ImGui.Spacing();
		
		if (light != null && light->RenderLight != null)
			this.DrawEdit(light->RenderLight);
	}

	private unsafe void DrawEdit(RenderLight* light) {
		ImGui.Text($"{(nint)light:X}");
		
		ImGui.Spacing();
		
		if (ImGui.BeginCombo("Type", $"Type {light->Type}")) {
			for (var i = 0; i < 7; i++) {
				var value = (LightType)i;
				if (ImGui.Selectable($"Type {i}", light->Type == value))
					light->Type = value;
			}
			ImGui.EndCombo();
		}
		
		ImGui.Spacing();

		if (ImGui.BeginCombo("Mode", $"Mode {light->Mode}")) {
			for (var i = 0; i < 3; i++) {
				var value = (LightMode)i;
				if (ImGui.Selectable($"Mode {i}", light->Mode == value))
					light->Mode = value;
			}
			ImGui.EndCombo();
		}
		
		ImGui.Spacing();
		
		this.DrawHDR("Red", ref light->Color.X);
		this.DrawHDR("Green", ref light->Color.Y);
		this.DrawHDR("Blue", ref light->Color.Z);
		ImGui.SliderFloat("Intensity", ref light->Color.W, 0, 1);
		
		ImGui.Spacing();

		var pos = (Vector3)light->Transform->Position;
		ImGui.DragFloat3("Position", ref pos);
		
		ImGui.Spacing();

		var first = true;
		foreach (var flag in Enum.GetValues<LightFlags>()) {
			if (first)
				first = false;
			else
				ImGui.SameLine();
			var enabled = light->Flags.HasFlag(flag);
			if (ImGui.Checkbox($"{flag}", ref enabled))
				light->Flags ^= flag;
		}
		
		ImGui.Spacing();

		ImGui.DragFloat("Radius", ref light->Radius, 0.1f, 0, 999);
		ImGui.DragFloat("CharaShadowRange", ref light->CharaShadowRange, 0.1f, 0, 999);
	}

	private void DrawHDR(string label, ref float hdr) {
		var value = (float)Math.Pow(hdr, 2);
		if (ImGui.SliderFloat(label, ref value, 0, 256))
			hdr = (float)Math.Sqrt(value);
	}
}
