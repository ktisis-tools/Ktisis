using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

using GLib.Popups;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Gobos;
using Ktisis.Data.Serialization;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Modules.Lights;

namespace Ktisis.Interface.Editor.Properties;

public class LightPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly ITextureProvider _tex;
	private readonly LocaleManager _locale;
	private readonly GoboSchema _goboSchema;
	private readonly PopupList<GoboEntry> _goboPopup;
	private GoboEntry? Gobo;

	public LightPropertyList(
		IEditorContext ctx,
		ITextureProvider tex,
		LocaleManager locale
	) {
		this._ctx = ctx;
		this._tex = tex;
		this._locale = locale;
		this._goboSchema = SchemaReader.ReadGobos();
		this._goboPopup = new PopupList<GoboEntry>(
			"##GoboPopup",
			this.DrawGoboRow
		).WithSearch(GoboSearchPredicate);
	}
	
	public unsafe override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (entity is not LightEntity light)
			return;
		ImGui.Text($"DEBUG: SceneLight {light.Address:X} | RenderLight {(uint)light.GetObject()->RenderLight:X}");
		
		builder.AddHeader("Light", () => this.DrawLightTab(light));
		builder.AddHeader("Shadows", () => this.DrawShadowsTab(light));
	}

	private unsafe void DrawLightTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
		this.DrawLightFlag("Enable reflections", light, LightFlags.Reflection);
		ImGui.Spacing();
		
		// Light type
		
		var lightTypePreview = this._locale.Translate($"lightType.{light->LightType}");
		if (ImGui.BeginCombo("Light Type", lightTypePreview)) {
			foreach (var value in Enum.GetValues<LightType>()) {
				var valueLabel = this._locale.Translate($"lightType.{value}");
				if (ImGui.Selectable(valueLabel, light->LightType == value)) {
					// if (value == LightType.PointLight) {
					// 	Ktisis.Log.Info($"switching to PointLight, resetting texture");
					// 	this._ctx.Scene.GetModule<LightModule>()?.UpdateSceneLightTexture(sceneLight, "garbage.tex\0");
					// }
					light->LightType = value;
				}
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
				using (var _ = ImRaii.ItemWidth(angleWidth)) {
					ImGui.SliderAngle("##AngleX", ref light->AreaAngle.X, -90, 90);
					ImGui.SameLine(0, angleSpace);
					ImGui.SliderAngle("Light Angle##AngleY", ref light->AreaAngle.Y, -90, 90);
				}
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
		if (ImGui.ColorEdit3("Color", ref color, ImGuiColorEditFlags.Hdr | ImGuiColorEditFlags.Uint8))
			light->Color.RGB = color;
		ImGui.DragFloat("Intensity", ref light->Color.Intensity, 0.01f, 0.0f, 100.0f);
		if (ImGui.DragFloat("Range##LightRange", ref light->Range, 0.1f, 0, 999))
			entity.Flags |= LightEntityFlags.Update;

		// RenderLight projection settings
		ImGui.Spacing();
		ImGui.AlignTextToFramePadding();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text("For Spot and Area Lights, a vanilla texture can now be applied.\nThis acts as a gobo, blocking or coloring some of the light source as if projected through the image.");
		}
		ImGui.SameLine();
		using (ImRaii.Disabled(light->LightType is (LightType.Directional or LightType.PointLight))) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Image, "Choose a texture for this light to project"))
				this._goboPopup.Open();

			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
			ImGui.Text($"Current Texture: {(entity.Gobo == null ? "None" : entity.Gobo.Name)}");
		}

		ImGui.Spacing();
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.FileImport, "Import light settings"))
			this._ctx.Interface.OpenLightFile((path, file) => this._ctx.Scene.ApplyLightFile(entity, file));

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Save, "Export light settings"))
			this._ctx.Interface.OpenLightExport(entity);

		this.DrawGoboPopup(entity);
	}

	private unsafe void DrawShadowsTab(LightEntity entity) {
		var sceneLight = entity.GetObject();
		var light = sceneLight != null ? sceneLight->RenderLight : null;
		if (light == null) return;
		
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

	private unsafe void DrawGoboPopup(LightEntity entity) {
		if (!this._goboPopup.IsOpen) return;
		if (!this._goboPopup.Draw(this._goboSchema.Gobos, this._goboSchema.Gobos.Count, out var selected, CalcItemHeight())) return;

		entity.Gobo = selected;
		this._ctx.Scene.GetModule<LightModule>().UpdateSceneLightTexture(entity.GetObject(), entity.Gobo!.Path);
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
