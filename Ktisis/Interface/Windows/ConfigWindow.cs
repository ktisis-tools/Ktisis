using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class ConfigWindow : KtisisWindow {
	private readonly ConfigManager _cfg;
	private readonly ContextManager _context;

	private Configuration Config => this._cfg.File;

	public ConfigWindow(
		ConfigManager cfg,
		ContextManager context
	) : base("Ktisis Settings") {
		this._cfg = cfg;
		this._context = context;
	}

	public override void Draw() {
		using var tabs = ImRaii.TabBar("##ConfigTabs");
		if (!tabs.Success) return;
		DrawTab("Categories", this.DrawCategoriesTab);
		DrawTab("Gizmo", this.DrawGizmoTab);
		DrawTab("Workspace", this.DrawWorkspaceTab);
		DrawTab("Input", this.DrawInputTab);
	}
	
	// Tabs

	private static void DrawTab(string name, Action handler) {
		using var tab = ImRaii.TabItem(name);
		if (!tab.Success) return;
		ImGui.Spacing();
		handler.Invoke();
	}
	
	// Categories

	private void DrawCategoriesTab() {
		ImGui.Checkbox("Display NSFW bones", ref this.Config.Categories.ShowNsfwBones);
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text("Requires IVCS or any custom skeleton.");
		}
	}
	
	// Gizmo

	private void DrawGizmoTab() {
		ImGui.Checkbox("Flip axis to face camera", ref this.Config.Gizmo.AllowAxisFlip);
		
		ImGui.Spacing();
		ImGui.Text("Style:");
		ImGui.Spacing();
		this.DrawGizmoStyle();
	}

	private void DrawGizmoStyle() {
		var defaults = GizmoConfig.DefaultStyle;
		var style = this.Config.Gizmo.Style;
		
		using var frame = ImRaii.Child("##CfgStyleFrame", ImGui.GetContentRegionAvail(), true);

		if (ImGui.CollapsingHeader("General")) {
			DrawStyleColor("Direction X", ref style.ColorDirectionX, defaults.ColorDirectionX);
			DrawStyleColor("Direction Y", ref style.ColorDirectionY, defaults.ColorDirectionY);
			DrawStyleColor("Direction Z", ref style.ColorDirectionZ, defaults.ColorDirectionZ);
			DrawStyleColor("Selected Color", ref style.ColorSelection, defaults.ColorSelection);
			DrawStyleColor("Inactive Color", ref style.ColorInactive, defaults.ColorInactive);
		}

		if (ImGui.CollapsingHeader("Position")) {
			DrawStyleFloat("Line Thickness##PosThickness", ref style.TranslationLineThickness, defaults.TranslationLineThickness);
			DrawStyleFloat("Arrow Size##PosArrowSize", ref style.TranslationLineArrowSize, defaults.TranslationLineArrowSize);
			DrawStyleFloat("Hatched Axis Thickness", ref style.HatchedAxisLineThickness, defaults.HatchedAxisLineThickness);
			DrawStyleFloat("Center Circle Size##PosCircleSize", ref style.CenterCircleSize, defaults.CenterCircleSize);
			DrawStyleColor("Plane X##PosPlaneColorX", ref style.ColorPlaneX, defaults.ColorPlaneX);
			DrawStyleColor("Plane Y##PosPlaneColorY", ref style.ColorPlaneY, defaults.ColorPlaneY);
			DrawStyleColor("Plane Z##PosPlaneColorZ", ref style.ColorPlaneZ, defaults.ColorPlaneZ);
			DrawStyleColor("Line Color##PosLineColor", ref style.ColorTranslationLine, defaults.ColorTranslationLine);
			DrawStyleColor("Hatched Axis Color", ref style.ColorHatchedAxisLines, defaults.ColorHatchedAxisLines);
		}

		if (ImGui.CollapsingHeader("Rotation")) {
			DrawStyleFloat("Inner Line Thickness##RotateThickness", ref style.RotationLineThickness, defaults.RotationLineThickness);
			DrawStyleFloat("Outer Line Thickness##RotateThicknessOuter", ref style.RotationOuterLineThickness, defaults.RotationOuterLineThickness);
			DrawStyleColor("Activated Border Color##RotateUsingBorder", ref style.ColorRotationUsingBorder, defaults.ColorRotationUsingBorder);
			DrawStyleColor("Activated Fill Color##RotateUsingFill", ref style.ColorRotationUsingFill, defaults.ColorRotationUsingFill);
		}

		if (ImGui.CollapsingHeader("Scale")) {
			DrawStyleFloat("Line Thickness##ScaleThickness", ref style.ScaleLineThickness, defaults.ScaleLineThickness);
			DrawStyleFloat("Circle Size##ScaleSize", ref style.ScaleLineCircleSize, defaults.ScaleLineCircleSize);
			DrawStyleColor("Activated Line Color##ScaleColor", ref style.ColorScaleLine, defaults.ColorScaleLine);
		}

		if (ImGui.CollapsingHeader("Text")) {
			DrawStyleColor("Text Color", ref style.ColorText, defaults.ColorText);
			DrawStyleColor("Shadow Color", ref style.ColorTextShadow, defaults.ColorTextShadow);
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
		ImGui.DragFloat(label, ref value);
	}
	
	// Workspace
	
	private void DrawWorkspaceTab() {
		ImGui.Checkbox("Open on entering GPose", ref this.Config.Editor.OpenOnEnterGPose);
	}
	
	// Input

	private void DrawInputTab() {
		ImGui.Text("Keybinds will become configurable in a later testing release.");
		
		ImGui.Checkbox("Enable keybinds", ref this.Config.Keybinds.Enabled);

		using var _ = ImRaii.Disabled(!this.Config.Keybinds.Enabled);
		
		ImGui.Text(
			"Currently available keybinds:\n" +
			"	• History - Undo (Ctrl + Z)\n" +
			"	• History - Redo (Ctrl + Shift + Z)\n" +
			"	• Gizmo - Position Mode (Ctrl + T)\n" +
			"	• Gizmo - Rotate Mode (Ctrl + R)\n" +
			"	• Gizmo - Scale Mode (Ctrl + S)\n" +
			"	• Editor - Unselect (Escape)"
		);
	}
	
	// Close handler

	public override void OnClose() {
		base.OnClose();
		this._cfg.Save();
		this._context.Current?.Scene.Refresh();
	}
}
