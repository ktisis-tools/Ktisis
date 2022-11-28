using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components.Toolbar {
	internal static class ToolbarActorsList {

		private static List<long> SavedObjects = new();
		private static List<GameObject>? SelectorList;
		private static string Search = "";
		private static readonly HashSet<ObjectKind> WhitelistObjectKinds = new() {
			ObjectKind.Player,
			ObjectKind.BattleNpc,
			ObjectKind.EventNpc,
			ObjectKind.MountType,
			ObjectKind.Companion,
		};

		// Draw
		public unsafe static void Draw() {
			// Prevent displaying the same target multiple time
			SavedObjects = SavedObjects.Distinct().ToList();
			SavedObjects.RemoveAll(o => !IsValidActor(o));

			var currentTarget = Ktisis.Target;
			if (!SavedObjects.Contains((long)currentTarget)) SavedObjects.Add((long)currentTarget);

			// Index of Currently selected
			var selectedIndex = -1;
			for (var index = 0; index < SavedObjects.Count; index++) {
				if (SavedObjects[index] != (long)currentTarget)
					continue;
				selectedIndex = index;
				break;
			}

			var actorNamesList = SavedObjects.Select(pointer => ((Actor*)pointer)->GetNameOrId() + ExtraInfo(pointer)).ToArray();
			ImGui.Combo("", ref selectedIndex, actorNamesList, actorNamesList.Length);
			GuiHelpers.Tooltip("Select active Actor");
			Services.Targets->GPoseTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)SavedObjects[selectedIndex];
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Add Actor to list."))
				OpenSelector();

			ImGui.SameLine();

			if (SelectorList != null)
				DrawListAddActor();
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
			var gameObject = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)target;

			var actor = (Actor*)gameObject;
			if (actor->Model == null) return false;

			var objectKind = (ObjectKind)gameObject->ObjectKind;
			return WhitelistObjectKinds.Contains(objectKind);

		}

		private static bool IsValidActor(GameObject gameObject) =>
			IsValidActor((long)gameObject.Address);

		private static bool IsNonNetworkObject(GameObject gameObject) =>
			gameObject.ObjectId == GameObject.InvalidGameObjectId;

		private static string ExtraInfo(GameObject gameObject) {
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

		private static float Distance(GameObject gameObject) =>
			(float)Math.Sqrt(gameObject.YalmDistanceX * gameObject.YalmDistanceX + gameObject.YalmDistanceZ * gameObject.YalmDistanceZ);

		private static bool IsYou(GameObject gameObject) =>
			GetGposeId(gameObject) == 201;

		private static bool IsGposeActor(GameObject gameObject) =>
			GetGposeId(gameObject) >= 200;

		private static bool IsGposeSpecialObject(GameObject gameObject) =>
			// this matches the weird object on ObjectID 200
			GetGposeId(gameObject) == 200;

		private static bool IsPlayer(GameObject gameObject) =>
			gameObject.ObjectKind == ObjectKind.Player;

		private static bool IsPlayerNotGpose(GameObject gameObject) =>
			gameObject.ObjectKind == ObjectKind.Player && GetGposeId(gameObject) < 200;

		private unsafe static byte GetGposeId(GameObject gameObject) =>
			((Actor*)gameObject.Address)->ObjectID;

	}
}