using System;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Editor.Strategy.Decor;
using Ktisis.Editor.Strategy.World;
using Ktisis.Interop.Structs.Character;
using Ktisis.Scene.Entities.Game;

using Character = Ktisis.Interop.Structs.Character.Character;

namespace Ktisis.Editor.Strategy.Actors;

public class ActorEditor : ObjectEditor, ICharacter {
	protected new ActorEntity Entity { get; }

	public ActorEditor(
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
}
