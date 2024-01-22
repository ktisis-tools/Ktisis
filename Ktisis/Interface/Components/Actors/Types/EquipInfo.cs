using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.GameData.Excel;

namespace Ktisis.Interface.Components.Actors.Types;

public class EquipInfo(IEquipmentEditor editor) : ItemInfo {
	public required EquipIndex Index;
	public required EquipmentModelId Model;

	public override EquipSlot Slot => this.Index.ToEquipSlot();

	public override ushort ModelId => this.Model.Id;
	public override byte StainId => this.Model.Stain;

	public void SetModel(ushort id, byte variant) => editor.SetEquipIdVariant(this.Index, id, variant);
	public override void SetEquipItem(ItemSheet item) => this.SetModel(item.Model.Id, (byte)item.Model.Variant);
	public override void SetStainId(byte id) => editor.SetEquipStainId(this.Index, id);
	public override void Unequip() => this.SetModel(0, 0);

	public override bool IsHideable => this.Slot is EquipSlot.Head;
	public override bool IsVisible => this.Slot is EquipSlot.Head && editor.GetHatVisible();
	public override void SetVisible(bool visible) {
		if (this.Slot is EquipSlot.Head)
			editor.SetHatVisible(visible);
	}

	public override bool IsVisor => this.Slot is EquipSlot.Head;
	public override bool IsVisorToggled => this.Slot is EquipSlot.Head && editor.GetVisorToggled();
	public override void SetVisorToggled(bool toggled) {
		if (this.Slot is EquipSlot.Head)
			editor.SetVisorToggled(toggled);
	}

	public override bool IsCurrent() => editor.GetEquipIndex(this.Index).Equals(this.Model);
	
	public override bool IsItemPredicate(ItemSheet item) => item.Model.Matches(this.Model.Id, this.Model.Variant);
}
