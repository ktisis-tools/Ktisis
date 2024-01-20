using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Data.Excel;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Components.Actors.Types;

public class WeaponInfo(IEquipmentEditor editor, ActorEntity actor) : ItemInfo {
	public required WeaponIndex Index;
	public required WeaponModelId Model;
		
	public override EquipSlot Slot => this.Index.ToEquipSlot();
		
	public override ushort ModelId => this.Model.Id;
	public override byte StainId => this.Model.Stain;

	public void SetModel(ushort id, ushort second, byte variant)
		=> editor.SetWeaponIdBaseVariant(actor, this.Index, id, second, variant);

	public override void SetEquipItem(ItemSheet item) {
		var isMainHand = this.Index == WeaponIndex.MainHand;
		var model = isMainHand && item.Model.Id != 0 || item.SubModel.Id == 0 ? item.Model : item.SubModel;
		this.SetModel(model.Id, model.Base, (byte)model.Variant);
		if (isMainHand && item.SubModel.Id != 0)
			editor.SetWeaponIdBaseVariant(actor, WeaponIndex.OffHand, item.SubModel.Id, item.SubModel.Base, (byte)item.SubModel.Variant);
	}

	public override void SetStainId(byte id) => editor.SetWeaponStainId(actor, this.Index, id);
	public override void Unequip() => this.SetModel(0, 0, 0);

	public override bool IsHideable => true;
	public override bool IsVisible => editor.GetWeaponVisible(actor, this.Index);
	public override void SetVisible(bool visible) => editor.SetWeaponVisible(actor, this.Index, visible);
	
	public override bool IsCurrent() => editor.GetWeaponIndex(actor, this.Index).Equals(this.Model);
	
	public override bool IsItemPredicate(ItemSheet item)
		=> item.Model.Matches(this.Model.Id, this.Model.Type, this.Model.Variant)
			|| item.SubModel.Id != 0 && item.SubModel.Matches(this.Model.Id, this.Model.Type, this.Model.Variant);
}
