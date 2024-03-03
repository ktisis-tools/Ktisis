using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Localization;

namespace Ktisis.Interface.Components.Config;

[Transient]
public class GizmoStyleEditor {
	private readonly ConfigManager _cfg;
	private readonly LocaleManager _locale;

	private Configuration Config => this._cfg.File;
	
	public GizmoStyleEditor(
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._cfg = cfg;
		this._locale = locale;
	}
	
	public void Draw() {
		var defaults = GizmoConfig.DefaultStyle;
		var style = this.Config.Gizmo.Style;
		
		using var frame = ImRaii.Child("##CfgStyleFrame", ImGui.GetContentRegionAvail(), true);

		if (ImGui.CollapsingHeader(this._locale.Translate("config.gizmo.editor.general.title"))) {
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.general.dir_x"), ref style.ColorDirectionX, defaults.ColorDirectionX);
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.general.dir_y"), ref style.ColorDirectionY, defaults.ColorDirectionY);
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.general.dir_z"), ref style.ColorDirectionZ, defaults.ColorDirectionZ);
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.general.active"), ref style.ColorSelection, defaults.ColorSelection);
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.general.inactive"), ref style.ColorInactive, defaults.ColorInactive);
		}

		if (ImGui.CollapsingHeader(this._locale.Translate("config.gizmo.editor.position.title"))) {
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.position.line_thick")}##PosThickness",
				ref style.TranslationLineThickness,
				defaults.TranslationLineThickness
			);
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.position.arrow_size")}##PosArrowSize",
				ref style.TranslationLineArrowSize,
				defaults.TranslationLineArrowSize
			);
			DrawStyleFloat(
				this._locale.Translate("config.gizmo.editor.position.axis_thick"),
				ref style.HatchedAxisLineThickness,
				defaults.HatchedAxisLineThickness
			);
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.position.circle_size")}##PosCircleSize",
				ref style.CenterCircleSize,
				defaults.CenterCircleSize
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.position.plane_x")}##PosPlaneColorX",
				ref style.ColorPlaneX,
				defaults.ColorPlaneX
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.position.plane_y")}##PosPlaneColorY",
				ref style.ColorPlaneY,
				defaults.ColorPlaneY
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.position.plane_z")}##PosPlaneColorZ",
				ref style.ColorPlaneZ,
				defaults.ColorPlaneZ
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.position.line_color")}##PosLineColor",
				ref style.ColorTranslationLine,
				defaults.ColorTranslationLine
			);
			DrawStyleColor(
				this._locale.Translate("config.gizmo.editor.position.axis_color"),
				ref style.ColorHatchedAxisLines,
				defaults.ColorHatchedAxisLines
			);
		}

		if (ImGui.CollapsingHeader(this._locale.Translate("config.gizmo.editor.rotation.title"))) {
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.rotation.inner_thick")}##RotateThickness",
				ref style.RotationLineThickness,
				defaults.RotationLineThickness
			);
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.rotation.outer_thick")}##RotateThicknessOuter",
				ref style.RotationOuterLineThickness,
				defaults.RotationOuterLineThickness
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.rotation.border_color")}##RotateUsingBorder",
				ref style.ColorRotationUsingBorder,
				defaults.ColorRotationUsingBorder
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.rotation.fill_color")}##RotateUsingFill",
				ref style.ColorRotationUsingFill,
				defaults.ColorRotationUsingFill
			);
		}

		if (ImGui.CollapsingHeader(this._locale.Translate("config.gizmo.editor.scale.title"))) {
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.scale.line_thick")}##ScaleThickness",
				ref style.ScaleLineThickness,
				defaults.ScaleLineThickness
			);
			DrawStyleFloat(
				$"{this._locale.Translate("config.gizmo.editor.scale.circle_size")}##ScaleSize",
				ref style.ScaleLineCircleSize,
				defaults.ScaleLineCircleSize
			);
			DrawStyleColor(
				$"{this._locale.Translate("config.gizmo.editor.scale.line_color")}##ScaleColor",
				ref style.ColorScaleLine,
				defaults.ColorScaleLine
			);
		}

		if (ImGui.CollapsingHeader(this._locale.Translate("config.gizmo.editor.text.title"))) {
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.text.color"), ref style.ColorText, defaults.ColorText);
			DrawStyleColor(this._locale.Translate("config.gizmo.editor.text.shadow_color"), ref style.ColorTextShadow, defaults.ColorTextShadow);
		}

		this.Config.Gizmo.Style = style;
	}

	private static void DrawStyleColor(string label, ref Vector4 value, Vector4 defaultValue) {
		var cX = ImGui.GetCursorPosX();
		
		using var _ = ImRaii.PushId($"##StyleFloat_{label}");
		using (var unused = ImRaii.Disabled(value.Equals(defaultValue))) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Undo, "Reset to default"))
				value = defaultValue;
		}
		
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cX));
		ImGui.ColorEdit4(label, ref value);
	}

	private static void DrawStyleFloat(string label, ref float value, float defaultValue) {
		var cX = ImGui.GetCursorPosX();

		using var _ = ImRaii.PushId($"##StyleFloat_{label}");
		using (var unused = ImRaii.Disabled(value.Equals(defaultValue))) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Undo, "Reset to default"))
				value = defaultValue;
		}
		
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - cX));
		ImGui.DragFloat(label, ref value, 0.01f);
	}
}
