using System.Numerics;

using Dalamud.Interface;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Editor.Types;

namespace Ktisis.Interface.Windows; 

public class WorkspaceWindow : KtisisWindow {
	private readonly IEditorContext _ctx;

	private readonly CameraSelector _cameras;
	private readonly WorkspaceState _workspace;
	private readonly SceneTree _sceneTree;

	private IEditorInterface Interface => this._ctx.Interface;
	
	public WorkspaceWindow(
		IEditorContext ctx
	) : base("Ktisis Workspace") {
		this._ctx = ctx;
		this._cameras = new CameraSelector(ctx);
		this._workspace = new WorkspaceState(ctx);
		this._sceneTree = new SceneTree(ctx);
	}
	
	// Constants
	
	private readonly static Vector2 MinimumSize = new(280, 300);
	
	// Pre-draw handlers

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
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
		this._workspace.Draw();

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
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Cog, "Settings"))
			this.Interface.OpenConfigWindow();
	}
	
	// Scene tree buttons

	private void DrawSceneTreeButtons() {
		if (Buttons.IconButton(FontAwesomeIcon.Plus))
			this.Interface.OpenSceneCreateMenu();
	}
}
 