using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;

using GLib.Widgets;

using Dalamud.Bindings.ImGui;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Entity;
using Ktisis.Scene.Entities;
using Ktisis.Services.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Structs.Lights;
using Ktisis.Editor.Context.Types;
using Ktisis.Common.Utility;

namespace Ktisis.Interface.Overlay;

public interface ISelectableFrame {
	public IEnumerable<IItemSelect> GetItems();
	
	public void AddItem(SceneEntity entity, Vector3 worldPos, IEditorContext ctx);
}

public interface IItemSelect {
	public string Name { get; }
    
	public SceneEntity Entity { get; }
	public Vector2 ScreenPos { get; }
	
	public float Distance { get; }
	
	public bool IsHovered { get; set; }
}

[Transient]
public class SelectableGui {
	private readonly ConfigManager _cfg;

	private Configuration Config => this._cfg.File;
	
	public SelectableGui(
		ConfigManager cfg
	) {
		this._cfg = cfg;
	}
    
	public ISelectableFrame BeginFrame() {
		return new SelectableFrame();
	}
	
	// Draw frame

	private int ScrollIndex;

	public bool Draw(
		ISelectableFrame frame,
		out SceneEntity? clicked,
		bool gizmo
	) {
		clicked = null;

		if (!this.Config.Overlay.DrawDotsGizmo && ImGuizmo.Gizmo.IsUsing)
			return false;
		
		var drawList = ImGui.GetWindowDrawList();

		var items = frame.GetItems().ToList();

		var isHovering = false;
		foreach (var item in items) {
			var display = this.Config.GetEntityDisplay(item.Entity);

			var isSelect = item.Entity.IsSelected;
			item.IsHovered = display.Mode switch {
				DisplayMode.Dot => this.DrawPrimDot(drawList, item.ScreenPos, display, isSelect),
				DisplayMode.Icon => this.DrawIconDot(drawList, item.ScreenPos, display, isSelect),
				_ => false
			};

			isHovering |= item.IsHovered;
		}

		if (!isHovering) return false;
		items.RemoveAll(item => !item.IsHovered);
		return this.DrawSelectWindow(items, out clicked, gizmo);
	}

	private bool DrawSelectWindow(
		IReadOnlyList<IItemSelect> items,
		out SceneEntity? clicked,
		bool gizmo
	) {
		clicked = null;
		if (items.Count == 0 || (gizmo && (ImGuizmo.Gizmo.IsUsing || ImGuizmo.Gizmo.IsOver)))
			return false;

		var begin = false;
		try {
			ImGui.SetNextWindowPos(ImGui.GetMousePos().AddX(20.0f));
			ImGui.SetNextWindowSize(-Vector2.One, ImGuiCond.Always);
			begin = ImGui.Begin("##Hover", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing);
			if (begin) return this.DrawSelectList(items, out clicked);
		} catch (Exception err) {
			Ktisis.Log.Error($"Error drawing select list:\n{err}");
		} finally {
			if (begin) ImGui.End();
		}

		return false;
	}

	private bool DrawSelectList(IReadOnlyList<IItemSelect> list, out SceneEntity? clicked) {
		clicked = null;
		
		// Sort objects by proximity to camera.
		// TODO: Configuration.OrderBoneListByDistance
		/*list.Sort((a, b) => {
			if (Math.Abs(a.Distance - b.Distance) > 0.01f)
				return a.Distance < b.Distance ? -1 : 1;
			return 0;
		});*/
		
		// Handle mouse wheel input and clamp scroll index
		this.ScrollIndex -= (int)ImGui.GetIO().MouseWheel;
		if (this.ScrollIndex >= list.Count)
			this.ScrollIndex = 0;
		else if (this.ScrollIndex < 0)
			this.ScrollIndex = list.Count - 1;
		
		// Capture mouse input.
		ImGui.SetNextFrameWantCaptureMouse(true);
		
		// Check for mouse click
		var isClick = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

		for (var i = 0; i < list.Count; i++) {
			var item = list[i];
			var isSelect = i == this.ScrollIndex;
			ImGui.Selectable(item.Name, isSelect);
			if (isSelect && isClick)
				clicked = item.Entity;
		}

		return clicked != null;
	}
	
	// Draw UI dots

	private bool DrawPrimDot(ImDrawListPtr drawList, Vector2 pos2d, EntityDisplay display, bool isSelect = false) {
		var radius = this.Config.Overlay.DotRadius;
		if (isSelect) radius += 1.0f;
		
		drawList.AddCircleFilled(
			pos2d,
			radius,
			display.Color,
			16
		);

		drawList.AddCircle(
			pos2d,
			radius,
			0xFF000000,
			16,
			isSelect ? 2.5f : 1.0f
		);

		return IsHovering(pos2d, radius);
	}

	private bool DrawIconDot(ImDrawListPtr drawList, Vector2 pos2d, EntityDisplay display, bool isSelect = false) {
		var size = Icons.CalcIconSize(display.Icon);
		var radius = UiBuilder.IconFont.FontSize;

		var isHover = IsHovering(pos2d, radius);

		drawList.AddCircleFilled(
			pos2d,
			radius,
			isSelect ? 0xAF000000u : (isHover ? 0xCA000000u : 0x70000000u),
			16
		);

		if (isSelect)
			drawList.AddCircle(pos2d, radius, 0xFFEFEFEF, 16, 1.5f);
		
		ImGui.SetCursorPos((pos2d - size / 2));
		Icons.DrawIcon(display.Icon, display.Color);
		
		return isHover;
	}

	private const int HoverPadding = 6;

	private static bool IsHovering(Vector2 pos2d, float radius) {
		return ImGui.IsMouseHoveringRect(
			pos2d.Add(-radius - HoverPadding),
			pos2d.Add(radius + HoverPadding)
		);
	}
	
	// Frame context
	
	private class SelectableFrame : ISelectableFrame {
		private readonly List<ItemSelect> Items = new();

		public IEnumerable<IItemSelect> GetItems() => this.Items.AsReadOnly();
		
		public unsafe void AddItem(SceneEntity entity, Vector3 worldPos, IEditorContext ctx) {
			var camera = CameraService.GetSceneCamera();
			if (camera == null) return;

			if (!CameraService.WorldToScreen(camera, worldPos, out var pos2d)) return;

			var dist = Vector3.Distance(camera->Object.Position, worldPos);
			var select = new ItemSelect(entity, pos2d, dist);
			this.Items.Add(select);

			// render a short ray in the facing-direction for LightEntities
			// s/o Meddle https://github.com/PassiveModding/Meddle/blob/main/Meddle/Meddle.Plugin/UI/Layout/Overlay.cs#L221
			if (entity is LightEntity light) {
				var ptr = light.GetObject();
				if (ptr == null || ptr->RenderLight == null) return;
				if (ptr->RenderLight->LightType == LightType.PointLight) return;

				var range = Math.Min(ptr->RenderLight->Range, 1);
				var rot = light.GetTransform()?.Rotation;
				if (rot == null) return;
				// account for renderlight projection offset
				if (ptr->RenderLight->LightType == LightType.AreaLight)
					rot *= (new Vector3(ptr->RenderLight->AreaAngle.X, ptr->RenderLight->AreaAngle.Y, 0) * MathHelpers.Rad2Deg).EulerAnglesToQuaternion();

				var dir = Vector3.Transform(new Vector3(0, 0, range), (Quaternion)rot);
				if (!CameraService.WorldToScreen(camera, worldPos + dir, out var endPos2d)) return;

				var opacity = ImGuizmo.Gizmo.IsUsing ? ctx.Config.Overlay.LineOpacityUsing : ctx.Config.Overlay.LineOpacity;
				var drawList = ImGui.GetWindowDrawList();
				var display = ctx.Config.GetEntityDisplay(light);
				drawList.AddLine(pos2d, endPos2d, display.Color.SetAlpha(opacity), ctx.Config.Overlay.LineThickness);
            }
		}
	}
	
	// Item selection info

	private class ItemSelect : IItemSelect {
		public string Name => this.Entity.Name;
		
		public SceneEntity Entity { get; }
		public Vector2 ScreenPos { get; }

		public float Distance { get; }
		public readonly int SortPriority;

		public bool IsHovered { get; set; }

		public ItemSelect(SceneEntity entity, Vector2 screenPos, float dist) {
			this.Entity = entity;
			this.ScreenPos = screenPos;
			this.Distance = dist;
		}
	}
}
