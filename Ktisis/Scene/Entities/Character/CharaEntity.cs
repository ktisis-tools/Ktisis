using System;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Structs.Characters;

namespace Ktisis.Scene.Entities.Character;

public abstract class CharaEntity : WorldEntity, ICharacter {
	private readonly IPoseBuilder _pose;

	protected CharaEntity(
		ISceneManager scene,
		IPoseBuilder pose
	) : base(scene) {
		this._pose = pose;
	}
	
	// Setup & update handling

	public override void Setup() {
		base.Setup();
		this._pose.Add(this);
	}

	public override void Update() {
		if (this.IsDrawing())
			base.Update();
	}

	public unsafe bool IsDrawing() {
		var ptr = this.GetCharacter();
		if (ptr == null) return false;
		return (ptr->UnkFlags_01 & 2) != 0 && ptr->UnkFlags_02 != 0;
	}
	
	// Character
	
	public virtual unsafe CharacterBase* GetCharacter() => (CharacterBase*)this.GetObject();
	
	public unsafe Customize? GetCustomize() {
		var ptr = this.GetCharacter();
		if (ptr == null) return null;
		return Structs.Characters.Character.From(ptr)->Customize;
	}

	public unsafe EquipmentModelId[]? GetEquipment() {
		var ptr = Structs.Characters.Character.From(this.GetCharacter());
		if (ptr == null) return null;
		return new Span<EquipmentModelId>(ptr->HumanEquip, 10).ToArray();
	}
}
