using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ImGuiNET;
using Dalamud.Interface;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Lumina.Excel.GeneratedSheets;

using Ktisis.Interop.Hooks;
using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Components {
	public static class StatusEffectControls {
		private static readonly ReadOnlyCollection<ObjectKind> ValidKinds = new List<ObjectKind>() {ObjectKind.Pc, ObjectKind.BattleNpc}.AsReadOnly();
		private static readonly Lazy<List<Status>> StatusSheet = new(() => Services.DataManager.GameData.GetExcelSheet<Status>()!.Where(i => i.StatusCategory != 0).ToList());

		private static int InputEffect = 0;
		private static bool SearchOpen = false;
		private static string SearchTerm = string.Empty;
		
		public unsafe static void Draw(Actor* actor) {
			BattleChara* battleChara = (BattleChara*)actor;
			
			if (ImGui.CollapsingHeader("Status Effect Control")) {
				if (!ValidKinds.Contains((ObjectKind)actor->GameObject.ObjectKind) || PoseHooks.PosingEnabled) {
					ImGui.Text("Status Effect Control is available when:");
					ImGui.BulletText("Posing is disabled");
					ImGui.BulletText("Actor is type BattleNPC or Player");
					return;
				}

				ImGui.SetNextItemWidth(130f);
				ImGui.InputInt("###input_effect", ref InputEffect);
				ImGui.SameLine();
				if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Search, "Search"))
					SearchOpen = true;
				ImGui.SameLine();
				if (ImGui.Button("Add Effect")) {
					battleChara->StatusManager.AddStatus((ushort)InputEffect);
				}

				ImGui.Spacing();

				var effects = GetEffects(battleChara);

				for (var i = 0; i < effects.Length; i++) {
					var effect = effects[i];
					if (effect == 0)
						continue;

					ImGui.Text($"{effect} - {StatusSheet.Value.Single((e) => e.RowId == effect).Name}");
					ImGui.SameLine();
					if (ImGui.Button($"Remove Effect###remove_effect_{i}")) {
						battleChara->StatusManager.RemoveStatus(i);
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
					var selected = ImGui.Selectable($"{t.RowId} - {t.Name}###{t}", a);
					var focus = ImGui.IsItemFocused();
					return (selected, focus);
				},
				(t) => InputEffect = (ushort)t.RowId,
				() => SearchOpen = false,
				ref SearchTerm,
				"Effect Select",
				"##effect_select",
				"##effect_search");
		}
		
		private unsafe static ushort[] GetEffects(BattleChara* battleChara) {
			const int maxEffects = 30;
			var effects = new ushort[maxEffects];

			for (var i = 0; i < maxEffects; i++) {
				effects[i] = (ushort) battleChara->StatusManager.GetStatusId(i);
			}

			return effects;
		}
	}
}