using System.Linq;

using Dalamud.Interface;

using ImGuiNET;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.Actor;
using Ktisis.Scene.Entities.Game;

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
		if (DrawButton(FontAwesomeIcon.ArrowsAlt, "Transform"))
			this._ui.OpenTransformWindow(context);

		if (DrawButton(FontAwesomeIcon.Camera, "Camera")) { }

		if (DrawButton(FontAwesomeIcon.Sun, "Env")) { }

		if (DrawButton(FontAwesomeIcon.Lightbulb, "Lights"))
			this._gui.GetOrCreate<LightEditor>().Open();

		if (DrawButton(FontAwesomeIcon.WaveSquare, "Actor")) {
			var window = this._gui.GetOrCreate<ActorEditWindow>(context);
			window.Target = (ActorEntity)context.Selection.GetSelected().First(sel => sel is ActorEntity);
			window.Open();
		}

		if (DrawButton(FontAwesomeIcon.EllipsisH, "Options", true)) { }
	}

	private static bool DrawButton(FontAwesomeIcon icon, string tooltip, bool final = false) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		var activate = Buttons.IconButtonTooltip(icon, tooltip);
		if (!final) ImGui.SameLine(0, spacing);
		return activate;
	}
}
