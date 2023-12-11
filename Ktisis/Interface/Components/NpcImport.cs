using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Utility.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;

using ImGuiNET;

using GLib.Popups;

using Ktisis.Util;
using Ktisis.Data.Files;
using Ktisis.Data.Npc;
using Ktisis.Localization;
using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Components {
	public class NpcImport {
		private readonly PopupList<INpcBase> _popup;

		private bool _npcListRaii;
		private readonly List<INpcBase> NpcList = new();

		public NpcImport() {
			this._popup = new PopupList<INpcBase>("##NpcImportPopup", DrawItem)
				.WithSearch(MatchQuery);
		}
		
		// Draw handlers

		private bool DrawItem(INpcBase npc, bool isFocus) {
			var style = ImGui.GetStyle();
			var fontSize = ImGui.GetFontSize();
            var result = ImGui.Selectable("##", isFocus, 0, ImGui.GetContentRegionAvail().WithY(fontSize * 2));

            ImGui.SameLine(style.ItemInnerSpacing.X, 0);
			ImGui.Text(npc.Name);
			
			var model = npc.GetModelId();
			ImGui.SameLine(style.ItemInnerSpacing.X, 0);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize);
			if (model == 0) {
				var custom = npc.GetCustomize();
				if (custom != null && custom.Value.Tribe != 0) {
					var value = custom.Value;
					var sex = value.Gender == Gender.Masculine ? "♂" : "♀";
					ImGui.TextDisabled($"{sex} {Locale.GetString(value.Tribe.ToString())}");
				} else {
					ImGui.TextDisabled("Unknown");
				}
			} else {
				ImGui.TextDisabled($"Model #{model}");
			}
			
			return result;
		}

		private bool MatchQuery(INpcBase npc, string query)
			=> npc.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
		
		// Open handler

		public void Open() {
			if (!this._npcListRaii) {
				this._npcListRaii = true;
				FetchNpcList();
			}
			
			this._popup.Open();
		}
		
		// Fetch NPC list on open

		private void FetchNpcList() {
			NpcImportService.GetNpcList().ContinueWith(task => {
				if (task.Exception != null) {
					Logger.Error($"Failed to retrieve NPC list:\n{task.Exception}");
					return;
				}

				foreach (var chunk in task.Result.Chunk(6000)) {
					lock (this.NpcList) {
						this.NpcList.AddRange(chunk);
					}
				}
			});
		}
		
		// Draw UI

		public void Draw(AnamCharaFile.SaveModes mode) {
			if (!this._popup.IsOpen)
				return;

			var height = ImGui.GetFontSize() * 2;
			lock (this.NpcList) {
				if (this._popup.Draw(this.NpcList, out var selected, height) && selected != null) {
					Services.Framework.RunOnFrameworkThread(() => {
						ApplyNpc(selected, mode);
					});
				}
			}
		}
		
		// Handle NPC select

		private unsafe void ApplyNpc(INpcBase npc, AnamCharaFile.SaveModes mode) {
			var target = Ktisis.Target;
			if (target == null) return;

			var body = mode.HasFlag(AnamCharaFile.SaveModes.AppearanceBody);
			var face = mode.HasFlag(AnamCharaFile.SaveModes.AppearanceFace);
			var hair = mode.HasFlag(AnamCharaFile.SaveModes.AppearanceHair);

			if (body) {
				var modelId = npc.GetModelId();
				if (modelId != ushort.MaxValue)
					target->ModelId = modelId;
			}

			if (body || face || hair) {
				var custom = npc.GetCustomize();
				if (custom != null && IsCustomizeValid(custom.Value, target->DrawData.Customize)) {
					var value = custom.Value;
					for (var i = 0; i < Customize.Length; i++) {
						var valid = (CustomizeIndex)i switch {
							CustomizeIndex.FaceType
								or (>= CustomizeIndex.FaceFeatures and <= CustomizeIndex.LipColor)
								or CustomizeIndex.Facepaint
								or CustomizeIndex.FacepaintColor => face,
							CustomizeIndex.HairStyle
								or CustomizeIndex.HairColor
								or CustomizeIndex.HairColor2
								or CustomizeIndex.HasHighlights => hair,
							(>= CustomizeIndex.Race and <= CustomizeIndex.Tribe)
								or (>= CustomizeIndex.RaceFeatureSize and <= CustomizeIndex.BustSize) => face || body,
							_ => body
						};
						if (!valid) continue;
						target->DrawData.Customize.Bytes[i] = value.Bytes[i];
					}
				}
			}

			var gear = mode.HasFlag(AnamCharaFile.SaveModes.EquipmentGear);
			var accs = mode.HasFlag(AnamCharaFile.SaveModes.EquipmentAccessories);
			if (gear || accs) {
				var equip = npc.GetEquipment();
				if (equip != null && IsEquipValid(equip.Value, target->DrawData.Equipment)) {
					var value = equip.Value;
					for (var i = 0; i < Structs.Actor.Equipment.SlotCount; i++) {
						var valid = (EquipIndex)i switch {
							<= EquipIndex.Feet => gear,
							<= EquipIndex.RingLeft => accs,
							_ => true
						};
						if (!valid) continue;
						target->DrawData.Equipment.Slots[i] = value.Slots[i];
					}
				}
			}
            
			if (mode.HasFlag(AnamCharaFile.SaveModes.EquipmentWeapons)) {
				var main = npc.GetMainHand();
				if (main != null)
					target->DrawData.MainHand.Equip = main.Value;

				var off = npc.GetOffHand();
				if (off != null)
					target->DrawData.OffHand.Equip = off.Value;
			}

			target->Redraw();
		}
		
		private unsafe bool IsCustomizeValid(Customize custom, Customize current) {
			for (var i = 0; i < Customize.Length; i++)
				if (custom.Bytes[i] != 0 && custom.Bytes[i] != current.Bytes[i])
					return true;
			return false;
		}

		private unsafe bool IsEquipValid(Structs.Actor.Equipment equip, Structs.Actor.Equipment current) {
			var isValid = false;
			var isUnique = false;
            
			for (var i = 0; i < Structs.Actor.Equipment.SlotCount; i++) {
				var item = equip.Slots[i];
				isValid |= item != 0;
				isUnique |= item != current.Slots[i];
				if (isValid && isUnique)
					return true;
			}

			return false;
		}
	}
}
