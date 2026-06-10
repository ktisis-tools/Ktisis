using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GLib.Popups.Context;

using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class TrayIcon : KtisisWindow {
	private readonly ITextureProvider _tex;
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly ISharedImmediateTexture SimpleIcon;
	private readonly ISharedImmediateTexture ColoredIcon;

	private bool _holding;
	private Vector2? _offset;
	public readonly ImRaii.StyleDisposable WindowStyle = new();
	public readonly ImRaii.ColorDisposable WindowColor = new();
	
	public TrayIcon(
		ITextureProvider tex,
		IEditorContext ctx,
		GuiManager gui,
		ImGuiWindowFlags flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking
	) : base("##TrayIcon", flags) {
		this.Size = new Vector2(64, 64);
		this._tex = tex;
		this._ctx = ctx;
		this._gui = gui;

		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetName().Name!;
		this.SimpleIcon = this._tex.GetFromManifestResource(assembly, $"{name}.Data.Images.icon_simple.png");
		this.ColoredIcon = this._tex.GetFromManifestResource(assembly, $"{name}.Data.Images.icon_colored.png");
	}

	public override void PreDraw() {
		base.PreDraw();
		this.WindowStyle.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		this.WindowStyle.Push(ImGuiStyleVar.WindowBorderSize, 0.0f);
		this.WindowStyle.Push(ImGuiStyleVar.FramePadding, Vector2.Zero);
		this.WindowColor.Push(ImGuiCol.Button, 0);
		this.WindowColor.Push(ImGuiCol.ButtonHovered, 0);
		this.WindowColor.Push(ImGuiCol.ButtonActive, 0);
		if (this._ctx is { IsGPosing: true, IsValid: true }) return;
		this.Close();
	}
	public override void PostDraw() {
		this.WindowColor.Dispose();
		this.WindowStyle.Dispose();
		base.PostDraw();
	}

	public override void OnClose() {
		// safety first!
		this.WindowColor.Dispose();
		this.WindowStyle.Dispose();
		base.OnClose();
	}

	public override void Draw() {
		var rightClick = false;
		var hovered = ImGui.IsWindowHovered();
		if (hovered && !this._holding)
			ImGui.ImageButton(this.ColoredIcon.GetWrapOrEmpty().Handle, Vector2.Create(64.0f));
		else
			ImGui.ImageButton(this.SimpleIcon.GetWrapOrEmpty().Handle, Vector2.Create(64.0f));

		var io = ImGui.GetIO();
		if (hovered) {
			if (io.MouseReleased[0] && io.MouseDownDurationPrev[0] <= 0.25f) {
				this._ctx.Interface.ToggleWorkspaceWindow();
				this.Close();
			} else if (io.MouseDown[0] && io.MouseDownDuration[0] > 0.25f) {
				this._offset ??= ImGui.GetMousePos() - ImGui.GetWindowPos();
				this._holding = true;
			} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
				rightClick = true;
			}
		}

		if (rightClick) {
			var menu = new ContextMenuBuilder()
				.Action("Dismiss", this.Close)
				.Action("Toggle Overlay", () => this._ctx.Config.Overlay.Visible ^= true)
				.Action("Offset Camera to Target Model", () => {
					unsafe {
						var camera = this._ctx.Cameras.Current;
						if (camera == null) return;

						var target = this._ctx.Cameras.ResolveOrbitTarget(camera);
						if (target == null) return;

						var gameObject = (GameObject*)target.Address;
						var drawObject = gameObject->DrawObject;
						if (drawObject == null) return;

						camera.RelativeOffset = drawObject->Object.Position - gameObject->Position;
					}
				})
				.Build($"TrayContextMenu_{this.GetHashCode():X}");
			this._gui.AddPopup(menu.Open());
		} else if (this._holding && this._offset != null) {
			if (!io.MouseReleased[0]) {
				ImGui.SetWindowPos(ImGui.GetMousePos() - this._offset.Value);
			} else {
				this._offset = null;
				this._holding = false;
			}
		}

		// safety first!
		this.WindowColor.Dispose();
		this.WindowStyle.Dispose();
	}
}
