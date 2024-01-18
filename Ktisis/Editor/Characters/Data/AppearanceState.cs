namespace Ktisis.Editor.Characters.Data;

public class AppearanceState {
	public uint ModelId = uint.MaxValue;

	public WeaponState Weapons = new();
	public EquipmentState Equipment = new();
	
	// Hat visibility

	public EquipmentToggle HatVisible { get; set; } = EquipmentToggle.None;
	public bool CheckHatVisible(bool visible) => this.HatVisible != EquipmentToggle.None ? this.HatVisible == EquipmentToggle.On : visible;
	
	// Visor toggle

	public EquipmentToggle VisorToggled { get; set; } = EquipmentToggle.None;
	public bool CheckVisorToggled(bool toggled) => this.VisorToggled != EquipmentToggle.None ? this.VisorToggled == EquipmentToggle.On : toggled;
}
