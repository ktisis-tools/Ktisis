using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Core;
using Ktisis.Scenes.Objects.World;

namespace Ktisis.Scenes.Objects.Game;

public class Actor : SceneObject {
	// UI

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.Child;

	// Actor properties

	public readonly ushort Index;

	public GameObject? GameObject => GetGameObject();
	private unsafe CSGameObject* CSGameObject => (CSGameObject*)(GameObject?.Address ?? 0);

	// Wrap this actor's model around a WorldObject
	
	private WorldObject? WorldObject;
	public override List<SceneObject>? Children => WorldObject?.Children;

	// Constructor

	private Actor(ushort index) {
		Index = index;
		if (GameObject is GameObject gameObject)
			Name = gameObject.Name.TextValue;
	}

	public static Actor? FromIndex(ushort index)
		=> Services.ObjectTable[index]?.IsValid() is true ? new Actor(index) : null;
	
	// Update

	internal unsafe override void Update() {
		var model = GetDrawObject();
		var addr = (nint)model;
		if (WorldObject != null) {
			if (addr != WorldObject.Address)
				WorldObject.Address = addr;
			WorldObject.Update();
		} else if (model != null)
			WorldObject = new WorldObject(addr);
	}
	
	// Helpers

	private GameObject? GetGameObject()
		=> Services.ObjectTable[Index] is GameObject gameObj && gameObj.IsValid() ? gameObj : null;

	private unsafe DrawObject* GetDrawObject() {
		var gameObj = this.CSGameObject;
		return gameObj != null ? gameObj->DrawObject : null;
	}
}