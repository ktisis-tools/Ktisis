using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scene.Objects.Render; 

public class SceneLight : RenderObject {
	// Light

	private unsafe Light* Light => (Light*)this.Address;
	
	// Constructor
	
	public SceneLight(nint address) : base(address) {}
}