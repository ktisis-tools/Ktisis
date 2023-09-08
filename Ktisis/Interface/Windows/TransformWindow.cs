using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using ImGuiNET;

using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Editing;
using Ktisis.Services;
using Ktisis.Common.Utility;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Overlay;
using Ktisis.Interface.Components;
using Ktisis.Interface.Widgets;
using Ktisis.ImGuizmo;

namespace Ktisis.Interface.Windows; 

public class TransformWindow : Window {
	// Constructor
    
	private readonly SceneManager _scene;
	private readonly CameraService _camera;

	private readonly Gizmo2D? Gizmo;
	private readonly TransformTable Table;
	
	private SceneEditor Editor => this._scene.Editor;

	public TransformWindow(
		SceneManager _scene,
		CameraService _camera
	) : base(
		"Transform Editor",
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize
	) {
		this._scene = _scene;
		this._camera = _camera;

		this.Gizmo = Gizmo2D.Create(GizmoID.TransformEditor);
		this.Table = new TransformTable("##Ktisis_TransformTable");
		this.Table.OnClickOperation += OnClickOperation;
		
		RespectCloseHotkey = false;
	}
	
	// State
	
	private bool ShowGizmo = true;
	
	// Events

	private void OnClickOperation(Operation op) {
		this.Editor.TransformOp = op;
	}
	
	// UI draw

	public override void PreOpenCheck() {
		if (!this._scene.IsActive) {
			this.Close();
			return;
		}

		var target = this.Editor.GetTransformTarget();

		if (target is null)
			this.IsOpen = false;
		else
			this.IsOpen = true;
	}

	public override void Draw() {
		if (!this._scene.IsActive) {
			this.Close();
			return;
		}

		var handler = this.Editor.GetHandler();
		var target = this.Editor.GetTransformTarget();
		if (handler is null || target is null) return;
		
		// Toggles

		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		var iconSize = UiBuilder.IconFont.FontSize * 2;
		var iconBtnSize =new Vector2(iconSize, iconSize);
		
		var mode = this.Editor.TransformMode;
		var modeIcon = mode == Mode.World ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home;
		var modeHint = mode == Mode.World ? "World Transform" : "Local Transform";
		if (Buttons.DrawIconButtonHint(modeIcon, modeHint, iconBtnSize))
			this.Editor.TransformMode = mode == Mode.World ? Mode.Local : Mode.World;
		
		ImGui.SameLine(0, spacing);

		var flags = this.Editor.Flags;

		var isMirror = flags.HasFlag(EditFlags.Mirror);
		var mrIcon = isMirror ? FontAwesomeIcon.ArrowDownUpAcrossLine : FontAwesomeIcon.GripLines;
		var mrHint = isMirror ? "Mirror Transform" : "Parallel Transform";
		if (Buttons.DrawIconButtonHint(mrIcon, mrHint, iconBtnSize))
			this.Editor.Flags ^= EditFlags.Mirror;

        ImGui.SameLine(0, spacing);
		var avail = ImGui.GetContentRegionAvail().X;
		if (avail > iconSize)
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - iconSize);

		var gizmoIcon = this.ShowGizmo ? FontAwesomeIcon.CaretUp : FontAwesomeIcon.CaretDown;
		var gizmoHint = this.ShowGizmo ? "Hide rotation gizmo" : "Show rotation gizmo";
		if (Buttons.DrawIconButtonHint(gizmoIcon, gizmoHint, iconBtnSize))
			this.ShowGizmo = !this.ShowGizmo;

		// Gizmo

		if (this.ShowGizmo) {
			var width = TransformTable.CalcWidth();
			DrawGizmo(target, width);
			ImGui.Spacing();
		} ImGui.Spacing();
		
		// Table
		// TODO: World/Local switch

		var local = target as ITransformLocal;

		var trans = local?.GetLocalTransform() ?? target.GetTransform();
		this.Table.Operation = this.Editor.TransformOp;
		if (trans is not null && this.Table.Draw(ref trans)) {
			if (local is not null)
				local.SetLocalTransform(trans);
			else
				target.SetTransform(trans);
		}
		
		// End
		
		//ImGui.PopItemWidth();
	}

	private unsafe void DrawGizmo(ITransform world, float width) {
		if (this.Gizmo is null || world.GetMatrix() is not Matrix4x4 matrix)
			return;
        
		var camera = this._camera.GetGameCamera();
		if (camera == null) return;

		var sceneCam = camera->CameraBase.SceneCamera;

		var pos = ImGui.GetCursorScreenPos();
		var size = new Vector2(width, width);

		this.Gizmo.Begin(size);
		this.Gizmo.Mode = this.Editor.TransformMode;

		var fov = camera->FoV;
		ImGui.GetWindowDrawList().AddCircleFilled(pos + size / 2, (width * Gizmo2D.ScaleFactor) / 2.05f, 0xCF202020);

		this.Gizmo.SetLookAt(sceneCam.Object.Position, matrix.Translation, fov);
		if (this.Gizmo.Manipulate(ref matrix, out var delta))
			this.Editor.Manipulate(world, matrix, delta);
		
		this.Gizmo.End();
	}
}
