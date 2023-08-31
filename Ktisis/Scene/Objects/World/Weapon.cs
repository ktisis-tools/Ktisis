using Ktisis.Data.Config.Display;

namespace Ktisis.Scene.Objects.World; 

public class Weapon : Character {
	// Properties

	public override ItemType ItemType => ItemType.Weapon;
	
	// Constructor
	
	public Weapon(nint address) : base(address) {}
}