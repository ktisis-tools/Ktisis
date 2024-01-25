using System.Numerics;

using Dalamud.Interface;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor;
using Ktisis.Interface.Editor.Types;

namespace Ktisis.Interface.Windows; 

public class WorkspaceWindow : KtisisWindow {
	private readonly IEditorContext _context;

	private readonly CameraSelector _cameras;
	private readonly WorkspaceState _state;
	private readonly SceneTree _sceneTree;

	private IEditorInterface Interface => this._context.Interface;
	
	public WorkspaceWindow(
		IEditorContext context
	) : base("Ktisis Workspace") {
		this._context = context;
		this._cameras = new CameraSelector(context);
		this._state = new WorkspaceState(context);
		this._sceneTree = new SceneTree(context);
	}
	
	// Constants
	
	private readonly static Vector2 MinimumSize = new(280, 300);
	
	// Pre-draw handlers

	public override void PreOpenCheck() {
		if (this._context.IsValid) return;
		Ktisis.Log.Verbose("Context for workspace window is stale, closing...");
		this.Close();
	}
	
	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = MinimumSize,
			MaximumSize = ImGui.GetIO().DisplaySize * 0.9f
		};
	}
	
	// Draw handler

	public override void Draw() {
		var style = ImGui.GetStyle();
		
		// Context buttons
		
		this.DrawContextButtons();
		ImGui.Spacing();
		this._cameras.Draw();
		this._state.Draw();

		var botHeight = UiBuilder.IconFont.FontSize + (style.ItemSpacing.Y + style.ItemInnerSpacing.Y) * 2;
		var treeHeight = ImGui.GetContentRegionAvail().Y - botHeight;
		this._sceneTree.Draw(treeHeight);

		ImGui.Spacing();
		
		this.DrawSceneTreeButtons();
	}
	
	// Context buttons

	private void DrawContextButtons() {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.ArrowsAlt, "Transform Editor"))
			this.Interface.OpenTransformWindow();

		ImGui.SameLine(0, spacing);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Sun, "Environment Editor"))
			this.Interface.OpenEnvironmentWindow();

		ImGui.SameLine(0, spacing);

		var gizmo = this._context.Config.Gizmo.Visible;
		var icon = gizmo ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
		if (Buttons.IconButtonTooltip(icon, "Toggle gizmo visibility"))
			this._context.Config.Gizmo.Visible = !gizmo;

		ImGui.SameLine(0, spacing);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Cog, "Settings"))
			this.Interface.OpenConfigWindow();
	}
	
	// Scene tree buttons

	private void DrawSceneTreeButtons() {
		if (Buttons.IconButton(FontAwesomeIcon.Plus))
			this.Interface.OpenSceneCreateMenu();
	}
}
 