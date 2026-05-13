using Dalamud.Bindings.ImGui;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Ipc;
using Ktisis.Scene.Types;

using GLib.Widgets;

namespace Ktisis.Interface.Components.Chara;

[Transient]
public class PluginDataEditorTab {

	
	private IpcManager _ipcManager;
	
	public PluginDataEditorTab(
		IpcManager ipc
	) {
		this._ipcManager = ipc;
	}
	public void Draw() {
		Separators.SeparatorText("Customize+", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
		if (this._ipcManager.IsCustomizeActive) {
			this.DrawCustomizePlus();
		} else {
			ImGui.Text("Customize+ wasn't found");
		}
		Separators.SeparatorText("Penumbra", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
		if (this._ipcManager.IsCustomizeActive) {
			this.DrawCustomizePlus();
		} else {
			ImGui.Text("Penumbra wasn't found");
		}
		Separators.SeparatorText("Glamourer", textColor: ImGui.GetColorU32(ImGuiCol.TextDisabled));
		if (this._ipcManager.IsCustomizeActive) {
			this.DrawCustomizePlus();
		} else {
			ImGui.Text("Glamourer wasn't found");
		}
	}

	public void DrawCustomizePlus() {
		
	}

	public void DrawPenumbra() {
		
	}

	public void DrawGlamourer() {
		
	}
}
