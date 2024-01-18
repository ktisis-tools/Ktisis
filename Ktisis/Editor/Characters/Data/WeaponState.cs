using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Data;

public class WeaponState {
	private WeaponContainer _container = new();
	private readonly bool[] _state = new bool[WeaponContainer.Length];
	
	private readonly EquipmentToggle[] _visible = new EquipmentToggle[WeaponContainer.Length];

	public WeaponModelId this[WeaponIndex index] {
		get => this._container[(uint)index];
		set {
			this._container[(uint)index] = value;
			this._state[(uint)index] = true;
		}
	}

	public bool IsSet(WeaponIndex index) => this._state[(uint)index];
	public void Unset(WeaponIndex index) => this._state[(uint)index] = false;

	public EquipmentToggle GetVisible(WeaponIndex index)
		=> this._visible[(uint)index];

	public void SetVisible(WeaponIndex index, bool visible)
		=> this._visible[(uint)index] = visible ? EquipmentToggle.On : EquipmentToggle.Off;
	
	public bool CheckVisible(WeaponIndex index, bool visible) {
		var value = this._visible[(uint)index];
		return value != EquipmentToggle.None ? value == EquipmentToggle.On : visible;
	}
}
