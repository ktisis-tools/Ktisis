using System;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Editor.Strategy.Decor;
using Ktisis.Editor.Strategy.World;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.Character;

using Character = Ktisis.Structs.Character.Character;

namespace Ktisis.Editor.Strategy.Actors;

public class ActorModify : ObjectModify, ICharacter {
	protected new ActorEntity Entity { get; }

	public ActorModify(
		ActorEntity entity
	) : base(entity) {
		this.Entity = entity;
	}

	public unsafe CharacterBase* GetCharacter()
		=> this.Entity.GetCharacter();
	
	public unsafe Customize? GetCustomize() {
		var ptr = Character.From(this.GetCharacter());
		if (ptr == null) return null;
		return ptr->Customize;
	}

	public unsafe EquipmentModelId[]? GetEquipment() {
		var ptr = Character.From(this.GetCharacter());
		if (ptr == null) return null;
		return new Span<EquipmentModelId>(ptr->HumanEquip, 10).ToArray();
	}

	public unsafe void Redraw() {
	}
}
