using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class TrayIcon : KtisisWindow {
	
	private ITextureProvider _tex;
	private IEditorContext _ctx;
	private bool _holding = false;
	
	public TrayIcon(
		ITextureProvider tex,
		IEditorContext ctx,
		string name = "##TrayIcon"
		) : base(name) {
		this.Size = new Vector2(68, 68);
		this._tex = tex;
		this._ctx = ctx;
	}
	public override void PreDraw() {
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
		ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
		ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x00000000);
		ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x00000000);
		base.PreDraw();
	}
	public override void PostDraw() {
		ImGui.PopStyleVar(2);
		ImGui.PopStyleColor(3);
		base.PostDraw();
	}
	public override void Draw() {
		this.Flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoResize;
		if(!this._ctx.IsGPosing)
			this.Close();
		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetName().Name!;

		
		var file = "simple";
		if (ImGui.IsAnyItemHovered() && !this._holding)
			file = "colored";
		var icon = this._tex.GetFromManifestResource(assembly, $"{name}.Data.Images.icon_{file}.png");

		ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, Vector2.Create(64f));
		var io = ImGui.GetIO();

		if(ImGui.IsAnyItemHovered()){
			if (io.MouseReleased[0] && io.MouseDownDurationPrev[0] < 0.5f) {
				this._ctx.Interface.ToggleWorkspaceWindow();
				this.Close();
			} else if(io.MouseDown[0] && io.MouseDownDuration[0] > 0.5f) {
				this._holding = true;
			}
		}
		if (this._holding) {
			if (!io.MouseReleased[0]) {
				ImGui.SetWindowPos(io.MousePos.Sub(32));
			} else {
				this._holding = false;
			}
		}
	}
}
