using System;
using System.Diagnostics;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms.Types;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Types;
using Ktisis.Services.Game;

using Matrix4x4 = System.Numerics.Matrix4x4;
using Vector3 = System.Numerics.Vector3;

namespace Ktisis.Interface.Overlay;

public class OverlayWindow : KtisisWindow {
	private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
		| ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;

	private readonly IGameGui _gui;
	
	private readonly IEditorContext _ctx;
	private readonly IGizmo _gizmo;
	private readonly IGizmo _gizmoGaze;
	public Vector3? GazeTarget;
	public bool GazeManipulated = false;
	private readonly SceneDraw _sceneDraw;

	public OverlayWindow(
		IGameGui gui,
		IEditorContext ctx,
		IGizmo gizmo,
		IGizmo gizmoGaze,
		SceneDraw draw
	) : base("##KtisisOverlay", WindowFlags) {
		this._gui = gui;
		this._ctx = ctx;
		this._gizmo = gizmo;
		this._gizmoGaze = gizmoGaze;
		this._sceneDraw = draw;
		this._sceneDraw.SetContext(ctx);
		this.PositionCondition = ImGuiCond.Always;
	}
	
	private ITransformMemento? Transform;

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context for overlay window is stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		this.Size = ImGui.GetMainViewport().Size;
		this.Position = ImGui.GetMainViewport().Pos;
	}
	
	// Main draw function

	public override void Draw() {
		if (!this._ctx.Config.Overlay.Visible) return;
		
		// var t = new Stopwatch();
		// t.Start();
		var gizmoDrawn = false;

		if (this.GazeTarget != null)
			this.GazeManipulated = this.DrawGazeGizmo();
		else {
			this.GazeManipulated = false;
			gizmoDrawn = this.DrawGizmo();
		}
		this._sceneDraw.DrawScene(gizmo: gizmoDrawn, gizmoIsEnded: this._gizmo.IsEnded);

		// t.Stop();
		// this.DrawDebugOverlay(t);
	}

	private bool DrawGizmo() {
		if (!this._ctx.Config.Gizmo.Visible)
			return false;
		
		var target = this._ctx.Transform.Target;
		var transform = target?.GetTransform();
		if (target == null || transform == null)
			return false;
		
		var view = CameraService.GetViewMatrix();
		var proj = CameraService.GetProjectionMatrix();
		if (view == null || proj == null || this.Size == null)
			return false;

		var size = this.Size.Value;
		this._gizmo.SetMatrix(view.Value, proj.Value);
		this._gizmo.BeginFrame(this.Position!.Value, size);

		var cfg = this._ctx.Config.Gizmo;
		this._gizmo.Mode = cfg.Mode;
		this._gizmo.Operation = cfg.Operation;
		this._gizmo.AllowAxisFlip = cfg.AllowAxisFlip;

		var matrix = transform.ComposeMatrix();
		var isManipulate = this._gizmo.Manipulate(ref matrix, out _);
		var isRaySnap = this.HandleShiftRaycast(ref matrix);
		if (isManipulate || isRaySnap) {
			this.Transform ??= this._ctx.Transform.Begin(target);
			this.Transform.SetTransform(new Transform(matrix, transform));
		}

		this._gizmo.EndFrame();
		if (this._gizmo.IsEnded) {
			this.Transform?.Dispatch();
			this.Transform = null;
		}

		return true;
	}

	private bool DrawGazeGizmo() {
		// todo: kinda wack
		if (!this._ctx.Config.Overlay.Visible) return false;
		if (this.GazeTarget == null) return false;

		var view = CameraService.GetViewMatrix();
		var proj = CameraService.GetProjectionMatrix();
		if (view == null || proj == null || this.Size == null)
			return false;

		// create transform target off position from ActorPropertyList if provided
		var transform = new Transform((Vector3)this.GazeTarget);
		var matrix = transform.ComposeMatrix();

		var cfg = this._ctx.Config.Gizmo;
		this._gizmoGaze.Mode = Mode.World;
		this._gizmoGaze.Operation = Operation.TRANSLATE;
		this._gizmoGaze.AllowAxisFlip = cfg.AllowAxisFlip;
		this._gizmoGaze.ScaleFactor = 0.075f;

		var size = this.Size.Value;
		this._gizmoGaze.SetMatrix(view.Value, proj.Value);

		// set target to decomposed position for ActorPropertyList to consume
		this._gizmoGaze.BeginFrame(this.Position!.Value, size);
		var isManipulate = this._gizmoGaze.Manipulate(ref matrix, out _);
		transform.DecomposeMatrixPrecise(matrix, transform);
		this.GazeTarget = transform.Position;
		this._gizmoGaze.EndFrame();

		return isManipulate;
	}

	private bool HandleShiftRaycast(ref Matrix4x4 matrix) {
		if (!this._ctx.Config.Gizmo.AllowRaySnap)
			return false;
		
		if (!ImGui.IsKeyDown(ImGuiKey.ModShift) || !ImGuizmo.Gizmo.IsUsing || ImGuizmo.Gizmo.CurrentOperation != Operation.TRANSLATE)
			return false;

		if (!this._gui.ScreenToWorld(ImGui.GetMousePos(), out var hitPos))
			return false;

		matrix.Translation = hitPos;
		return true;
	}

	private void DrawDebugOverlay(Stopwatch? t) {
		ImGui.SetCursorPosY(ImGui.GetStyle().WindowPadding.Y);
		for (var i = 0; i < 5; i++)
			ImGui.Spacing();
		DrawDebug(t);
	}

	public void DrawDebug(Stopwatch? t) {
		ImGui.Text($"Context: {this._ctx.GetHashCode():X} ({this._ctx.IsValid})");
		ImGui.Text($"Scene: {this._ctx.Scene.GetHashCode():X} {this._ctx.Scene.UpdateTime:00.00}ms");
		if (t != null)
			ImGui.Text($"Overlay: {this.GetHashCode()} {t.Elapsed.TotalMilliseconds:00.00}ms");
		ImGui.Text($"Gizmo: {this._gizmo.GetHashCode():X} {this._gizmo.Id} ({this._gizmo.Operation}, {ImGuizmo.Gizmo.IsUsing})");
		ImGui.Text($"Gaze Gizmo?: {this._gizmoGaze.GetHashCode():X} {this._gizmoGaze.Id} ({this._gizmoGaze.Operation}, {ImGuizmo.Gizmo.IsUsing})");
		var target = this._ctx.Transform.Target;
		ImGui.Text($"Target: {target?.GetHashCode() ?? 0:X7} {target?.GetType().Name ?? "NULL"} ({target?.Targets?.Count() ?? 0}, {target?.Primary?.Name ?? "NULL"})");
		var history = this._ctx.Actions.History;
		ImGui.Text($"History: {history.Count} ({history.CanUndo}, {history.CanRedo})");
	}
}
