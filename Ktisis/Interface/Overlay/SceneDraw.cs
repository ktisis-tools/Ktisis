using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
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
	private bool _isHoveringWorld = false;
	
	private IEditorContext _ctx = null!;

	private OverlayConfig Config => this._ctx.Config.Overlay;

	public SceneDraw(
		SelectableGui select,
		RefOverlay refs
	) {
		this._select = select;
		this._refs = refs;
	}

	public void SetContext(IEditorContext ctx) => this._ctx = ctx;
	
	public void DrawScene(bool gizmo = false, bool gizmoIsEnded = false) {
		this._isHoveringWorld = false;
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._ctx.Scene.Children);
		this.DrawSelect(frame, gizmo, gizmoIsEnded);
		if (this._ctx.ShowWorldObjects)
			this.DrawWorldObjects();
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
				case ReferenceImage image:
					this._refs.DrawInstance(image);
					continue;
			}

			this.DrawEntities(frame, entity.Children);
		}
	}
	
	// Skeletons

	private unsafe void DrawSkeleton(ISelectableFrame frame, EntityPose pose) {
		if (!pose.ShouldDraw()) return;

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
				if (node?.Visible != true) continue;

				var transform = node.CalcTransformOverlay();
				if (transform == null) continue;
				
				frame.AddItem(node, transform.Position, this._ctx);
				
				// Draw lines to children.

				if (!this.Config.DrawLines) continue;
				if (!this.Config.DrawLinesGizmo && ImGuizmo.Gizmo.IsUsing) continue;

				for (var c = i; c < boneCt; c++) {
					if (hkaSkeleton->ParentIndices[c] != i) continue;

					var bone = pose.GetBoneFromMap(index, c);
					if (bone?.Visible != true) continue;

					var lineTo = bone.CalcTransformOverlay();
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

		var opacity = ImGuizmo.Gizmo.IsUsing ? this.Config.LineOpacityUsing : this.Config.LineOpacity;
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
		var drawList = ImGui.GetBackgroundDrawList();
		var camera = CameraService.GetSceneCamera();
		if (camera == null) return;

		foreach (var obj in this._ctx.Scene.World.Objects) {
			if (this._ctx.Scene.Children.OfType<ObjectEntity>().Any(ent => ent.Object.Equals(obj))) continue;
			if (!CameraService.WorldToScreen(camera, obj.InitialTransform.Position, out var worldPos2d)) continue;
			if (!this.IsObjectInRange(obj)) continue;

			drawList.AddCircleFilled(worldPos2d, this.Config.WorldDotRadius + this.Config.WorldDotOutlineWidth - 1.0f, this.Config.WorldDotColor);
			if (this.Config.WorldDotOutlineWidth > 0.0f)
				drawList.AddCircle(worldPos2d, this.Config.WorldDotRadius + this.Config.WorldDotOutlineWidth / 2, 0xFF000000, 16, this.Config.WorldDotOutlineWidth);

			// if hovering a different dot, or hovering a ImGui window, or not hovering this, skip
			var radius = 6.0f + this.Config.WorldDotRadius + this.Config.WorldDotOutlineWidth / 2;
			var radVec = new Vector2(radius, radius);
			if (this._isHoveringWorld || ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) || !ImGui.IsMouseHoveringRect(worldPos2d - radVec, worldPos2d + radVec))
				continue;

			this._isHoveringWorld = true;
			using (ImRaii.Tooltip()) {
				using var _col = ImRaii.PushColor(ImGuiCol.Text, this.Config.WorldDotColor);
				ImGui.Text($"Click to add Object");
			}
			ImGui.SetNextFrameWantCaptureMouse(true);
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
				this._ctx.Scene.Factory
					.BuildObject()
					.SetName($"Object {this._ctx.Scene.Children.OfType<ObjectEntity>().Count() + 1}")
					.SetAddress(obj.Address)
					.Add();
		}
	}

	private unsafe bool IsObjectInRange(WorldObject obj) {
		var objPos = new Vector2(obj.InitialTransform.Position.X, obj.InitialTransform.Position.Z);
		var camPos = new Vector2();
		var currentCamera = this._ctx.Cameras.Current;
		if (currentCamera is WorkCamera freeCam) {
			camPos.X = freeCam.Position.X;
			camPos.Y = freeCam.Position.Z;
		} else if (currentCamera != null) {
			camPos.X = currentCamera.Camera->Position.X;
			camPos.Y = currentCamera.Camera->Position.Z;
		} else return false;

		return Vector2.Distance(camPos, objPos) <= this.Config.WorldCameraRange;
	}
}
