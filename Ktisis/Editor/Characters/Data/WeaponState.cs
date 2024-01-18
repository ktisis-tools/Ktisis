using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Data;

public class WeaponState {
	private WeaponContainer _container = new();
	private readonly bool[] _state = new bool[WeaponContainer.Length];

	public WeaponModelId this[WeaponIndex index] {
		get => this._container[(uint)index];
		set {
			this._container[(uint)index] = value;
			this._state[(uint)index] = true;
		}
	}

	public bool IsSet(WeaponIndex index) => this._state[(uint)index];
	public void Unset(WeaponIndex index) => this._state[(uint)index] = false;
}
