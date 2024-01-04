using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Context;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Strategy.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Services;

namespace Ktisis.Interface.Overlay;

[Transient]
public class SceneDraw {
	private readonly CameraService _camera;
	private readonly SelectableGui _select;
	
	private IEditorContext _context = null!;

	public SceneDraw(
		CameraService camera,
		SelectableGui select
	) {
		this._camera = camera;
		this._select = select;
	}

	public void SetContext(IEditorContext context)
		=> this._context = context;
	
	public void DrawScene() {
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._context.Scene.Children);
		this.DrawSelect(frame);
	}
	
	private void DrawEntities(ISelectableFrame frame, IEnumerable<SceneEntity> entities) {
		foreach (var entity in entities) {
			if (entity is EntityPose pose) {
				this.DrawSkeleton(frame, pose);
				continue;
			}
			
			if (entity.Edit() is IVisibility { Visible: true } and ITransform manip) {
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

		var drawList = ImGui.GetBackgroundDrawList();

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
				if (node?.Edit().Visible != true) continue;

				var transform = HavokPoseUtil.GetWorldTransform(skeleton, hkaPose, i);
				if (transform == null) continue;
				
				frame.AddItem(node, transform.Position);
				
				// Draw lines to children.

				for (var c = i; c < boneCt; c++) {
					if (hkaSkeleton->ParentIndices[c] != i) continue;

					if (pose.GetBoneFromMap(index, c)?.Edit().Visible != true)
						continue;

					var lineTo = HavokPoseUtil.GetWorldTransform(skeleton, hkaPose, c);
					if (lineTo != null)
						this.DrawLine(drawList, transform.Position, lineTo.Position);
				}
			}
		}
	}
	
	private unsafe void DrawLine(ImDrawListPtr drawList, Vector3 fromPos, Vector3 toPos) {
		var camera = this._camera.GetSceneCamera();
		if (camera == null) return;

		if (!camera->WorldToScreen(fromPos, out var fromPos2d)) return;
		if (!camera->WorldToScreen(toPos, out var toPos2d)) return;

		drawList.AddLine(fromPos2d, toPos2d, 0xFFFFFFFF);
	}
	
	private void DrawSelect(ISelectableFrame frame) {
		var result = this._select.Draw(frame, out var clicked);
		if (!result || clicked == null) return;
		var mode = GuiHelpers.GetSelectMode();
		this._context.Selection.Select(clicked, mode);
	}
}
