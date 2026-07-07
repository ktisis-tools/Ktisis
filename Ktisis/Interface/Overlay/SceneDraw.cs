using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Overlay;

[Transient]
public class SceneDraw {
	private readonly SelectableGui _select;
	private readonly RefOverlay _refs;
	
	private IEditorContext _ctx = null!;
	private readonly GPoseService _gpose;

	private OverlayConfig Config => this._ctx.Config.Overlay;

	public SceneDraw(
		SelectableGui select,
		RefOverlay refs,
		GPoseService gpose
	) {
		this._select = select;
		this._refs = refs;
		this._gpose = gpose;
	}

	public void SetContext(IEditorContext ctx) => this._ctx = ctx;
	
	public void DrawScene(bool gizmo = false, bool gizmoIsEnded = false) {
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._ctx.Scene.Children);
		this.DrawSelect(frame, gizmo, gizmoIsEnded);
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
					float? opacity = null;
					if (pose.Parent is ActorEntity actor)
						opacity = this.GetOpacityMultiplier(actor);

					this.DrawLine(camera, drawList, transform.Position, lineTo.Position, display.Color, opacity);
				}
			}
		}
	}
	
	private unsafe void DrawLine(Camera* camera, ImDrawListPtr drawList, Vector3 fromPos, Vector3 toPos, uint color, float? opacityMultiplier) {
		if (!CameraService.WorldToScreen(camera, fromPos, out var fromPos2d)) return;
		if (!CameraService.WorldToScreen(camera, toPos, out var toPos2d)) return;

		var opacity = ImGuizmo.IsUsing() ? this.Config.LineOpacityUsing : this.Config.LineOpacity;
		if (opacityMultiplier is not null)
			opacity *= opacityMultiplier.Value;
		drawList.AddLine(fromPos2d, toPos2d, color.SetAlpha(opacity), this.Config.LineThickness);
	}
	
	private void DrawSelect(ISelectableFrame frame, bool gizmo, bool gizmoIsEnded) {
		var result = this._select.Draw(frame, out var clicked, gizmo);
		if (!result || clicked == null) return;
		if (gizmo && gizmoIsEnded) return;
		var mode = GuiHelpers.GetSelectMode();
		this._ctx.Selection.Select(clicked, mode);
	}

	private float GetOpacityMultiplier(ActorEntity actor) {
		if (!this.Config.DimOverlayForInactiveActors) return 1.0f;

		if (this.Config.ActiveStateType is ActiveState.Target or ActiveState.Both) {
			if (this._gpose.GPoseTarget?.ObjectIndex == actor.Actor.ObjectIndex)
				return 1.0f;
		} else if (this.Config.ActiveStateType is ActiveState.Selection or ActiveState.Both) {
			if (actor.IsSelected || actor.Recurse().Any(x => x.IsSelected))
				return 1.0f;
		}

		return this.Config.InactiveOpacity;
	}
}
