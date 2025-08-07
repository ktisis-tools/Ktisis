using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

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
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.ArrowsAlt, this._ctx.Locale.Translate("transform_edit.title")))
			this.Interface.OpenTransformWindow();

		ImGui.SameLine(0, spacing);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Sun, this._ctx.Locale.Translate("env_edit.title")))
			this.Interface.OpenEnvironmentWindow();

		ImGui.SameLine(0, spacing);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Cog, this._ctx.Locale.Translate("config.title")))
			this.Interface.OpenConfigWindow();

		ImGui.SameLine(0, spacing);
		
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Portrait, "Pose View"))
			this.Interface.OpenPosingWindow();

		ImGui.SameLine(0, spacing);
		ImGui.SetCursorPosX(ImGui.GetContentRegionMax().X - Buttons.CalcSize() * 2 - spacing);
		
		using (var _ = ImRaii.Disabled(!this._ctx.Actions.History.CanUndo))
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.StepBackward, this._ctx.Locale.Translate("actions.History_Undo")))
				this._ctx.Actions.History.Undo();
		
		ImGui.SameLine(0, spacing);
		
		using (var _ = ImRaii.Disabled(!this._ctx.Actions.History.CanRedo))
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.StepForward, this._ctx.Locale.Translate("actions.History_Redo")))
				this._ctx.Actions.History.Redo();
	}
	
	// Scene tree buttons

	private void DrawSceneTreeButtons() {
		if (Buttons.IconButton(FontAwesomeIcon.Plus))
			this.Interface.OpenSceneCreateMenu();
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Sync, this._ctx.Locale.Translate("workspace.refresh_actors")))
			this.Interface.RefreshGposeActors();
	}
}
 