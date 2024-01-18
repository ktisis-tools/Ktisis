using Dalamud.Interface.Internal;

using Ktisis.Data.Excel;

namespace Ktisis.Interface.Components.Actors.Types;

public abstract class ItemInfo {
	public bool FlagUpdate { get; set; }

	public ItemSheet? Item { get; set; }
	public IDalamudTextureWrap? Texture { get; set; }
		
	public abstract EquipSlot Slot { get; }
		
	public abstract ushort ModelId { get; }
	public abstract byte StainId { get; }

	public abstract void SetEquipItem(ItemSheet item);
	public abstract void SetStainId(byte id);
	public abstract void Unequip();
	
	public abstract bool IsHideable { get; }
	public abstract bool IsVisible();
	public abstract void SetVisible(bool visible);
	
	public abstract bool IsCurrent();
	public abstract bool IsItemPredicate(ItemSheet item);
}
