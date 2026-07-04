using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGuizmo;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Popup;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Entities.World;
using Ktisis.Services.Game;
using Ktisis.Structs.Objects;

namespace Ktisis.Interface.Overlay;

[Transient]
public class SceneDraw {
	private readonly SelectableGui _select;
	private readonly RefOverlay _refs;
	private WorldObject? _hovered;
	
	private IEditorContext _ctx = null!;
	private readonly GuiManager _gui;
	private WorldObjectPopup? _popup;

	private OverlayConfig Config => this._ctx.Config.Overlay;

	public SceneDraw(
		SelectableGui select,
		RefOverlay refs,
		GuiManager gui
	) {
		this._select = select;
		this._refs = refs;
		this._gui = gui;
	}

	public void SetContext(IEditorContext ctx) => this._ctx = ctx;
	
	public void DrawScene(bool gizmo = false, bool gizmoIsEnded = false) {
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._ctx.Scene.Children);
		this.DrawSelect(frame, gizmo, gizmoIsEnded);
		if (this._ctx.ShowWorldObjects)
			this.DrawWorldObjects();
	}

	public void DrawRefOverlay() {
		foreach (var image in this._ctx.Scene.Children.OfType<ReferenceImage>())
			this._refs.DrawInstance(image);
	}

	private void DrawEntities(ISelectableFrame frame, IEnumerable<SceneEntity> entities) {
		foreach (var entity in entities) {
			switch (entity) {
				case EntityPose pose:
					this.DrawSkeleton(frame, pose);
					continue;
				case IVisibility { Visible: true } and ITransform manip:
					var position = manip.GetTransform()?.Position;
					if (position != null)
						frame.AddItem(entity, position.Value, this._ctx);
					break;
			}

			this.DrawEntities(frame, entity.Children);
		}
	}
	
	// Skeletons

	private unsafe void DrawSkeleton(ISelectableFrame frame, EntityPose pose) {
		if (!pose.ShouldDraw() && !this.Config.BulkVisOverride) return;

		var skeleton = pose.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null) return;

		var camera = CameraService.GetSceneCamera();
		if (camera == null) return;

		var drawList = ImGui.GetWindowDrawList();

		var partialCt = skeleton->PartialSkeletonCount;
		for (var index = 0; index < partialCt; index++) {
			var partial = skeleton->PartialSkeletons[index];
			var hkaPose = partial.GetHavokPose(0);
			if (hkaPose == null || hkaPose->Skeleton == null)
				continue;

			var hkaSkeleton = hkaPose->Skeleton;
			var boneCt = hkaSkeleton->Bones.Length;
			for (var i = 0; i < boneCt; i++) {
				var node = pose.GetBoneFromMap(index, i);
				if (node?.Visible != true && !this.Config.BulkVisOverride) continue;

				var transform = node?.CalcTransformOverlay();
				if (transform == null || node == null) continue;
				
				frame.AddItem(node, transform.Position, this._ctx);
				
				// Draw lines to children.

				if (!this.Config.DrawLines) continue;
				if (!this.Config.DrawLinesGizmo && ImGuizmo.IsUsing()) continue;

				for (var c = i; c < boneCt; c++) {
					if (hkaSkeleton->ParentIndices[c] != i) continue;

					var bone = pose.GetBoneFromMap(index, c);
					if (bone?.Visible != true && !this.Config.BulkVisOverride) continue;

					var lineTo = bone?.CalcTransformOverlay();
					if (lineTo == null) continue;

					var display = this._ctx.Config.GetEntityDisplay(node);
					this.DrawLine(camera, drawList, transform.Position, lineTo.Position, display.Color);
				}
			}
		}
	}
	
	private unsafe void DrawLine(Camera* camera, ImDrawListPtr drawList, Vector3 fromPos, Vector3 toPos, uint color) {
		if (!CameraService.WorldToScreen(camera, fromPos, out var fromPos2d)) return;
		if (!CameraService.WorldToScreen(camera, toPos, out var toPos2d)) return;

		var opacity = ImGuizmo.IsUsing() ? this.Config.LineOpacityUsing : this.Config.LineOpacity;
		drawList.AddLine(fromPos2d, toPos2d, color.SetAlpha(opacity), this.Config.LineThickness);
	}
	
	private void DrawSelect(ISelectableFrame frame, bool gizmo, bool gizmoIsEnded) {
		var result = this._select.Draw(frame, out var clicked, gizmo);
		if (!result || clicked == null) return;
		if (gizmo && gizmoIsEnded) return;
		var mode = GuiHelpers.GetSelectMode();
		this._ctx.Selection.Select(clicked, mode);
	}

	private unsafe void DrawWorldObjects() {
		var isHoveringWorld = false;
		var drawList = ImGui.GetBackgroundDrawList();
		var camera = CameraService.GetSceneCamera();
		var clip = SelectableGui.WindowOverlaps();
		if (camera == null) return;

		foreach (var obj in this._ctx.Scene.World.Objects) {
			if (this._ctx.Scene.Children.OfType<ObjectEntity>().Any(ent => ent.Object.Equals(obj))) continue;
			if (!CameraService.WorldToScreen(camera, obj.InitialTransform.Position, out var worldPos2d)) continue;
			var distance = this.ObjectDistance(obj);
			if (distance > this.Config.WorldCameraRange) continue;

			var nodeScale = float.Lerp(1.0f, this.Config.WorldNodeScaleFactor, (distance / this.Config.WorldCameraRange));

			drawList.AddNgonFilled(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth - 1.0f) * nodeScale, this.Config.WorldNodeColor, 4);
			if (this.Config.WorldNodeOutlineWidth > 0.0f)
				drawList.AddNgon(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale, 0xFF000000, 4, this.Config.WorldNodeOutlineWidth);

			// if hovering a different dot, or hovering a ImGui window, or not hovering this, or the popup is open for this obj already, skip
			var radius = (6.0f + this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale;
			var radVec = new Vector2(radius, radius);
			if (isHoveringWorld
				|| SelectableGui.CheckPosClip(worldPos2d, clip)
				|| !ImGui.IsMouseHoveringRect(worldPos2d - radVec, worldPos2d + radVec)
				|| (this._popup is { IsOpen: true } && this._popup.WorldObj.Equals(obj))
			)
				continue;

			isHoveringWorld = true;
			this.SetHovered(obj);
			using (ImRaii.Tooltip()) {
				using var _col = ImRaii.PushColor(ImGuiCol.Text, this.Config.WorldNodeColor);
				ImGui.Text($"Object Details...");
			}

			ImGui.SetNextFrameWantCaptureMouse(true);
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
				this._popup = this._gui.CreatePopup<WorldObjectPopup>(obj, distance, this._ctx);
				this._popup.Open();
			}
		}

		if (!isHoveringWorld)
			this.SetHovered(null);
	}

	private unsafe float ObjectDistance(WorldObject obj) {
		var objPos = new Vector2(obj.InitialTransform.Position.X, obj.InitialTransform.Position.Z);
		var camPos = new Vector2();
		var currentCamera = this._ctx.Cameras.Current;
		if (currentCamera is WorkCamera freeCam) {
			camPos.X = freeCam.Position.X;
			camPos.Y = freeCam.Position.Z;
		} else if (currentCamera != null) {
			camPos.X = currentCamera.Camera->Position.X;
			camPos.Y = currentCamera.Camera->Position.Z;
		}

		return Vector2.Distance(camPos, objPos);
	}

	private void SetHovered(WorldObject? obj) {
		if (obj.Equals(this._hovered)) return;
		this._hovered?.SetOutline(OutlineChoice.None);

		this._hovered = obj;
		this._hovered?.SetOutline(this.Config.WorldOutlineColor);
	}
}
