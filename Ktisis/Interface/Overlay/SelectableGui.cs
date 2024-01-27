using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Entity;
using Ktisis.Scene.Entities;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Overlay;

public interface ISelectableFrame {
	public IEnumerable<IItemSelect> GetItems();
	
	public void AddItem(SceneEntity entity, Vector3 worldPos);
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
	private readonly CameraService _camera;
	private readonly ConfigManager _cfg;

	private Configuration Config => this._cfg.File;
	
	public SelectableGui(
		CameraService camera,
		ConfigManager cfg
	) {
		this._camera = camera;
		this._cfg = cfg;
	}
    
	public ISelectableFrame BeginFrame() {
		return new SelectableFrame(this._camera);
	}
	
	// Draw frame

	private int ScrollIndex;

	public bool Draw(ISelectableFrame frame, out SceneEntity? clicked) {
		clicked = null;
		
		var drawList = ImGui.GetBackgroundDrawList();

		var items = frame.GetItems().ToList();

		var isHovering = false;
		foreach (var item in items) {
			var display = this.Config.Editor.GetDisplayForType(item.Entity.Type);

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
		return this.DrawSelectWindow(items, out clicked);
	}

	private bool DrawSelectWindow(IReadOnlyList<IItemSelect> items, out SceneEntity? clicked) {
		clicked = null;
		if (ImGuizmo.Gizmo.IsUsing || ImGuizmo.Gizmo.IsOver || items.Count == 0)
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
		private readonly CameraService _camera;

		private readonly List<ItemSelect> Items = new();
		
		public SelectableFrame(
			CameraService camera
		) {
			this._camera = camera;
		}

		public IEnumerable<IItemSelect> GetItems() => this.Items.AsReadOnly();
		
		public unsafe void AddItem(SceneEntity entity, Vector3 worldPos) {
			var camera = this._camera.GetSceneCamera();
			if (camera == null) return;

			if (!this._camera.WorldToScreen(worldPos, out var pos2d))
				return;

			var dist = Vector3.Distance(camera->Object.Position, worldPos);
			var select = new ItemSelect(entity, pos2d, dist);
			this.Items.Add(select);
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
