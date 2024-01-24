using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Context;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Services;

namespace Ktisis.Scene.Factory;

public interface IEntityFactory {
	public IActorBuilder BuildActor(GameObject actor);
	public ILightBuilder BuildLight();
	public IObjectBuilder BuildObject();
	public IPoseBuilder BuildPose();
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
	
	// Builders

	public IActorBuilder BuildActor(GameObject actor) => new ActorBuilder(this.Scene, this.BuildPose(), actor);

	public ILightBuilder BuildLight() => new LightBuilder(this.Scene);

	public IObjectBuilder BuildObject() => new ObjectBuilder(this.Scene, this.BuildPose(), this._naming);

	public IPoseBuilder BuildPose() => new PoseBuilder(this.Scene);
}
