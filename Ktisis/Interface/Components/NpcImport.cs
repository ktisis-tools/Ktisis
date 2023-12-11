﻿using System;
using System.Linq;
using System.Collections.Generic;

using ImGuiNET;

using GLib.Popups;

using Ktisis.Data.Npc;
using Ktisis.Structs.Actor;
using Ktisis.Util;

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

		private bool DrawItem(INpcBase npc, bool isFocus)
			=> ImGui.Selectable(npc.Name, isFocus);

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

		public void Draw() {
			if (!this._popup.IsOpen)
				return;

			lock (this.NpcList) {
				if (this._popup.Draw(this.NpcList, out var selected) && selected != null)
					OnNpcSelect(selected);
			}
		}
		
		// Handle NPC select

		private void OnNpcSelect(INpcBase npc) {
			Services.Framework.RunOnFrameworkThread(() => {
				ApplyNpc(npc);
			});
		}

		private unsafe void ApplyNpc(INpcBase npc) {
			var target = Ktisis.Target;
			if (target == null) return;

			var modelId = npc.GetModelId();
			if (modelId != ushort.MaxValue)
				target->ModelId = modelId;

			var custom = npc.GetCustomize();
			if (custom != null && IsCustomizeValid(custom.Value, target->DrawData.Customize)) {
				var value = custom.Value;
				for (var i = 0; i < Customize.Length; i++)
					target->DrawData.Customize.Bytes[i] = value.Bytes[i];
			}

			var equip = npc.GetEquipment();
			if (equip != null && IsEquipValid(equip.Value, target->DrawData.Equipment)) {
				var value = equip.Value;
				for (var i = 0; i < Structs.Actor.Equipment.SlotCount; i++)
					target->DrawData.Equipment.Slots[i] = value.Slots[i];
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
