using System;
using System.Collections.Generic;
using System.Linq;

using ImGuiNET;
using Dalamud.Interface;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	public static class StatusEffectControls {
		private static int InputEffect = 0;
		private static bool SearchOpen = false;
		private static string SearchTerm = string.Empty;

		private static Lazy<List<Status>> StatusSheet = new(() => Services.DataManager.GameData.GetExcelSheet<Status>()!.Where(i => i.Category != 0).ToList());

		public static unsafe void Draw(Actor* actor) {
			if (ImGui.CollapsingHeader("Status Effect Control")) {
				if (!BattleActor.ValidKinds.Contains((ObjectKind)actor->GameObject.ObjectKind) || PoseHooks.PosingEnabled) {
					ImGui.Text("Status Effect Control is available when:");
					ImGui.BulletText("Posing is disabled");
					ImGui.BulletText("Actor is type BattleNPC or Player");
					return;
				}

				BattleActor* battleActor = (BattleActor*)actor;

				ImGui.SetNextItemWidth(130f);
				ImGui.InputInt("###input_effect", ref InputEffect);
				ImGui.SameLine();
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Search, "Search"))
					SearchOpen = true;
				ImGui.SameLine();
				if (ImGui.Button("Add Effect")) {
					battleActor->StatusEffects.AddStatusEffect((ushort)InputEffect);
				}

				ImGui.Spacing();

				var effects = battleActor->StatusEffects.GetEffects();

				for (int i = 0; i < effects.Length; i++) {
					ushort effect = effects[i];
					if (effect == 0)
						continue;

					ImGui.Text($"{effect} - {StatusSheet.Value.Single((i) => i.RowId == effect).Name}");
					ImGui.SameLine();
					if (ImGui.Button($"Remove Effect###remove_effect_{i}")) {
						battleActor->StatusEffects.RemoveStatusEffect(i);
					}
				}

				ImGui.Spacing();
			}

			if (SearchOpen)
				DrawListSearchEffect();
		}

		private unsafe static void DrawListSearchEffect() {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList | PopupSelect.HoverPopupWindowFlags.SearchBar,
				StatusSheet.Value!,
				(e, input) => e.Where(t => $"{t.RowId} - {t.Name}".Contains(input, StringComparison.OrdinalIgnoreCase)),
				(t, a) => {
					// draw Line
					bool selected = ImGui.Selectable($"{t.RowId} - {t.Name}###{t}", a);
					bool focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => InputEffect = (ushort)t.RowId,
				() => SearchOpen = false,
				ref SearchTerm,
				"Effect Select",
				"##effect_select",
				"##effect_search");
		}
	}
}