using System;

using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Data.Files;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Context.Types;
using Ktisis.GameData.Excel.Types;
using Ktisis.Interface.Components.Chara.Select;
using Ktisis.Interface.Components.Files;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows.Import;

public class CharaImportDialog : EntityEditWindow<ActorEntity> {
	private readonly IEditorContext _ctx;

	private readonly NpcSelect _npcs;
	private readonly FileSelect<CharaFile> _select;

	public CharaImportDialog(
		IEditorContext ctx,
		NpcSelect npc,
		FileSelect<CharaFile> select
	) : base(
		"Import Appearance",
		ctx,
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._ctx = ctx;
		this._npcs = npc;
		this._npcs.OnSelected += this.OnNpcSelected;
		this._select = select;
		this._select.OpenDialog = this.OnFileDialogOpen;
	}
	
	// Events

	public override void OnOpen() {
		this._npcs.Fetch();
	}
	
	private void OnFileDialogOpen(FileSelect<CharaFile> sender) {
		this._ctx.Interface.OpenCharaFile(sender.SetFile);
	}

	private void OnNpcSelected(INpcBase _) {
		if (this.Context.Config.File.ImportNpcApplyOnSelect)
			this.ApplyNpc();
	}
	
	// Draw UI

	private enum LoadMethod {
		File,
		Npc
	}

	private LoadMethod Method = LoadMethod.File;
	
	private bool HasSelection => this.Method switch {
		LoadMethod.File => this._select.IsFileOpened,
		LoadMethod.Npc => this._npcs.Selected != null,
		_ => false
	};
	
	public override void Draw() {
		ImGui.Text($"Importing appearance for {this.Target.Name}");
		ImGui.Spacing();
		
		this.DrawMethodRadio("File", LoadMethod.File);
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		this.DrawMethodRadio("NPC", LoadMethod.Npc);
		
		ImGui.Spacing();

		switch (this.Method) {
			case LoadMethod.File:
				this._select.Draw();
				break;
			case LoadMethod.Npc:
				this._npcs.Draw();
				ImGui.Spacing();
				ImGui.Checkbox("Apply on selection", ref this.Context.Config.File.ImportNpcApplyOnSelect);
				break;
			default:
				throw new ArgumentOutOfRangeException(this.Method.ToString());
		}
		
		ImGui.Spacing();
		this.DrawCharaApplication();
	}

	private void DrawMethodRadio(string label, LoadMethod method) {
		if (ImGui.RadioButton(label, this.Method == method))
			this.Method = method;
	}

	private void DrawCharaApplication() {
		this.DrawModesSelect();
		
		ImGui.Spacing();
		ImGui.Spacing();
		
		using var _ = ImRaii.Disabled(!this.HasSelection);
		if (ImGui.Button("Apply"))
			this.Apply();
		
		ImGui.Spacing();
	}

	private void DrawModesSelect() {
		ImGui.Text("Appearance");
		this.DrawModeSwitch("Body", SaveModes.AppearanceBody);
		ImGui.SameLine();
		this.DrawModeSwitch("Face", SaveModes.AppearanceFace);
		ImGui.SameLine();
		this.DrawModeSwitch("Hair", SaveModes.AppearanceHair);
		
		ImGui.Spacing();
		
		ImGui.Text("Equipment");
		this.DrawModeSwitch("Gear", SaveModes.EquipmentGear);
		ImGui.SameLine();
		this.DrawModeSwitch("Accessories", SaveModes.EquipmentAccessories);
		ImGui.SameLine();
		this.DrawModeSwitch("Weapons", SaveModes.EquipmentWeapons);
	}

	private void DrawModeSwitch(string label, SaveModes mode) {
		var enabled = this._ctx.Config.File.ImportCharaModes.HasFlag(mode);
		if (ImGui.Checkbox($"{label}##CharaImportDialog_{mode}", ref enabled))
			this._ctx.Config.File.ImportCharaModes ^= mode;
	}
	
	// Application

	private void Apply() {
		switch (this.Method) {
			case LoadMethod.File:
				this.ApplyCharaFile();
				break;
			case LoadMethod.Npc:
				this.ApplyNpc();
				break;
			default:
				throw new ArgumentOutOfRangeException(this.Method.ToString());
		}
	}

	private void ApplyCharaFile() {
		var file = this._select.Selected?.File;
		if (file != null)
			this.Context.Characters.ApplyCharaFile(this.Target, file, this._ctx.Config.File.ImportCharaModes);
	}

	private void ApplyNpc() {
		var npc = this._npcs.Selected;
		if (npc != null)
			this.Context.Characters.ApplyNpc(this.Target, npc, this._ctx.Config.File.ImportCharaModes);
	}
}
