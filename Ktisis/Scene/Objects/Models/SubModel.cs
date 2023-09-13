using Ktisis.Data.Config.Display;
using Ktisis.Scene.Objects.World;

namespace Ktisis.Scene.Objects.Models; 

public class SubModel : WorldObject {
	// Properties

	public override ItemType ItemType => ItemType.ModelSlot;
	
	// Constructor

	public SubModel(nint address) : base(address) {}
}
