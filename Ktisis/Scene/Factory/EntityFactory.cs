using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Context;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Services;

namespace Ktisis.Scene.Factory;

public interface IEntityFactory {
	public IActorBuilder CreateActor(GameObject actor);
	public ILightBuilder CreateLight();
	public IObjectBuilder CreateObject();
	public IPoseBuilder CreatePose();
}

public class EntityFactory : IEntityFactory {
	private readonly IContextMediator _mediator;
	private readonly INameResolver _naming;
	
	public EntityFactory(
		IContextMediator mediator,
		INameResolver naming
	) {
		this._mediator = mediator;
		this._naming = naming;
	}
	
	private ISceneManager Scene => this._mediator.Context.Scene;

	public IActorBuilder CreateActor(GameObject actor)
		=> new ActorBuilder(this.Scene, this.CreatePose(), actor);

	public ILightBuilder CreateLight()
		=> new LightBuilder(this.Scene);

	public IObjectBuilder CreateObject()
		=> new ObjectBuilder(this.Scene, this.CreatePose(), this._naming);

	public IPoseBuilder CreatePose()
		=> new PoseBuilder(this.Scene);
}
