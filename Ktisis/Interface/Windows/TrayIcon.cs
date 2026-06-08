using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class TrayIcon : KtisisWindow {

	private ITextureProvider _tex;
	private IEditorContext _ctx;
	private bool _holding;
	private Vector2? _offset;
	private bool _rightClick;

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
		if (!this._ctx.IsGPosing)
			this.Close();
		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetName().Name!;


		var file = "simple";
		if (ImGui.IsWindowHovered() && !this._holding)
			file = "colored";
		var icon = this._tex.GetFromManifestResource(assembly, $"{name}.Data.Images.icon_{file}.png");

		var io = ImGui.GetIO();
		ImGui.ImageButton(icon.GetWrapOrEmpty().Handle, Vector2.Create(64f));

		if (ImGui.IsWindowHovered()) {
			if (io.MouseReleased[0] && io.MouseDownDurationPrev[0] < 0.5f) {
				this._ctx.Interface.ToggleWorkspaceWindow();
				this.Close();
			} else if (io.MouseDown[0] && io.MouseDownDuration[0] > 0.5f) {
				this._offset ??= ImGui.GetMousePos() - ImGui.GetWindowPos();
				this._holding = true;
			} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
				this._rightClick = true;
			}
		}
		if (this._rightClick) {
			ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4f);
			using (ImRaii.ContextPopup("##TrayPopup")) {
					if (ImGui.Selectable(" Dismiss"))
						this.Close();
					else if (ImGui.Selectable(" Toggle Overlay"))
						this._ctx.Config.Overlay.Visible ^= true;
					else if (ImGui.Selectable(" Offset Camera to Target Model", size: ImGui.CalcTextSize(" Offset Camera to Target Model") + new Vector2(6, 2))) {
						unsafe {
							var camera = this._ctx.Cameras.Current;
							var target = this._ctx.Cameras.ResolveOrbitTarget(camera);
							var gameObject = (GameObject*)target.Address;
							var drawObject = gameObject->DrawObject;
							if (drawObject != null)
								camera.RelativeOffset = drawObject->Object.Position - gameObject->Position;
						}
					}
			}
	
			ImGui.PopStyleVar();
		}
		if (this._holding) {
			if (!io.MouseReleased[0]) {
				ImGui.SetWindowPos(ImGui.GetMousePos() - this._offset.Value);
			} else {
				this._offset = null;
				this._holding = false;
			}
		}
	}
}
