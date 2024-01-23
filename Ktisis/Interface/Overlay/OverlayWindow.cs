using System.Diagnostics;
using System.Linq;

using ImGuiNET;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Common.Math;

using Ktisis.Editor.Context;
using Ktisis.Editor.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Services;

namespace Ktisis.Interface.Overlay;

public class OverlayWindow : KtisisWindow {
	private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
		| ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;
	
	private readonly IEditorContext _context;
	private readonly IGizmo _gizmo;
	
	private readonly SceneDraw _sceneDraw;
	
	private readonly IGameGui _gui;
	private readonly CameraService _camera;

	private ITransformHandler Handler => this._context.Transform;

	public OverlayWindow(
		IEditorContext context,
		IGizmo gizmo,
		SceneDraw draw,
		IGameGui gui,
		CameraService camera
	) : base("##KtisisOverlay", WindowFlags) {
		this._context = context;
		this._gizmo = gizmo;
		this._sceneDraw = draw;
		this._gui = gui;
		this._camera = camera;
		this._sceneDraw.SetContext(context);
	}
	
	private ITransformMemento? Transform;

	public override void PreOpenCheck() {
		if (this._context.IsValid) return;
		Ktisis.Log.Verbose("Context for overlay window is stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.Size = ImGui.GetIO().DisplaySize;
		this.Position = Vector2.Zero;
	}
	
	// Main draw function

	public override void Draw() {
		//var t = new Stopwatch();
		//t.Start();
		
		this._sceneDraw.DrawScene();
		if (this._context.Config.Gizmo.Visible)
			this.DrawGizmo();
		
		//t.Stop();
		//this.DrawDebug(t);
	}

	private void DrawGizmo() {
		var target = this.Handler.Target;
		var transform = target?.GetTransform();
		if (target == null || transform == null) return;
		
		var view = this._camera.GetViewMatrix();
		var proj = this._camera.GetProjectionMatrix();
		if (view == null || proj == null || this.Size == null)
			return;

		var size = this.Size.Value;
		this._gizmo.SetMatrix(view.Value, proj.Value);
		this._gizmo.BeginFrame(Vector2.Zero, size);

		var cfg = this._context.Config.Gizmo;
		this._gizmo.Mode = cfg.Mode;
		this._gizmo.Operation = cfg.Operation;
		this._gizmo.AllowAxisFlip = cfg.AllowAxisFlip;

		var matrix = transform.ComposeMatrix();
		if (this._gizmo.Manipulate(ref matrix, out _)) {
			this.Transform ??= this.Handler.Begin(target);
			target.SetMatrix(matrix);
		}

		this._gizmo.EndFrame();
		if (this._gizmo.IsEnded) {
			this.Transform?.Dispatch();
			this.Transform = null;
		}
	}

	private void DrawDebug(Stopwatch t) {
		ImGui.SetCursorPosY(ImGui.GetStyle().WindowPadding.Y);
		for (var i = 0; i < 5; i++)
			ImGui.Spacing();
		ImGui.Text($"Context: {this._context.GetHashCode():X} ({this._context.IsValid})");
		ImGui.Text($"Scene: {this._context.Scene.GetHashCode():X} {this._context.Scene.UpdateTime:00.00}ms");
		ImGui.Text($"Overlay: {this.GetHashCode()} {t.Elapsed.TotalMilliseconds:00.00}ms");
		ImGui.Text($"Gizmo: {this._gizmo.GetHashCode():X} {this._gizmo.Id} ({this._gizmo.Operation}, {ImGuizmo.Gizmo.IsUsing})");
		var target = this._context.Transform.Target;
		ImGui.Text($"Target: {target?.GetHashCode() ?? 0:X7} {target?.GetType().Name ?? "NULL"} ({target?.Targets?.Count() ?? 0}, {target?.Primary?.Name ?? "NULL"})");
		var history = this._context.Actions.History;
		ImGui.Text($"History: {history.Count} ({history.CanUndo}, {history.CanRedo})");
	}
}
