using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Data;

public class EquipmentState {
	// Equipment indices
	
	private EquipmentContainer _container = new();
	private readonly bool[] _state = new bool[EquipmentContainer.Length];

	public EquipmentModelId this[EquipIndex index] {
		get => this._container[(uint)index];
		set {
			this._container[(uint)index] = value;
			this._state[(uint)index] = true;
		}
	}

	public bool IsSet(EquipIndex index) => this._state[(uint)index];
	public void Unset(EquipIndex index) => this._state[(uint)index] = false;
	
	// Hat visibility

	public EquipmentVisible HatVisible { get; set; } = EquipmentVisible.None;
	
	public bool CheckHatVisible(bool visible) => this.HatVisible != EquipmentVisible.None ? this.HatVisible == EquipmentVisible.Visible : visible;

	// Flags
	
	private EquipmentFlags _flagSet = EquipmentFlags.None;
	private EquipmentFlags _flagValues = EquipmentFlags.None;

	public bool IsFlagSet(EquipmentFlags flag) => this._flagSet.HasFlag(flag);

	public void UnsetFlag(EquipmentFlags flag) => this._flagSet &= ~flag;
	
	public bool GetFlagState(EquipmentFlags flag, bool value) => this.IsFlagSet(flag) ? this._flagValues.HasFlag(flag) : value;

	public void SetFlagState(EquipmentFlags flag, bool value) {
		this._flagSet |= flag;
		if (value)
			this._flagValues |= flag;
		else
			this._flagValues &= ~flag;
	}
}
