using System;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows.ToolbarModules;

public class Workspace : WorkspaceWindow  {

	private IEditorContext _editorContext;
	
	public Workspace(
		IEditorContext ctx
	) : base(ctx) {
		this._editorContext = ctx;
	}

	public override void PreDraw() {
	}
	
	public override void Draw() {
		var style = ImGui.GetStyle();
		
		// Context buttons
		
		this._cameras.Draw();
		this._workspace.DrawCompact();

		var botHeight = (UiBuilder.DefaultFontSizePx + (style.ItemSpacing.Y + style.ItemInnerSpacing.Y) * 2) * ImGuiHelpers.GlobalScale;
		var treeHeight = ((ImGui.GetTextLineHeightWithSpacing() + 5) * (Math.Max(10, this._editorContext.Scene.Children.Count())+ 5)) - botHeight; //TODO: would prefer sizing based upon expanded nodes but this will do for now
		this._sceneTree.Draw(treeHeight);

		ImGui.Spacing();
		
		this.DrawSceneTreeButtons();
	}
}
