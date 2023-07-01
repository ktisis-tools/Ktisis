using Dalamud.Interface;

namespace Ktisis.Scenes.Objects.Models;

public class ModelSlots : SceneObject {
	// Trees

	public override uint Color => 0xFFBAFFB2;

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.CubesStacked;

	// Constructor

	public ModelSlots() {
		Name = "Models";
	}

	// Models

	internal override void Update() {
		// TODO: CharacterBase->Models
		// Model +0x58 can be moved
		// * -> +0xC0 to decouple it from the skeleton
	}
}
