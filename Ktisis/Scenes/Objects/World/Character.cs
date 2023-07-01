using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Scenes.Objects.Models;

namespace Ktisis.Scenes.Objects.World; 

public class Character : WorldObject {
	// Character

	private unsafe CharacterBase* Entity => (CharacterBase*)Address;
	
	// Armature & Models

	private Armature? Armature;
	private ModelSlots? Models;
	
	// Constructor

	public Character(nint address) : base(address) {
		Armature = new Armature();
		Armature.ParentTo(this);

		Models = new ModelSlots();
		Models.ParentTo(this);
	}
	
	// Update armature

	internal unsafe override void Update() {
		// Don't do anything until the model is fully loaded.
		var model = this.Entity;
		if (model == null || model->UnkFlags_02 == 0) return;

		Armature?.Update();
		
		// Update children
		base.Update();
	}
}