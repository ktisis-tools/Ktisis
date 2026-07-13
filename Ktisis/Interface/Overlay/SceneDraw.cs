using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Popup;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Services.Game;
using Ktisis.Structs.Lights;
using Ktisis.Structs.Objects;

namespace Ktisis.Interface.Overlay;

[Transient]
public class SceneDraw {
	private readonly SelectableGui _select;
	private readonly RefOverlay _refs;
	private WorldObject? _hovered;
	private IGameObject? _hoveredActor;
	private bool _isHoveringWorld;
	private bool _isHoveringActor;
	private bool _isHoveringLight;
	
	private IEditorContext _ctx = null!;
	private readonly GuiManager _gui;
	private WorldObjectPopup? _popup;
	private ActorService _actors;
	private readonly GPoseService _gpose;

	private OverlayConfig Config => this._ctx.Config.Overlay;

	public SceneDraw(
		SelectableGui select,
		RefOverlay refs,
		GuiManager gui,
		ActorService actors,
		GPoseService gpose
	) {
		this._select = select;
		this._refs = refs;
		this._gui = gui;
		this._actors = actors;
		this._gpose = gpose;
	}

	public void SetContext(IEditorContext ctx) => this._ctx = ctx;
	
	public void DrawScene(bool gizmo = false, bool gizmoIsEnded = false) {
		var frame = this._select.BeginFrame();
		this.DrawEntities(frame, this._ctx.Scene.Children);
		this.DrawSelect(frame, gizmo, gizmoIsEnded);
		if (this._ctx.ShowWorldObjects) {
			this._isHoveringWorld = false;
			this._isHoveringActor = false;
			this._isHoveringLight = false;

			this.DrawWorldObjects();
			this.DrawWorldActors();
			this.DrawWorldLights();

			if (!this._isHoveringWorld)
				this.SetHovered(null);
			if (!this._isHoveringActor)
				this.SetHoveredActor(null);
		}
	}

	public void DrawRefOverlay() {
		foreach (var image in this._ctx.Scene.Children.OfType<ReferenceImage>())
			this._refs.DrawInstance(image);
	}

	private void DrawEntities(ISelectableFrame frame, IEnumerable<SceneEntity> entities, float opacity = 1.0f) {
		foreach (var entity in entities) {
			switch (entity) {
				case EntityPose pose:
					this.DrawSkeleton(frame, pose);
					continue;
				case IVisibility { Visible: true } and ITransform manip:
					var position = manip.GetTransform()?.Position;
					if (position != null)
						frame.AddItem(entity, position.Value, this._ctx, opacity);
					break;
			}

			if (entity is ActorEntity actor)
				this.DrawEntities(frame, entity.Children, this.GetOpacityMultiplier(actor));
			else
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

		float? opacity = null;
		if (pose.Parent is ActorEntity actor)
			opacity = this.GetOpacityMultiplier(actor);

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
				
				if (opacity is not null)
					frame.AddItem(node, transform.Position, this._ctx, opacity.Value);
				else
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

	private unsafe void DrawWorldObjects() {
		var drawList = ImGui.GetBackgroundDrawList();
		var camera = CameraService.GetSceneCamera();
		var clip = SelectableGui.WindowOverlaps();
		if (camera == null) return;

		foreach (var obj in this._ctx.Scene.World.Objects) {
			if (this._ctx.Scene.Children.OfType<ObjectEntity>().Any(ent => ent.Object.Equals(obj))) continue;
			if (!CameraService.WorldToScreen(camera, obj.InitialTransform.Position, out var worldPos2d)) continue;
			var distance = this.ObjectDistance(new Vector2(obj.InitialTransform.Position.X, obj.InitialTransform.Position.Z));
			if (distance > this.Config.WorldCameraRange) continue;

			var nodeScale = float.Lerp(1.0f, this.Config.WorldNodeScaleFactor, (distance / this.Config.WorldCameraRange));

			drawList.AddNgonFilled(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth - 1.0f) * nodeScale, this.Config.WorldNodeColor, 4);
			if (this.Config.WorldNodeOutlineWidth > 0.0f)
				drawList.AddNgon(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale, 0xFF000000, 4, this.Config.WorldNodeOutlineWidth);

			// if hovering a different dot, or hovering a ImGui window, or not hovering this, or the popup is open for this obj already, skip
			var radius = (6.0f + this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale;
			var radVec = new Vector2(radius, radius);
			if (this._isHoveringWorld
				|| SelectableGui.CheckPosClip(worldPos2d, clip)
				|| !ImGui.IsMouseHoveringRect(worldPos2d - radVec, worldPos2d + radVec)
				|| (this._popup is { IsOpen: true } && this._popup.WorldObj.Equals(obj))
			)
				continue;

			this._isHoveringWorld = true;
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
	}

	private unsafe void DrawWorldActors() {
		var drawList = ImGui.GetBackgroundDrawList();
		var camera = CameraService.GetSceneCamera();
		var clip = SelectableGui.WindowOverlaps();
		if (camera == null) return;

		foreach (var overworldActor in this._actors.GetOverworldActors()) {
			if (this._ctx.Scene.Children.OfType<ActorEntity>().Any(ent => ent.Actor.ObjectIndex == overworldActor.ObjectIndex)) continue;
			if (!CameraService.WorldToScreen(camera, overworldActor.Position, out var worldPos2d)) continue;
			var distance = this.ObjectDistance(new Vector2(overworldActor.Position.X, overworldActor.Position.Z));
			if (distance > this.Config.WorldCameraRange) continue;

			var nodeScale = float.Lerp(1.0f, this.Config.WorldNodeScaleFactor, (distance / this.Config.WorldCameraRange));

			drawList.AddNgonFilled(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth - 1.0f) * nodeScale, this.Config.ActorNodeColor, 5);
			if (this.Config.WorldNodeOutlineWidth > 0.0f)
				drawList.AddNgon(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale, 0xFF000000, 5, this.Config.WorldNodeOutlineWidth);

			var radius = (6.0f + this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale;
			var radVec = new Vector2(radius, radius);
			if (this._isHoveringWorld
				|| this._isHoveringActor
				|| SelectableGui.CheckPosClip(worldPos2d, clip)
				|| !ImGui.IsMouseHoveringRect(worldPos2d - radVec, worldPos2d + radVec)
				|| (this._popup is { IsOpen: true })
			)
				continue;

			this._isHoveringActor = true;
			this.SetHoveredActor(overworldActor);
			using (ImRaii.Tooltip()) {
				using var _col = ImRaii.PushColor(ImGuiCol.Text, this.Config.WorldNodeColor);
				var label = overworldActor.Name.TextValue.IsNullOrEmpty() ? $"{overworldActor.ObjectIndex}" : $"{overworldActor.Name} ({overworldActor.ObjectIndex})";
				ImGui.Text($"Add Actor {label}");
			}
			ImGui.SetNextFrameWantCaptureMouse(true);
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
				var module = this._ctx.Scene.GetModule<ActorModule>();
				module.AddFromOverworld(overworldActor);
			}
		}
	}

	private unsafe void DrawWorldLights() {
		var drawList = ImGui.GetBackgroundDrawList();
		var camera = CameraService.GetSceneCamera();
		var clip = SelectableGui.WindowOverlaps();
		if (camera == null) return;

		foreach (var light in this._ctx.Scene.World.Lights) {
			if (this._ctx.Scene.Children.OfType<LightEntity>().Any(ent => ent.WorldLight.Equals(light))) continue;
			if (!CameraService.WorldToScreen(camera, light.InitialTransform.Position, out var worldPos2d)) continue;
			var distance = this.ObjectDistance(new Vector2(light.InitialTransform.Position.X, light.InitialTransform.Position.Z));
			if (distance > this.Config.WorldCameraRange) continue;
			var sceneLight = (SceneLight*)light.Address;
			if (sceneLight is null || !sceneLight->DrawObject.IsVisible) continue;

			var nodeScale = float.Lerp(1.0f, this.Config.WorldNodeScaleFactor, (distance / this.Config.WorldCameraRange));

			drawList.AddNgonFilled(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth - 1.0f) * nodeScale, this.Config.LightNodeColor, 3);
			if (this.Config.WorldNodeOutlineWidth > 0.0f)
				drawList.AddNgon(worldPos2d, (this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale, 0xFF000000, 3, this.Config.WorldNodeOutlineWidth);

			// if hovering a different dot, or hovering a ImGui window, or not hovering this, or the popup is open for this obj already, skip
			var radius = (6.0f + this.Config.WorldNodeRadius + this.Config.WorldNodeOutlineWidth / 2) * nodeScale;
			var radVec = new Vector2(radius, radius);
			if (this._isHoveringWorld
				|| this._isHoveringActor
				|| this._isHoveringLight
				|| SelectableGui.CheckPosClip(worldPos2d, clip)
				|| !ImGui.IsMouseHoveringRect(worldPos2d - radVec, worldPos2d + radVec)
				|| (this._popup is { IsOpen: true })
			)
				continue;

			this._isHoveringLight = true;
			using (ImRaii.Tooltip()) {
				using var _col = ImRaii.PushColor(ImGuiCol.Text, this.Config.WorldNodeColor);
				ImGui.Text($"Add World Light");
			}

			ImGui.SetNextFrameWantCaptureMouse(true);
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
				var module = this._ctx.Scene.GetModule<LightModule>();
				module.AddFromOverworld(light);
			}
		}
	}

	private unsafe float ObjectDistance(Vector2 xzPosition) {
		var camPos = new Vector2();
		var currentCamera = this._ctx.Cameras.Current;
		if (currentCamera is WorkCamera freeCam) {
			camPos.X = freeCam.Position.X;
			camPos.Y = freeCam.Position.Z;
		} else if (currentCamera != null) {
			camPos.X = currentCamera.Camera->Position.X;
			camPos.Y = currentCamera.Camera->Position.Z;
		}

		return Vector2.Distance(camPos, xzPosition);
	}

	private void SetHovered(WorldObject? obj) {
		if (obj.Equals(this._hovered)) return;
		this._hovered?.SetOutline(OutlineChoice.None);

		this._hovered = obj;
		this._hovered?.SetOutline(this.Config.WorldOutlineColor);
	}

	private void SetHoveredActor(IGameObject? actor) {
		if (actor is not null && actor.Equals(this._hoveredActor)) return;
		this.SetActorHighlight(false);

		this._hoveredActor = actor;
		this.SetActorHighlight(true);
	}

	private static ObjectHighlightColor GetHighlightColor(OutlineChoice choice) => choice switch {
		OutlineChoice.None => ObjectHighlightColor.None,
		OutlineChoice.Red => ObjectHighlightColor.Red,
		OutlineChoice.Green => ObjectHighlightColor.Green,
		OutlineChoice.Blue => ObjectHighlightColor.Blue,
		OutlineChoice.Yellow => ObjectHighlightColor.Yellow,
		OutlineChoice.Orange => ObjectHighlightColor.Orange,
		OutlineChoice.Pink => ObjectHighlightColor.Magenta,
		_ => ObjectHighlightColor.None
	};

	private unsafe void SetActorHighlight(bool highlightOn) {
		if (this._hoveredActor is null) return;

		var csPtr = (CSGameObject*)this._hoveredActor.Address;
		if (csPtr == null || csPtr->DrawObject == null) return;

		if (highlightOn) {
			csPtr->Highlight(GetHighlightColor(this.Config.WorldOutlineColor));
		} else
			csPtr->Highlight(ObjectHighlightColor.None);
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
