using Dalamud.Utility;

using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Data.Config.Display;
using Ktisis.Scene.Objects.World;

namespace Ktisis.Scene.Objects.Game; 

public class Actor : Character {
	// Properties

	public override string Name {
		get {
			var gameObj = this.GetGameObject();
			if (gameObj is null) return "Unknown";

			var name = gameObj.Name.TextValue;
			if (name.IsNullOrEmpty())
				name = $"Actor {gameObj.ObjectIndex}";
			return name;
		}
	}

	public override ItemType ItemType => ItemType.Actor;
	
	// Constructor
	
	public Actor(GameObject gameObj) : base(nint.Zero) {
		this._gameObject = gameObj;
	}
	
	// GameObject
	
	private readonly GameObject _gameObject;

	public GameObject? GetGameObject()
		=> this._gameObject.IsValid() ? this._gameObject : null;

	public unsafe CSGameObject* GetStruct()
		=> (CSGameObject*)(this.GetGameObject()?.Address ?? nint.Zero);
	
	// Update handler

	protected unsafe override void UpdateAddress() {
		var ptr = this.GetStruct();
		var addr = (nint)(ptr != null ? ptr->DrawObject : null);
		if (this.Address != addr)
			this.Address = addr;
	}
}