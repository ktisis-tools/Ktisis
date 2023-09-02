using System;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Data;
using Ktisis.Services;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Widgets;
using Ktisis.Data.Config.Display;

namespace Ktisis.Interface.Overlay;

public delegate void OnItemSelectedHandler(SceneObject item);

public class DotSelection {
	// Constructor
	
	private readonly CameraService _camera;
	private readonly DataService _data;

	public DotSelection(CameraService _camera, DataService _data) {
		this._camera = _camera;
		this._data = _data;
	}
	
	// Events

	public event OnItemSelectedHandler? OnItemSelected;
	
	// Item select dots

	private readonly List<DotItem> HoverItems = new();

	public void Clear() => this.HoverItems.Clear();

	public unsafe void AddItem(SceneObject item, Vector3 worldPos) {
		var camera = this._camera.GetSceneCamera();
		if (camera == null) return;

		if (!camera->WorldToScreen(worldPos, out var pos2d))
			return;

		var dist = Vector3.Distance(camera->Object.Position, worldPos);

		var display = this._data.GetConfig()
			.GetItemDisplay(item.ItemType);

		var isSelect = item.HasFlag(ObjectFlags.Selected);
		var isHover = display.Mode switch {
			DisplayMode.Dot => DrawPrimDot(pos2d, display),
			DisplayMode.Icon => DrawIconDot(pos2d, display, isSelect),
			_ => false
		};

		if (!isSelect && isHover) {
			var pos = new Vector3(pos2d, dist);
			var hItem = new DotItem(item, pos);
			this.HoverItems.Add(hItem);
		}
	}
	
	// Draw UI dot

	private const int HoverPadding = 6;

	private bool IsHovering(Vector2 pos2d, float radius) => ImGui.IsMouseHoveringRect(
		pos2d.Add(-radius - HoverPadding),
		pos2d.Add(radius + HoverPadding)
	);

	private bool DrawPrimDot(Vector2 pos2d, ItemDisplay display) {
		// TODO
		const float radius = 7f;
		
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddCircleFilled(
			pos2d,
			radius,
			display.Color,
			64
		);
		
		drawList.AddCircle(
			pos2d,
			radius,
			0xFF000000,
			64,
			1f
		);
		
		return IsHovering(pos2d, radius);
	}

	private bool DrawIconDot(Vector2 pos2d, ItemDisplay display, bool isSelect = false) {
		var size = Icons.CalcIconSize(display.Icon);
		var radius = UiBuilder.IconFont.FontSize;

		var isHover = IsHovering(pos2d, radius);

		var drawList = ImGui.GetWindowDrawList();
		drawList.AddCircleFilled(
			pos2d,
			radius,
			isSelect ? 0xAF000000u : (isHover ? 0x9A000000u : 0x70000000u),
			64
		);
		
		if (isSelect)
			drawList.AddCircle(pos2d, radius, 0xFF000000, 64, 2f);

		ImGui.SetCursorPos((pos2d - size / 2).AddX(0.75f));
		Icons.DrawIcon(display.Icon, display.Color);
		
		return isHover;
	}
	
	// Draw selection list

	private int ScrollIndex;

	public void DrawHoverWindow() {
		if (ImGuizmo.Gizmo.IsUsing || ImGuizmo.Gizmo.IsOver || this.HoverItems.Count == 0)
			return;
		
		var begin = false;
		try {
			ImGui.SetNextWindowPos(ImGui.GetMousePos().AddX(20));
			ImGui.SetNextWindowSize(-Vector2.One, ImGuiCond.Always);
			begin = ImGui.Begin("Hover", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing);
			if (begin)
				DrawHoverList();
		} catch (Exception err) {
			PluginLog.Error($"Error while drawing dot select:\n{err}");
		} finally {
			if (begin)
				ImGui.End();
			Clear();
		}
	}

	private unsafe void DrawHoverList() {
        // TODO: Configuration.OrderBoneListByDistance
		// Sort objects by those closest to the camera.
        
		this.HoverItems.Sort((a, b) => {
			if (Math.Abs(a.ScreenPos.Z - b.ScreenPos.Z) > 0.01f)
				return a.ScreenPos.Z < b.ScreenPos.Z ? -1 : 1;
			return a.SortPriority - b.SortPriority;
		});
		
		// Handle mouse wheel input and clamp index value
		
		this.ScrollIndex -= (int)ImGui.GetIO().MouseWheel;
		if (this.ScrollIndex >= this.HoverItems.Count)
			this.ScrollIndex = 0;
		else if (this.ScrollIndex < 0)
			this.ScrollIndex = this.HoverItems.Count - 1;
		
		// Capture mouse input to intercept mouse clicks.
		ImGui.SetNextFrameWantCaptureMouse(true);
		
		// Check for mouse click.
		var isClick = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

		for (var i = 0; i < this.HoverItems.Count; i++) {
			var item = this.HoverItems[i];
			var isSelect = i == this.ScrollIndex;
			ImGui.Selectable(item.Name, isSelect);
			if (isSelect && isClick)
				this.OnItemSelected?.Invoke(item.Item);
		}
		
		ImGui.End();
	}
	
	// SelectItem

	private class DotItem {
		public readonly SceneObject Item;
		public readonly Vector3 ScreenPos;

		public readonly int SortPriority;

		public string Name => this.Item.Name;

		public DotItem(SceneObject item, Vector3 screenPos) {
			this.Item = item;
			this.ScreenPos = screenPos;
			if (item is ArmatureNode arm)
				this.SortPriority = arm.SortPriority;
		}
	}
}