using Dalamud.Interface;

namespace Ktisis.Scenes.Objects.World;

public class Weapon : Character {
	// Trees

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.ChessPawn;

	// Constructor

	public Weapon(nint address) : base(address) { }
}
