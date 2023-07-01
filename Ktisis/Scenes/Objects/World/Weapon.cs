using Dalamud.Interface;

namespace Ktisis.Scenes.Objects.World; 

// TODO: UX: Consider treating weapons standalone as an armature root?
// Unless I want to parent objects to weapons. That sounds fun.

public class Weapon : Character {
	// Trees

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.ChessPawn;
	
	// Constructor
	
	public Weapon(nint address) : base(address) { }
}