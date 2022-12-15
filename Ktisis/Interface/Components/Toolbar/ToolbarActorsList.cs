using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components.Toolbar {
	internal static class ToolbarActorsList {

		// Draw
		public unsafe static void Draw() {
			// Prevent displaying the same target multiple time
			ActorsList.SavedObjects = ActorsList.SavedObjects.Distinct().ToList();
			ActorsList.SavedObjects.RemoveAll(o => !ActorsList.IsValidActor(o));

			var currentTarget = Ktisis.Target;
			if (!ActorsList.SavedObjects.Contains((long)currentTarget)) ActorsList.SavedObjects.Add((long)currentTarget);

			// Index of Currently selected
			var selectedIndex = -1;
			for (var index = 0; index < ActorsList.SavedObjects.Count; index++) {
				if (ActorsList.SavedObjects[index] != (long)currentTarget)
					continue;
				selectedIndex = index;
				break;
			}

			var actorNamesList = ActorsList.SavedObjects.Select(pointer => ((Actor*)pointer)->GetNameOrId() + ActorsList.ExtraInfo(pointer)).ToArray();
			ImGui.Combo("", ref selectedIndex, actorNamesList, actorNamesList.Length);
			GuiHelpers.Tooltip("Select active Actor");
			Services.Targets->GPoseTarget = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)ActorsList.SavedObjects[selectedIndex];
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Add Actor to list."))
				ActorsList.OpenSelector();

			ImGui.SameLine();

			if (ActorsList.SelectorList != null)
				ActorsList.DrawListAddActor();
		}

	}
}
