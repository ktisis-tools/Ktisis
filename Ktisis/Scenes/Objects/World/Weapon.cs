using Dalamud.Interface;

using CSWeapon = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Weapon;

namespace Ktisis.Scenes.Objects.World;

public class Weapon : Character {
	// Trees

	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.Magic;
	
	// Weapon
	
	private unsafe CSWeapon* Entity => (CSWeapon*)Address;
	
	// Constructor
	
	public Weapon(nint address) : base(address) { }
}