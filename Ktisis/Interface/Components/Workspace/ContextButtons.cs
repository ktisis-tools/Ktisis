using Dalamud.Interface;

using ImGuiNET;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Editor;
using Ktisis.Editor.Context;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class ContextButtons {
	private readonly GuiManager _gui;
	private readonly EditorUi _ui;
	
	public ContextButtons(
		GuiManager gui,
		EditorUi ui
	) {
		this._gui = gui;
		this._ui = ui;
	}
	
	public void Draw(IEditorContext context) {
		// TODO: Cleanup dev code.
		
		if (DrawButton(FontAwesomeIcon.ArrowsAlt, "Transform"))
			this._ui.OpenTransformWindow(context);

		if (DrawButton(FontAwesomeIcon.Sun, "Env"))
			this._ui.OpenEnvironmentWindow(context);

		var gizmo = context.Config.Gizmo.Visible;
		var icon = gizmo ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
		if (DrawButton(icon, "Gizmo"))
			context.Config.Gizmo.Visible = !gizmo;

		if (DrawButton(FontAwesomeIcon.EllipsisH, "Options", true)) { }
	}

	private static bool DrawButton(FontAwesomeIcon icon, string tooltip, bool final = false) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		var activate = Buttons.IconButtonTooltip(icon, tooltip);
		if (!final) ImGui.SameLine(0, spacing);
		return activate;
	}
}
