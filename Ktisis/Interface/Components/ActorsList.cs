using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	internal static class ActorsList {

		private static List<long> SavedObjects = new(); // TODO: clean the list on gpose leave
		private static bool IsSelectorListOpened = false;
		private static string Search = "";
		private static readonly ObjectKind[] WhitelistObjectKinds =  {
				ObjectKind.Pc,
				ObjectKind.BattleNpc,
				ObjectKind.EventNpc,
				ObjectKind.Mount,
				ObjectKind.Companion,
			};

		public unsafe static void Draw() {
			// This cleans up the list a little, while waiting for the TODO to clean it on gpose leave
			SavedObjects = SavedObjects.Distinct().ToList();

			var currentTarget = Ktisis.Target;
			if (!SavedObjects.Contains((long)currentTarget)) SavedObjects.Add((long)currentTarget);

			SavedObjects.RemoveAll(o => !IsValidActor(o));

			var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, ControlButtons.ButtonSize.Y);
			if (ImGui.CollapsingHeader("Actor List")) {
				long? toRemove = null;
				foreach (var pointer in SavedObjects) {
					if (!IsValidActor(pointer)) continue;
					if (ImGui.Button($"{((Actor*)pointer)->GetNameOrId()}##ActorList##{pointer}", buttonSize))
						Services.Targets->GPoseTarget = (GameObject*)pointer; // TODO: check if this is safe for expected actors, and unexpected actors
					if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
						toRemove = pointer;
				}
				if (toRemove != null) SavedObjects.Remove((long)toRemove);

				if (GuiHelpers.IconButtonTooltip(Dalamud.Interface.FontAwesomeIcon.Plus, "Add Actor", ControlButtons.ButtonSize))
					OpenSelector();

				if (IsSelectorListOpened)
					DrawListAddActor();
			}
		}

		public static void OpenSelector() => IsSelectorListOpened = true;
		public static void CloseSelector() => IsSelectorListOpened = false;


		private unsafe static void DrawListAddActor() {
			//var sight = Services.Targets->ObjectFilterArray0;
			var playerMinionFriends = Services.Targets->ObjectFilterArray1;
			var otherPlayersAndNpc = Services.Targets->ObjectFilterArray2;
			var playerMinionFriendsAgain = Services.Targets->ObjectFilterArray3;

			List<long> allObjectsAround = new();
			for (int i = 0; i < playerMinionFriends.Length; i++)
				allObjectsAround.Add((long)playerMinionFriends[i]);
			for (int i = 0; i < otherPlayersAndNpc.Length; i++)
				allObjectsAround.Add((long)otherPlayersAndNpc[i]);
			for (int i = 0; i < playerMinionFriendsAgain.Length; i++)
				allObjectsAround.Add((long)playerMinionFriendsAgain[i]);

			var sanitizedObjects = allObjectsAround.Where(IsValidActor).Distinct();

			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				sanitizedObjects,
				(e, input) => e.Where(t => ((Actor*)t)->GetNameOrId().Contains(input, StringComparison.OrdinalIgnoreCase)),
				(i) => { },
				(t, a) => { // draw Line
					bool selected = ImGui.Selectable($"{((Actor*)t)->GetNameOrId()}##{t}", a);
					bool focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => SavedObjects.Add(t), // on Select
				CloseSelector, // on close
				ref Search,
				"Actor Select",
				"##actor_select",
				"##actor_search");
		}

		public static unsafe bool IsValidActor(long target) {
			var gameObject = (GameObject*)target;
			if (gameObject == null) return false;

			var actor = (Actor*)gameObject;
			if (actor == null) return false;

			var objectKind = (ObjectKind)gameObject->ObjectKind;
			if (!WhitelistObjectKinds.Contains(objectKind))
				return false;

			//PluginLog.Debug($"{((Actor*)target)->GetNameOrId()} Kind:{gameObject->ObjectKind} SubKind:{gameObject->SubKind}");
			//both ennemies and Striking dummies are 2 5

			return true;
		}
	}
}
