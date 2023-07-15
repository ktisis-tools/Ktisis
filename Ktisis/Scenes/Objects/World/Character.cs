using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Interface.SceneUi.Logic;
using Ktisis.Scenes.Objects.Models;

namespace Ktisis.Scenes.Objects.World;

public class Character : WorldObject, IManipulable {
	// Character

	private unsafe CharacterBase* Entity => (CharacterBase*)Address;

	// Armature & Models

	private Armature? Armature;
	private CharaModels? Models;

	// Constructor

	public Character(nint address) : base(address) {
		Armature = new Armature();
		Armature.AddToParent(this);

		Models = new CharaModels();
		Models.AddToParent(this);
	}

	// Update armature

	internal unsafe bool IsRendering() {
		var model = this.Entity;
		return model != null && (model->UnkFlags_01 & 2) != 0 && model->UnkFlags_02 != 0;
	}

	internal override void Update() {
		// Don't do anything until the model is fully loaded.
		if (!IsRendering()) return;

		Armature?.Update();
		Models?.Update();

		// Update children
		base.Update();
	}

	// hehe

	public Matrix4x4? ComposeMatrix() {
		return IsRendering() ? GetTransform()?.ComposeMatrix() : null;
	}
}
