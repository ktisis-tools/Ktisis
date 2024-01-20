using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.State;

public class CustomizeState {
	private CustomizeContainer _container = new();
	private readonly bool[] _state = new bool[CustomizeContainer.Size];

	public byte this[CustomizeIndex index] {
		get => this._container[(uint)index];
		set {
			this._container[(uint)index] = value;
			this._state[(uint)index] = true;
		}
	}

	public void SetIfActive(CustomizeIndex index, byte value) {
		if (this.IsSet(index))
			this[index] = value;
	}

	public bool IsSet(CustomizeIndex index) => this._state[(uint)index];
	public void Unset(CustomizeIndex index) => this._state[(uint)index] = false;
}
