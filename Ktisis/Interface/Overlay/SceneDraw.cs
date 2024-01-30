using System.Collections.Generic;
using System.Numerics;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Overlay;

[Transient]
public class SceneDraw {
	private readonly CameraService _camera;
	private readonly SelectableGui _select;
	
	private IEditorContext _ctx = null!;

	private OverlayConfig Config => this._ctx.Config.Overlay;

	public SceneDraw(
		CameraService camera,
		SelectableGui select
	) {
		this._camera = camera;
		this._select = select;
	}

	public void SetContext(IEditorContext ctx) => this._ctx = ctx;
	
	public void DrawScene() {
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._ctx.Scene.Children);
		this.DrawSelect(frame);
	}
	
	private void DrawEntities(ISelectableFrame frame, IEnumerable<SceneEntity> entities) {
		foreach (var entity in entities) {
			if (entity is EntityPose pose) {
				this.DrawSkeleton(frame, pose);
				continue;
			}

			if (entity is IVisibility { Visible: true } and ITransform manip) {
				var position = manip.GetTransform()?.Position;
				if (position != null)
					frame.AddItem(entity, position.Value);
			}

			this.DrawEntities(frame, entity.Children);
		}
	}

	private unsafe void DrawSkeleton(ISelectableFrame frame, EntityPose pose) {
		var skeleton = pose.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null) return;

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
					if (lineTo != null)
						this.DrawLine(drawList, transform.Position, lineTo.Position);
				}
			}
		}
	}
	
	private void DrawLine(ImDrawListPtr drawList, Vector3 fromPos, Vector3 toPos) {
		if (!this._camera.WorldToScreen(fromPos, out var fromPos2d)) return;
		if (!this._camera.WorldToScreen(toPos, out var toPos2d)) return;

		var opacity = ImGuizmo.Gizmo.IsUsing ? this.Config.LineOpacityUsing : this.Config.LineOpacity;
		drawList.AddLine(fromPos2d, toPos2d, 0xFFFFFFFF.SetAlpha(opacity), this.Config.LineThickness);
	}
	
	private void DrawSelect(ISelectableFrame frame) {
		var result = this._select.Draw(frame, out var clicked);
		if (!result || clicked == null) return;
		var mode = GuiHelpers.GetSelectMode();
		this._ctx.Selection.Select(clicked, mode);
	}
}
