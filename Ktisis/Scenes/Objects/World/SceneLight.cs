using Dalamud.Interface;

using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scenes.Objects.World; 

public class SceneLight : WorldObject {
	// Tree :)
	
	public override FontAwesomeIcon Icon { get; init; } = FontAwesomeIcon.Lightbulb;

	public override uint Color { get; init; } = 0xFF68EDFF;
	
	// Light

	private unsafe Light* Entity => (Light*)Address;

	// Constructor

	public SceneLight(nint addr) : base(addr) { }
}