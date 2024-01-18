using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Data.Excel;
using Ktisis.Editor.Characters.Data;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Actors.Types;

public class EquipInfo(IEquipmentEditor editor, ActorEntity actor) : ItemInfo {
	public required EquipIndex Index;
	public required EquipmentModelId Model;

	public override EquipSlot Slot => this.Index.ToEquipSlot();

	public override ushort ModelId => this.Model.Id;
	public override byte StainId => this.Model.Stain;

	public void SetModel(ushort id, byte variant) => editor.SetEquipIdVariant(actor, this.Index, id, variant);
	public override void SetEquipItem(ItemSheet item) => this.SetModel(item.Model.Id, (byte)item.Model.Variant);
	public override void SetStainId(byte id) => editor.SetEquipStainId(actor, this.Index, id);
	public override void Unequip() => this.SetModel(0, 0);

	public override bool IsHideable => this.Slot is EquipSlot.Head;
	public override bool IsVisible => this.Slot is EquipSlot.Head && editor.GetHatVisible(actor);
	public override void SetVisible(bool visible) {
		if (this.Slot is EquipSlot.Head)
			editor.SetHatVisible(actor, visible);
	}

	public override bool IsVisor => this.Slot is EquipSlot.Head;
	public override bool IsVisorToggled => this.Slot is EquipSlot.Head && editor.GetVisorToggled(actor);
	public override void SetVisorToggled(bool toggled) {
		if (this.Slot is EquipSlot.Head)
			editor.SetVisorToggled(actor, toggled);
	}

	public override bool IsCurrent() => editor.GetEquipIndex(actor, this.Index).Equals(this.Model);
	
	public override bool IsItemPredicate(ItemSheet item) => item.Model.Matches(this.Model.Id, this.Model.Variant);
}
