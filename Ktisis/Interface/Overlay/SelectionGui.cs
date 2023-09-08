using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Logging;

using ImGuiNET;

using Ktisis.Data;
using Ktisis.Data.Config.Display;
using Ktisis.Common.Extensions;
using Ktisis.Data.Config;
using Ktisis.Interface.Widgets;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Impl;
using Ktisis.Services;

namespace Ktisis.Interface.Overlay;

public delegate void OnItemSelectedHandler(SceneObject item);

public class SelectionGui {
	// Constructor

	private readonly CameraService _camera;
	private readonly ConfigService _cfg;

	public SelectionGui(CameraService _camera, ConfigService _cfg) {
		this._camera = _camera;
		this._cfg = _cfg;
	}

	// Events

	public event OnItemSelectedHandler? OnItemSelected;

	// Items

	private readonly List<ItemSelect> Items = new();

	public void Clear() => this.Items.Clear();

	public unsafe void AddItem(SceneObject item, Vector3 worldPos) {
		var camera = this._camera.GetSceneCamera();
		if (camera == null) return;

		if (!camera->WorldToScreen(worldPos, out var pos2d))
			return;

		var dist = Vector3.Distance(camera->Object.Position, worldPos);
		var select = new ItemSelect(item, pos2d, dist);
		this.Items.Add(select);
	}

	// Draw

	private int ScrollIndex;

	public void Draw() {
		var drawList = ImGui.GetBackgroundDrawList();

		var isHovering = false;
		foreach (var item in this.Items) {
			var display = this._cfg.GetItemDisplay(item.Item.ItemType);
			var pos2d = new Vector2(item.ScreenPos.X, item.ScreenPos.Y);

			var isSelect = item.Item.IsSelected();
			item.IsHovered = display.Mode switch {
				DisplayMode.Dot => DrawPrimDot(drawList, pos2d, display, isSelect),
				DisplayMode.Icon => DrawIconDot(drawList, pos2d, display, isSelect),
				_ => false
			};

			isHovering |= item.IsHovered;
		}

		if (isHovering)
			DrawSelectWindow(this.Items.Where(item => item.IsHovered).ToList());

		this.Clear();
	}

	private void DrawSelectWindow(List<ItemSelect> list) {
		if (ImGuizmo.Gizmo.IsUsing || ImGuizmo.Gizmo.IsOver || list.Count == 0)
			return;

		var begin = false;
		try {
			ImGui.SetNextWindowPos(ImGui.GetMousePos().AddX(20));
			ImGui.SetNextWindowSize(-Vector2.One, ImGuiCond.Always);
			begin = ImGui.Begin("Hover", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing);
			if (begin) DrawSelectList(list);
		} catch (Exception err) {
			PluginLog.Error($"Error while drawing select list:\n{err}");
		} finally {
			if (begin) ImGui.End();
		}
	}

	private void DrawSelectList(List<ItemSelect> list) {
		// Sort objects by those closest to the camera.
		//  TODO: Configuration.OrderBoneListByDistance
		list.Sort((a, b) => {
			if (Math.Abs(a.Distance - b.Distance) > 0.01f)
				return a.Distance < b.Distance ? -1 : 1;
			return a.SortPriority - b.SortPriority;
		});

		// Handle mouse wheel input and clamp scroll index
		this.ScrollIndex -= (int)ImGui.GetIO().MouseWheel;
		if (this.ScrollIndex >= list.Count)
			this.ScrollIndex = 0;
		else if (this.ScrollIndex < 0)
			this.ScrollIndex = list.Count - 1;

		// Capture mouse input to intercept mouse clicks.
		ImGui.SetNextFrameWantCaptureMouse(true);

		// Check for mouse click.
		var isClick = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

		for (var i = 0; i < list.Count; i++) {
			var item = list[i];
			var isSelect = i == this.ScrollIndex;
			ImGui.Selectable(item.Name, isSelect);
			if (isSelect && isClick)
				this.OnItemSelected?.Invoke(item.Item);
		}
	}

	// Draw UI dot

	private const int HoverPadding = 6;

	private bool IsHovering(Vector2 pos2d, float radius) => ImGui.IsMouseHoveringRect(
		pos2d.Add(-radius - HoverPadding),
		pos2d.Add(radius + HoverPadding)
	);

	private bool DrawPrimDot(ImDrawListPtr drawList, Vector2 pos2d, ItemDisplay display, bool isSelect = false) {
		// TODO
		var radius = isSelect ? 8f : 7f;

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
			isSelect ? 2.5f : 1f
		);

		return IsHovering(pos2d, radius);
	}

	private bool DrawIconDot(ImDrawListPtr drawList, Vector2 pos2d, ItemDisplay display, bool isSelect = false) {
		var size = Icons.CalcIconSize(display.Icon);
		var radius = UiBuilder.IconFont.FontSize;

		var isHover = IsHovering(pos2d, radius);

		drawList.AddCircleFilled(
			pos2d,
			radius,
			isSelect ? 0xAF000000u : (isHover ? 0x9A000000u : 0x70000000u),
			16
		);

		if (isSelect)
			drawList.AddCircle(pos2d, radius, 0xFFEFEFEF, 16, 1.5f);

		ImGui.SetCursorPos((pos2d - size / 2).AddX(0.75f));
		Icons.DrawIcon(display.Icon, display.Color);

		return isHover;
	}

	// ItemSelect

	private class ItemSelect {
		public readonly SceneObject Item;
		public readonly Vector2 ScreenPos;

		public readonly float Distance;
		public readonly int SortPriority;

		public string Name => this.Item.Name;

		public bool IsHovered;

		public ItemSelect(SceneObject item, Vector2 screenPos, float dist) {
			this.Item = item;
			this.ScreenPos = screenPos;

			this.Distance = dist;
			if (item is ISortPriority prio)
				this.SortPriority = prio.SortPriority;
		}
	}
}
