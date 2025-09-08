using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Overlay;

[Transient]
public class SceneDraw {
	private readonly SelectableGui _select;
	private readonly RefOverlay _refs;
	
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
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._ctx.Scene.Children);
		this.DrawSelect(frame, gizmo, gizmoIsEnded);
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
						frame.AddItem(entity, position.Value);
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

				var transform = node.CalcTransformWorld();
				if (transform == null) continue;
				
				frame.AddItem(node, transform.Position);
				
				// Draw lines to children.

				if (!this.Config.DrawLines) continue;
				if (!this.Config.DrawLinesGizmo && ImGuizmo.Gizmo.IsUsing) continue;

				for (var c = i; c < boneCt; c++) {
					if (hkaSkeleton->ParentIndices[c] != i) continue;

					var bone = pose.GetBoneFromMap(index, c);
					if (bone?.Visible != true) continue;

					var lineTo = bone.CalcTransformWorld();
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
}
