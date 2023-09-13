using Ktisis.Config.Display;
using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scene.Objects.World; 

public class SceneLight : WorldObject {
	// Properties

	public override ItemType ItemType => ItemType.Light;
	
	// Light

	private unsafe Light* Light => (Light*)this.Address;
	
	// Constructor
	
	public SceneLight(nint address) : base(address) {}
}
