using Ktisis.Config.Display;

namespace Ktisis.Scene.Objects.Models; 

public class CharaModels : SceneObject {
	// Properties

	public override string Name => "Models";

	public override ItemType ItemType => ItemType.Models;
	
	// Update handler

	public override void Update(SceneGraph scene, SceneContext ctx) {
		// TODO
		base.Update(scene, ctx);
	}
}
