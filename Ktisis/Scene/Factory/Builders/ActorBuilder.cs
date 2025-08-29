using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Common.Extensions;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Builders;

public interface IActorBuilder : IEntityBuilder<ActorEntity, IActorBuilder> { }

public sealed class ActorBuilder : EntityBuilder<ActorEntity, IActorBuilder>, IActorBuilder {
	private readonly IPoseBuilder _pose;
	private readonly IGameObject _gameObject;

	public ActorBuilder(
		ISceneManager scene,
		IPoseBuilder pose,
		IGameObject gameObject
	) : base(scene) {
		this.Name = gameObject.GetNameOrFallback(scene.Context.Config.Editor.IncognitoPlayerNames);
		this._pose = pose;
		this._gameObject = gameObject;
	}
	
	protected override ActorBuilder Builder => this;

	protected override ActorEntity Build() {
		return new ActorEntity(
			this.Scene,
			this._pose,
			this._gameObject
		) {
			Name = this.Name
		};
	}
}
