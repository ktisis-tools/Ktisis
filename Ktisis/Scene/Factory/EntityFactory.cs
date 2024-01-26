using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Context;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Factory.Creators;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;
using Ktisis.Services;
using Ktisis.Services.Data;
using Ktisis.Structs.Lights;

namespace Ktisis.Scene.Factory;

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

	private IEditorContext Context => this._mediator.Context;
	private ISceneManager Scene => this.Context.Scene;
	
	// Builders

	public IActorBuilder BuildActor(GameObject actor) => new ActorBuilder(this.Scene, this.BuildPose(), actor);

	public ILightBuilder BuildLight() => new LightBuilder(this.Scene);

	public IObjectBuilder BuildObject() => new ObjectBuilder(this.Scene, this.BuildPose(), this._naming);

	public IPoseBuilder BuildPose() => new PoseBuilder(this.Scene);
	
	// Creators

	public IActorCreator CreateActor() => new ActorCreator(this.Context);

	public ILightCreator CreateLight() => new LightCreator(this.Scene);

	public ILightCreator CreateLight(LightType type) => this.CreateLight().SetType(type);
}
