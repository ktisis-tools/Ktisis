using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows.ToolbarModules;

public class ChangeStatePopup: KtisisPopup {

	private IEditorContext _ctx;
	private bool _state;
	public ChangeStatePopup(

		IEditorContext ctx,
		ImGuiWindowFlags flags = ImGuiWindowFlags.Modal
	) : base("##ToolbarConfirmPopup", flags) {
		this._ctx = ctx;
		this._state = this._ctx.Config.Editor.UseToolbar;
		this._ctx.Config.Editor.UseToolbar = !this._state;
	}

	protected override void OnDraw() {
			var width = ImGui.CalcTextSize($"This will close {(this._state ? "all open Ktisis windows and reopen the Toolbar." : "the toolbar and reopen the Workspace Window.")})").X;
			ImGui.SetCursorPosX((width - ImGui.CalcTextSize($"You are about to {(this._state ? "enable" : "disable")} the toolbar.").X)/2);
			ImGui.TextUnformatted($"You are about to {(this._state? "enable" : "disable")} the toolbar.");
			ImGui.TextUnformatted($"This will close {(this._state? "all open Ktisis windows and open the Toolbar." : "the toolbar and open the Workspace Window.")}");

			
			var buttons = ImGui.CalcTextSize("Continue").X + ImGui.CalcTextSize("Cancel").X + (ImGui.GetStyle().FramePadding.X * 4);
			ImGui.SetCursorPosX((width-buttons)/2);
			
			if (ImGui.Button("Continue")) {
				this._ctx.Plugin.Gui.ResetWorkspace();
				this._ctx.Config.Editor.UseToolbar = this._state;
				this._ctx.Interface.Prepare();
				this.Close();
			}

			ImGui.SameLine();
			if (ImGui.Button("Cancel")) {
				this.Close();
			}
	}
}
	

