using System.Collections.Generic;

using Dalamud.Interface;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Core;
using Ktisis.Common.Utility;
using Ktisis.Scenes.Objects.World;
using Ktisis.Scenes.Objects.Impl;

namespace Ktisis.Scenes.Objects.Game;

public class Actor : SceneObject, IManipulable {
	// UI

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.Child;

	// Actor properties

	public readonly ushort Index;

	public GameObject? GameObject => GetGameObject();
	private unsafe CSGameObject* CSGameObject => (CSGameObject*)(GameObject?.Address ?? 0);

	// Encapsulate this actor's model as a WorldObject

	private Character? Character;
	public override List<SceneObject> Children => Character?.Children!;

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
		if (Character != null) {
			if (addr != Character.Address)
				Character.Address = addr;
			if (Character.Name != Name)
				Character.Name = Name;
			Character.Update();
		} else if (model != null) {
			Character = new Character(addr) {
				Name = Name
			};
		}
	}

	// Transform wrapper

	public Transform? GetTransform()
		=> Character?.GetTransform();

	public void SetTransform(Transform trans)
		=> Character?.SetTransform(trans);

	// Helpers

	private GameObject? GetGameObject()
		=> Services.ObjectTable[Index] is GameObject gameObj && gameObj.IsValid() ? gameObj : null;

	private unsafe DrawObject* GetDrawObject() {
		var gameObj = this.CSGameObject;
		return gameObj != null ? gameObj->DrawObject : null;
	}
}
