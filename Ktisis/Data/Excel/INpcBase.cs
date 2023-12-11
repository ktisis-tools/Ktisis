using Ktisis.Structs.Actor;

namespace Ktisis.Data.Npc {
	public interface INpcBase {
		public string Name { get; set; }

		public ushort GetModelId() => 0;
		
		public Customize? GetCustomize() => null;

		public Equipment? GetEquipment() => null;

		public WeaponEquip? GetMainHand();

		public WeaponEquip? GetOffHand();
	}
}
