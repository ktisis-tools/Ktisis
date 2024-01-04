using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Editor.Strategy.Actors;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Types;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Ktisis.Scene.Entities.Game;

public class ActorEntity : CharaEntity {
	public readonly GameObject Actor;

	public override bool IsValid => this.Scene.IsValid && this.Actor.IsValid();
	
	public ushort ObjectIndex => this.Actor.ObjectIndex;

	public ActorEntity(
		ISceneManager scene,
		IPoseBuilder pose,
		GameObject actor
	) : base(scene, pose) {
		this.Type = EntityType.Actor;
		this.Actor = actor;
	}
	
	// Update handler

	public override void Update() {
		if (!this.IsValid) return;
		this.UpdateChara();
		base.Update();
	}

	private unsafe void UpdateChara() {
		var address = (nint)this.GetCharacter();
		if (this.Address != address)
			this.Address = address;
	}
	
	// CharacterBase

	private unsafe CSGameObject* CsGameObject => (CSGameObject*)this.Actor.Address;

	public override unsafe CharacterBase* GetCharacter() {
		var ptr = this.CsGameObject != null ? this.CsGameObject->DrawObject : null;
		if (ptr == null || ptr->Object.GetObjectType() != ObjectType.CharacterBase)
			return null;
		return (CharacterBase*)ptr;
	}
}
