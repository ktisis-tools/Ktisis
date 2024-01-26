using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Windows.Editors;

public class LightWindow : EntityEditWindow<LightEntity> {
	private readonly LocaleManager _locale;

	public LightWindow(
		IEditorContext ctx,
		LocaleManager locale
	) : base("Light Editor", ctx) {
		this._locale = locale;
	}

	// Draw handlers
	
	public override void PreDraw() {
		base.PreDraw();
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(400, 300),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.90f
		};
	}
	
	public override void Draw() {
		var s = this.Context.Selection;
		if (s.Count == 1) {
			var p = s.GetSelected().First();
			if (p is LightEntity l)
				this.SetTarget(l);
		}

		var entity = this.Target;
		
		ImGui.Text($"{entity.Name}:");
		ImGui.Spacing();

		using var _ = ImRaii.TabBar("##LightEditTabs");
		this.DrawTab("Light", this.DrawLightTab, entity);
		this.DrawTab("Shadows", this.DrawShadowsTab, entity);
	}
	
	// Tabs

	private void DrawTab(string label, Action<LightEntity> draw, LightEntity entity) {
		using var _tab = ImRaii.TabItem(label);
		if (_tab.Success) draw.Invoke(entity);
	}
	
	// Light Tab

	private unsafe void DrawLightTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		ImGui.Spacing();
		this.DrawLightFlag("Enable reflections", light, LightFlags.Reflection);
		ImGui.Spacing();
		
		// Light type
		
		var lightTypePreview = this._locale.Translate($"lightType.{light->LightType}");
		if (ImGui.BeginCombo("Light Type", lightTypePreview)) {
			foreach (var value in Enum.GetValues<LightType>()) {
				var valueLabel = this._locale.Translate($"lightType.{value}");
				if (ImGui.Selectable(valueLabel, light->LightType == value))
					light->LightType = value;
			}
			ImGui.EndCombo();
		}
		
		switch (light->LightType) {
			case LightType.SpotLight:
				ImGui.SliderFloat("Cone Angle##LightAngle", ref light->LightAngle, 0.0f, 180.0f, "%0.0f deg");
				ImGui.SliderFloat("Falloff Angle##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f deg");
				break;
			case LightType.AreaLight:
				var angleSpace = ImGui.GetStyle().ItemInnerSpacing.X;
				var angleWidth = ImGui.CalcItemWidth() / 2 - angleSpace;
				ImGui.PushItemWidth(angleWidth);
				ImGui.SliderAngle("##AngleX", ref light->AreaAngle.X, -90, 90);
				ImGui.SameLine(0, angleSpace);
				ImGui.SliderAngle("Light Angle##AngleY", ref light->AreaAngle.Y, -90, 90);
				ImGui.PopItemWidth();
				ImGui.SliderFloat("Falloff Angle##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f deg");
				break;
		}
		
		ImGui.Spacing();
		
		// Falloff
		
		var falloffPreview = this._locale.Translate($"lightFalloff.{light->FalloffType}");
		if (ImGui.BeginCombo("Falloff Type", falloffPreview)) {
			foreach (var value in Enum.GetValues<FalloffType>()) {
				var valueLabel = this._locale.Translate($"lightFalloff.{value}");
				if (ImGui.Selectable(valueLabel, light->FalloffType == value))
					light->FalloffType = value;
			}
			ImGui.EndCombo();
		}

		ImGui.DragFloat("Falloff Power##FalloffPower", ref light->Falloff, 0.01f, 0.0f, 1000.0f);
		
		// Base light settings
		
		ImGui.Spacing();
		
		var color = light->Color.RGB;
		if (ImGui.ColorEdit3("Color", ref color, ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.Uint8))
			light->Color.RGB = color;
		ImGui.DragFloat("Intensity", ref light->Color.Intensity, 0.01f, 0.0f, 100.0f);
		if (ImGui.DragFloat("Range##LightRange", ref light->Range, 0.1f, 0, 999))
			entity.Flags |= LightEntityFlags.Update;
	}
	
	// Shadows tab

	private unsafe void DrawShadowsTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		ImGui.Spacing();
		this.DrawLightFlag("Dynamic shadows", light, LightFlags.Dynamic);
		ImGui.Spacing();
		
		this.DrawLightFlag("Cast character shadows", light, LightFlags.CharaShadow);
		this.DrawLightFlag("Cast object shadows", light, LightFlags.ObjectShadow);

		ImGui.Spacing();
		ImGui.DragFloat("Shadow Range", ref light->CharaShadowRange, 0.1f, 0.0f, 1000.0f);
		ImGui.Spacing();
		ImGui.DragFloat("Shadow Near", ref light->ShadowNear, 0.01f, 0.0f, 1000.0f);
		ImGui.DragFloat("Shadow Far", ref light->ShadowFar, 0.01f, 0.0f, 1000.0f);
	}
	
	// Utility
	
	private unsafe void DrawLightFlag(string label, RenderLight* light, LightFlags flag) {
		var active = light->Flags.HasFlag(flag);
		if (ImGui.Checkbox(label, ref active))
			light->Flags ^= flag;
	}
}
