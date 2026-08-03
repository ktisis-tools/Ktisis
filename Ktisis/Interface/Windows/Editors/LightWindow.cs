using System;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

using GLib.Popups;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Gobos;
using Ktisis.Data.Serialization;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Windows.Editors;

public class LightWindow : EntityEditWindow<LightEntity> {
	private readonly LocaleManager _locale;
	private readonly ITextureProvider _tex;
	private readonly GoboSchema _goboSchema;
	private readonly PopupList<GoboEntry> _goboPopup;
	private GoboEntry? Gobo;

	public LightWindow(
		IEditorContext ctx,
		ITextureProvider tex,
		LocaleManager locale
	) : base("Light Editor", ctx, windowId:"###KtisisLightEditor") {
		this._tex = tex;
		this._locale = locale;
		this._goboSchema = SchemaReader.ReadGobos();
		this._goboPopup = new PopupList<GoboEntry>(
			"##GoboPopup",
			this.DrawGoboRow
		).WithSearch(GoboSearchPredicate);
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
		this.UpdateTarget();
		
		var s = this.Context.Selection;
		if (s.Count == 1) {
			var p = s.GetSelected().First();
			if (p is LightEntity l)
				this.SetTarget(l);
		}

		var entity = this.Target;
		
		ImGui.Text($"Editing Light: {entity.Name}");
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

	internal unsafe void DrawLightTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		this.DrawLightFlag(Ktisis.Locale.Translate("object_edit.light.light.reflection"), light, LightFlags.Reflection);
		ImGui.Spacing();
		
		// Light type
		
		var lightTypePreview = this._locale.Translate($"lightType.{light->LightType}");
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.light.light.type"), lightTypePreview)) {
			foreach (var value in Enum.GetValues<LightType>()) {
				var valueLabel = this._locale.Translate($"lightType.{value}");
				if (ImGui.Selectable(valueLabel, light->LightType == value)) {
					if (value is not (LightType.SpotLight or LightType.AreaLight))
						entity.RemoveGobo();
					light->LightType = value;
				}
			}
			ImGui.EndCombo();
		}
		
		switch (light->LightType) {
			case LightType.SpotLight:
				ImGui.SliderFloat($"{Ktisis.Locale.Translate("object_edit.light.light.spot.angle")}##LightAngle", ref light->LightAngle, 0.0f, 180.0f, "%0.0f deg");
				ImGui.SliderFloat($"{Ktisis.Locale.Translate("object_edit.light.light.spot.falloff")}##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f deg");
				break;
			case LightType.AreaLight:
				var angleSpace = ImGui.GetStyle().ItemInnerSpacing.X;
				var angleWidth = ImGui.CalcItemWidth() / 2 - angleSpace;
				using (var _ = ImRaii.ItemWidth(angleWidth)) {
					ImGui.SliderAngle("##AngleX", ref light->AreaAngle.X, -90, 90);
					ImGui.SameLine(0, angleSpace);
					ImGui.SliderAngle($"{Ktisis.Locale.Translate("object_edit.light.light.area.angle")}##AngleY", ref light->AreaAngle.Y, -90, 90);
				}
				ImGui.SliderFloat($"{Ktisis.Locale.Translate("object_edit.light.light.area.falloff")}##LightAngle", ref light->FalloffAngle, 0.0f, 180.0f, "%0.0f deg");
				break;
			
		}
		
		ImGui.Spacing();
		
		// Falloff
		
		var falloffPreview = this._locale.Translate($"lightFalloff.{light->FalloffType}");
		if (ImGui.BeginCombo(Ktisis.Locale.Translate("object_edit.light.light.falloff.type"), falloffPreview)) {
			foreach (var value in Enum.GetValues<FalloffType>()) {
				var valueLabel = this._locale.Translate($"lightFalloff.{value}");
				if (ImGui.Selectable(valueLabel, light->FalloffType == value))
					light->FalloffType = value;
			}
			ImGui.EndCombo();
		}

		ImGui.DragFloat($"{Ktisis.Locale.Translate("object_edit.light.light.falloff.power")}##FalloffPower", ref light->Falloff, 0.01f, 0.0f, 1000.0f);
		
		// Base light settings
		
		ImGui.Spacing();
		var color = light->Color.RGB;
		if (ImGui.ColorEdit3(Ktisis.Locale.Translate("object_edit.light.light.color"), ref color, ImGuiColorEditFlags.Hdr | ImGuiColorEditFlags.Uint8))
			light->Color.RGB = color;
		ImGui.DragFloat(Ktisis.Locale.Translate("object_edit.light.light.intensity"), ref light->Color.Intensity, 0.01f, 0.0f, 100.0f);
		if (ImGui.DragFloat($"{Ktisis.Locale.Translate("object_edit.light.light.range")}##LightRange", ref light->Range, 0.1f, 0, 999))
			entity.Flags |= LightEntityFlags.Update;

		// RenderLight projection settings
		ImGui.Spacing();
		ImGui.AlignTextToFramePadding();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(Ktisis.Locale.Translate("object_edit.light.light.gobos.info"));
		}
		ImGui.SameLine();
		using (ImRaii.Disabled(light->LightType is (LightType.Directional or LightType.PointLight))) {
			var tooltip = Ktisis.Locale.Translate("object_edit.light.light.gobos.choose");
			if (entity.Gobo != null)
				tooltip += Ktisis.Locale.Translate("object_edit.light.light.gobos.remove");
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Image, tooltip))
				this._goboPopup.Open();
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				entity.RemoveGobo();

			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
			ImGui.Text($"{Ktisis.Locale.Translate("object_edit.light.light.gobos.current")} {(entity.Gobo == null ? "N/A" : entity.Gobo.Name)}");
		}

		ImGui.Spacing();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, Ktisis.Locale.Translate("object_edit.light.light.import")))
			this.Context.Interface.OpenLightFile((path, file) => this.Context.Scene.ApplyLightFile(entity, file));

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Save, Ktisis.Locale.Translate("object_edit.light.light.export")))
			this.Context.Interface.OpenLightExport(entity);

		this.DrawGoboPopup(entity);
	}

	internal unsafe void DrawShadowsTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		this.DrawLightFlag(Ktisis.Locale.Translate("object_edit.light.shadow.dynamic"), light, LightFlags.Dynamic);
		ImGui.Spacing();
		
		this.DrawLightFlag(Ktisis.Locale.Translate("object_edit.light.shadow.chara"), light, LightFlags.CharaShadow);
		this.DrawLightFlag(Ktisis.Locale.Translate("object_edit.light.shadow.object"), light, LightFlags.ObjectShadow);

		ImGui.Spacing();
		ImGui.DragFloat(Ktisis.Locale.Translate("object_edit.light.shadow.range"), ref light->CharaShadowRange, 0.1f, 0.0f, 1000.0f);
		ImGui.Spacing();
		ImGui.DragFloat(Ktisis.Locale.Translate("object_edit.light.shadow.near"), ref light->ShadowNear, 0.01f, 0.0f, 1000.0f);
		ImGui.DragFloat(Ktisis.Locale.Translate("object_edit.light.shadow.far"), ref light->ShadowFar, 0.01f, 0.0f, 1000.0f);
	}

	private unsafe void DrawGoboPopup(LightEntity entity) {
		if (!this._goboPopup.IsOpen) return;
		if (!this._goboPopup.Draw(this._goboSchema.Gobos, this._goboSchema.Gobos.Count, out var selected, CalcItemHeight())) return;

		entity.SetGobo(selected!);
	}

	private static float CalcItemHeight() => (ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemInnerSpacing.Y) * 2;
	private bool DrawGoboRow(GoboEntry gobo, bool isFocus) {
		var height = CalcItemHeight();
		var space = ImGui.GetStyle().ItemInnerSpacing.X;
		var cursor = ImGui.GetCursorPosX();
		var result = ImGui.Button(string.Empty, new Vector2(ImGui.GetContentRegionAvail().X, height));
		ImGui.SameLine(cursor, height+space);
		ImGui.Text(gobo.Name);

		ImGui.SameLine(cursor, height+space);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight());
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.Text).SetAlpha(0xAF)))
			ImGui.Text(gobo.Path);

		ImGui.SameLine(cursor);
		var size = new Vector2(height, height);
		ISharedImmediateTexture? img = null;
		try {
			img = this._tex.GetFromGame(gobo.Path);
		} catch {
			Ktisis.Log.Error($"[LightPropertyList] Couldn't resolve ITextureProvider path for gobo!\n{gobo.Name} @ {gobo.Path}");
		}
		if (img != null)
			ImGui.Image(img.GetWrapOrEmpty().Handle, size);
		else
			ImGui.Dummy(size);

		return result;
	}

	private static bool GoboSearchPredicate(GoboEntry gobo, string query)
		=> gobo.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || gobo.Path.Contains(query, StringComparison.OrdinalIgnoreCase);
	
	private unsafe void DrawLightFlag(string label, RenderLight* light, LightFlags flag) {
		var active = light->Flags.HasFlag(flag);
		if (ImGui.Checkbox(label, ref active))
			light->Flags ^= flag;
	}
}
