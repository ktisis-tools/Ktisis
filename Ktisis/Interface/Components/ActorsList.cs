using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Interface;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	internal static class ActorsList {

		private static List<long> SavedObjects = new();
		private static List<DalamudGameObject>? SelectorList = null;
		private static string Search = "";
		private static readonly HashSet<ObjectKind> WhitelistObjectKinds = new(){
				ObjectKind.Player,
				ObjectKind.BattleNpc,
				ObjectKind.EventNpc,
				ObjectKind.MountType,
				ObjectKind.Companion,
			};

		// TODO to clear the list on gpose leave
		public static void Clear() => SavedObjects.Clear();


		// Draw

		public unsafe static void Draw() {
			// Prevent displaying the same target multiple time
			SavedObjects = SavedObjects.Distinct().ToList();

			var currentTarget = Ktisis.Target;
			if (!SavedObjects.Contains((long)currentTarget)) SavedObjects.Add((long)currentTarget);

			// Remove invalid actors, as SelectorList is only created on the list opening
			// It can be actors turning invalid
			SavedObjects.RemoveAll(o => !IsValidActor(o));

			var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, ControlButtons.ButtonSize.Y);
			if (ImGui.CollapsingHeader("Actor List")) {
				long? toRemove = null;
				foreach (var pointer in SavedObjects) {
					if (!IsValidActor(pointer)) continue;
					if (ImGui.Button($"{((Actor*)pointer)->GetNameOrId()}{ExtraInfo(pointer)}##ActorList##{pointer}", buttonSize))
						Services.Targets->GPoseTarget = (GameObject*)pointer;
					if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
						toRemove = pointer;
				}
				if (toRemove != null) SavedObjects.Remove((long)toRemove);

				if (GuiHelpers.IconButtonTooltip(Dalamud.Interface.FontAwesomeIcon.Plus, "Add Actor", ControlButtons.ButtonSize))
					OpenSelector();

				ImGui.SameLine(ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X) - GuiHelpers.CalcIconSize(FontAwesomeIcon.InfoCircle).X);
				ControlButtons.VerticalAlignTextOnButtonSize();

				// help hover
				GuiHelpers.Icon(FontAwesomeIcon.InfoCircle, false);
				if (ImGui.IsItemHovered()) {
					ImGui.BeginTooltip();
					ImGui.Text("Right click to remove an Actor from the list");
					ImGui.EndTooltip();
				}
				if (SelectorList != null)
					DrawListAddActor();
			}
		}

		private static void OpenSelector() =>
			SelectorList = Services.ObjectTable
			// filter unwanted objects
			.Where(o =>
				o.IsValid()
				&& IsValidActor(o)
				// && !IsPlayerNotGpose(o) // uncomment to prevent selectability of non-gpose players
				&& !IsGposeSpecialObject(o))

			// group gpose and non-gpose instances of the same player to only get the gpose player object
			// (TODO: add world id or name, for accuracy)
			// non-gpose companions don't have a model in gpose, they're filtered by IsValidActor()
			.GroupBy(o => IsPlayer(o) ? o.Name.TextValue : o.Name.TextValue + "_" + GetGposeId(o))
			.Select(o => o.OrderBy(t => !IsGposeActor(t)).First())

			// order by closest to the player
			.OrderBy(a => Distance(a))
			.ToList();

		private static void CloseSelector() => SelectorList = null;

		private unsafe static void DrawListAddActor() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				SelectorList!,
				(e, input) => e.Where(t => ((Actor*)t.Address)->GetNameOrId().Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => { // draw Line
					bool selected = ImGui.Selectable($"{((Actor*)t.Address)->GetNameOrId()}{ExtraInfo(t)}##{t}", a);
					bool focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => SavedObjects.Add((long)t.Address), // on Select
				CloseSelector, // on close
				ref Search,
				"Actor Select",
				"##actor_select",
				"##actor_search");
		}


		// Filters

		private unsafe static bool IsValidActor(long target) {
			var gameObject = (GameObject*)target;
			if (gameObject == null) return false;

			var actor = (Actor*)gameObject;
			if (actor == null) return false;
			if (actor->Model == null) return false;

			var objectKind = (ObjectKind)gameObject->ObjectKind;
			if (!WhitelistObjectKinds.Contains(objectKind))
				return false;

			return true;
		}
		private static bool IsValidActor(DalamudGameObject gameObject) =>
			IsValidActor((long)gameObject.Address);
		private static bool IsNonNetworkObject(DalamudGameObject gameObject) =>
			gameObject.ObjectId == DalamudGameObject.InvalidGameObjectId;
		private static string ExtraInfo(DalamudGameObject gameObject) {
			List<string> info = new();
			if (IsGposeActor(gameObject))
				info.Add("GPose");
			if (IsYou(gameObject))
				info.Add("You");
			else if (IsPlayer(gameObject))
				info.Add("Player");
			return info.Any() ? $" ({String.Join(", ", info)})" : "";
		}
		private static string ExtraInfo(long gameObjectPointer) =>
			ExtraInfo(Services.ObjectTable.CreateObjectReference((IntPtr)gameObjectPointer)!);
		private static float Distance(DalamudGameObject gameObject) =>
			(float)Math.Sqrt(gameObject.YalmDistanceX * gameObject.YalmDistanceX + gameObject.YalmDistanceZ * gameObject.YalmDistanceZ);
		private static bool IsYou(DalamudGameObject gameObject) =>
			GetGposeId(gameObject) == 201;
		private static bool IsGposeActor(DalamudGameObject gameObject) =>
			GetGposeId(gameObject) >= 200;
		private static bool IsGposeSpecialObject(DalamudGameObject gameObject) =>
			// this matches the weird object on ObjectID 200
			GetGposeId(gameObject) == 200;
		private static bool IsPlayer(DalamudGameObject gameObject) =>
			gameObject.ObjectKind == ObjectKind.Player;
		private static bool IsPlayerNotGpose(DalamudGameObject gameObject) =>
			gameObject.ObjectKind == ObjectKind.Player && GetGposeId(gameObject) < 200;
		private unsafe static byte GetGposeId(DalamudGameObject gameObject) =>
			((Actor*)gameObject.Address)->ObjectID;

	}
}
