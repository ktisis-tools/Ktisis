using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Scene.Factory.Builders;

namespace Ktisis.Scene.Factory;

public interface IEntityFactory {
	public IActorBuilder CreateActor(GameObject actor);
	public ILightBuilder CreateLight();
	public IObjectBuilder CreateObject();
	public IPoseBuilder CreatePose();
}

public class EntityFactory : IEntityFactory {
	private readonly ISceneManager _scene;
	
	public EntityFactory(
		ISceneManager scene
	) {
		this._scene = scene;
	}

	public IActorBuilder CreateActor(GameObject actor)
		=> new ActorBuilder(this._scene, this.CreatePose(), actor);

	public ILightBuilder CreateLight()
		=> new LightBuilder(this._scene);

	public IObjectBuilder CreateObject()
		=> new ObjectBuilder(this._scene, this.CreatePose());

	public IPoseBuilder CreatePose()
		=> new PoseBuilder(this._scene);
}
