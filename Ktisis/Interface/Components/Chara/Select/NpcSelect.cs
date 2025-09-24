using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;
using GLib.Popups;

using Ktisis.Core.Attributes;
using Ktisis.GameData.Excel.Types;
using Ktisis.Localization;
using Ktisis.Services.Data;
using Ktisis.Structs.Characters;

namespace Ktisis.Interface.Components.Chara.Select;

public delegate void OnNpcSelected(INpcBase npc);

[Transient]
public class NpcSelect {
	private readonly NpcService _npc;
	private readonly LocaleManager _locale;

	// TODO: delegate search implementation to ListBox
	private readonly PopupList<INpcBase> _popup;
	
	public INpcBase? Selected { get; set; }
	public event OnNpcSelected? OnSelected;
	
	public NpcSelect(
		NpcService npc,
		LocaleManager locale
	) {
		this._npc = npc;
		this._locale = locale;
		this._popup = new PopupList<INpcBase>("##NpcImportPopup", this.DrawItem).WithSearch(MatchQuery);
		this.Fetch();
	}
	
	// Data
	
	private enum NpcLoadState {
		Waiting,
		Success,
		Failed
	}

	private NpcLoadState _npcLoadState = NpcLoadState.Waiting;
	private readonly List<INpcBase> _npcList = new();
	private List<INpcBase> _monsterList = new();

	public void Fetch() {
		if (this._npcLoadState == NpcLoadState.Success) return;
		this._npc.GetNpcList().ContinueWith(task => {
			if (task.Exception != null) {
				Ktisis.Log.Error($"Failed to fetch NPC list:\n{task.Exception}");
				this._npcLoadState = NpcLoadState.Failed;
				return;
			}

			this._npcList.Clear();
			this._npcList.AddRange(task.Result);
			this._monsterList.Clear();
			this._monsterList.AddRange(task.Result.Where(entry => entry.GetModelId() != 0));
			this._npcLoadState = NpcLoadState.Success;
		});
	}

	public void FetchMonsters() {
		if (this._npcLoadState == NpcLoadState.Success) return;
		this._npc.GetNpcList().ContinueWith(task => {
			if (task.Exception != null) {
				Ktisis.Log.Error($"Failed to fetch NPC list:\n{task.Exception}");
				this._npcLoadState = NpcLoadState.Failed;
				return;
			}

			this._npcList.Clear();
			this._npcList.AddRange(task.Result.Where(entry => entry.GetModelId() != 0));
			this._npcLoadState = NpcLoadState.Success;
		});
	}
	
	// Draw

	public void Draw() {
		switch (this._npcLoadState) {
			case NpcLoadState.Waiting:
				ImGui.Text("Loading NPCs...");
				break;
			case NpcLoadState.Failed:
				ImGui.Text("Failed to load NPCs.\nCheck your error log for more information.");
				break;
			case NpcLoadState.Success:
				this.DrawSelect();
				break;
			default:
				throw new InvalidEnumArgumentException($"Invalid value: {this._npcLoadState}");
		}

		using (var _ = ImRaii.Disabled(this.Selected == null)) {
			ImGui.SameLine();
			if (Buttons.IconButton(FontAwesomeIcon.UndoAlt))
				this.Selected = null;
		}
	}

	public void DrawSearchIcon() {
		switch (this._npcLoadState) {
			case NpcLoadState.Waiting:
				ImGui.Text("Loading NPCs...");
				break;
			case NpcLoadState.Failed:
				ImGui.Text("Failed to load NPCs.\nCheck your error log for more information.");
				break;
			case NpcLoadState.Success:
				if (Buttons.IconButtonTooltip(FontAwesomeIcon.Search, "Browse NPCs..."))
					this._popup.Open();

				var height = ImGui.GetFontSize() * 2;
				if (this._popup.Draw(this._monsterList, out var npc, height) && npc != null)
					this.Select(npc);
				break;
			default:
				throw new InvalidEnumArgumentException($"Invalid value: {this._npcLoadState}");
		}
	}

	private void DrawSelect() {
		var preview = this.Selected != null ? this.Selected.Name : "Select...";
		if (ImGui.BeginCombo("##NpcCombo", preview)) {
			ImGui.CloseCurrentPopup();
			ImGui.EndCombo();
		}
		if (ImGui.IsItemActivated())
			this._popup.Open();

		var height = ImGui.GetFontSize() * 2;
		if (this._popup.Draw(this._npcList, out var npc, height) && npc != null)
			this.Select(npc);
	}

	private void Select(INpcBase npc) {
		this.Selected = npc;
		this.OnSelected?.Invoke(npc);
	}
	
	// Popup

	private bool DrawItem(INpcBase npc, bool isFocus) {
		var style = ImGui.GetStyle();
		var fontSize = ImGui.GetFontSize();

		var result = ImGui.Selectable("##", isFocus, 0, ImGui.GetContentRegionAvail().WithY(fontSize * 2.0f));

		ImGui.SameLine(style.ItemInnerSpacing.X, 0);
		ImGui.Text(npc.Name);

		var model = npc.GetModelId();
		ImGui.SameLine(style.ItemInnerSpacing.X, 0);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize);
		if (model == 0) {
			var custom = npc.GetCustomize();
			if (custom != null && custom.Value.Tribe != 0) {
				var sex = custom.Value.Gender == Gender.Masculine ? "♂" : "♀";
				var tribe = this._locale.Translate($"{custom.Value.Tribe}");
				ImGui.TextDisabled($"{sex} {tribe}");
			} else {
				ImGui.TextDisabled("Unknown");
			}
		} else {
			ImGui.TextDisabled($"Model #{model}");
		}

		return result;
	}

	private static bool MatchQuery(INpcBase npc, string query)
		=> npc.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
}
