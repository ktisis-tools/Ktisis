namespace Ktisis.Editor.Characters.Data;

public class AppearanceState {
	public uint ModelId = uint.MaxValue;

	public WeaponState Weapons = new();
	public EquipmentState Equipment = new();
}
