using Ktisis.Data.Config.Display;
using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scene.Objects.Render; 

public class SceneLight : RenderObject {
	// Properties

	public override ItemType ItemType => ItemType.Light;
	
	// Light

	private unsafe Light* Light => (Light*)this.Address;
	
	// Constructor
	
	public SceneLight(nint address) : base(address) {}
}