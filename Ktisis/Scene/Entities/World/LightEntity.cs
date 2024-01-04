using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.World;

public class LightEntity : WorldEntity {
	public LightEntity(
		ISceneManager scene
	) : base(scene) {
		this.Type = EntityType.Light;
	}
}
