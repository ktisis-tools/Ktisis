using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows.ToolbarModules;

public class ChangeStatePopup: KtisisPopup {
	private IEditorContext _ctx;
	private bool _state;
	public ChangeStatePopup(

		IEditorContext ctx,
		ImGuiWindowFlags flags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove
	) : base("##ToolbarConfirmPopup", flags) {
		this._ctx = ctx;
		this._state = this._ctx.Config.Editor.UseToolbar;
		this._ctx.Config.Editor.UseToolbar = !this._state;
	}

	protected override void OnDraw() {
		ImGuiP.SetWindowPos(ImGuiP.GetCurrentWindow(), ImGui.GetWindowViewport().GetCenter() - (ImGui.GetWindowSize()/2));  //TODO: Move to GLib somehow
		var closeString = $"{Ktisis.Locale.Translate("toolbar.popup.close")} {(this._state ? Ktisis.Locale.Translate("toolbar.popup.close_workspace") : Ktisis.Locale.Translate("toolbar.popup.close_toolbar"))}";
		var stateString = $"{Ktisis.Locale.Translate("toolbar.popup.state")} {(this._state ? Ktisis.Locale.Translate("toolbar.popup.state_enable") : Ktisis.Locale.Translate("toolbar.popup.state_disable"))} {Ktisis.Locale.Translate("toolbar.popup.state_end")}";
		var yesString = Ktisis.Locale.Translate("toolbar.popup.yes");
		var noString = Ktisis.Locale.Translate("toolbar.popup.no");

		var width = ImGui.CalcTextSize(closeString).X;
		ImGui.SetCursorPosX((width - ImGui.CalcTextSize(stateString).X)/2);
		ImGui.TextUnformatted(stateString);
		ImGui.TextUnformatted(closeString);

		var buttons = ImGui.CalcTextSize(yesString).X + ImGui.CalcTextSize(noString).X + (ImGui.GetStyle().FramePadding.X * 4);
		ImGui.SetCursorPosX((width-buttons)/2);

		if (ImGui.Button(yesString)) {
			this._ctx.Plugin.Gui.ResetWorkspace();
			this._ctx.Config.Editor.UseToolbar = this._state;
			this._ctx.Interface.Prepare();
			this.Close();
		}

		ImGui.SameLine();
		if (ImGui.Button(noString)) {
			this.Close();
		}
	}
}
	

