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

		private static List<long> SavedObjects = new();
		private static bool IsSelectorListOpened = false;
		private static string Search = "";


		public unsafe static void Draw() {
			var currentTarget = Ktisis.Target;
			if (!SavedObjects.Any(t => t == (long)currentTarget)) SavedObjects.Add((long)currentTarget);

			SavedObjects.RemoveAll(o => {
				var target = (GameObject*)o;
				if (target == null) return true;
				// if (!Services.Targets->IsObjectInViewRange(target)) return true; // "View range" is not "Spawn range", we want a way to check if the actor has despawned
				return false;
			});


			var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, ControlButtons.ButtonSize.Y);

			if (ImGui.CollapsingHeader("Actor List")) {
				long? toRemove = null;
				foreach (var pointer in SavedObjects) {

					var target = (GameObject*)pointer;
					if (target == null) continue;
					var actor = (Actor*)pointer;
					if (actor == null) continue;

					if (ImGui.Button($"{actor->GetNameOrId()}##ActorList##{pointer}", buttonSize))
						Services.Targets->GPoseTarget = target; // TODO: check if this is safe for expected actors, and unexpected actors
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
			var meAndMyMinion = Services.Targets->ObjectFilterArray1;
			var otherPeopleAndNpc = Services.Targets->ObjectFilterArray2;
			var meAndMyMinionAgain = Services.Targets->ObjectFilterArray3;

			List<long> allObjectsAround = new();
			for (int i = 0; i < meAndMyMinion.Length; i++)
				allObjectsAround.Add((long)meAndMyMinion[i]);
			for (int i = 0; i < otherPeopleAndNpc.Length; i++)
				allObjectsAround.Add((long)otherPeopleAndNpc[i]);
			for (int i = 0; i < meAndMyMinionAgain.Length; i++)
				allObjectsAround.Add((long)meAndMyMinionAgain[i]);

			var sanitizedObjects = allObjectsAround.Where(t => {
				var actor = (Actor*)t;
				if (actor == null) return false;
				return true;
			}).Distinct();


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
	}
}
