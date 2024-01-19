using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.State;

public class EquipmentState {
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
}
