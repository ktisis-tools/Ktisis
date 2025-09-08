using System.Threading.Tasks;

using Dalamud.Utility;

using Ktisis.Data.Files;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Creators;

public interface IActorCreator : IEntityCreator<ActorEntity, IActorCreator> {
	public IActorCreator WithAppearance(CharaFile file);
}

public sealed class ActorCreator : EntityCreator<ActorEntity, IActorCreator>, IActorCreator {
	private CharaFile? Appearance;

	public ActorCreator(
		ISceneManager scene
	) : base(scene) { }
	
	protected override IActorCreator Builder => this;

	public IActorCreator WithAppearance(CharaFile file) {
		this.Appearance = file;
		return this;
	}

	public async Task<ActorEntity> Spawn() {
		var module = this.Scene.GetModule<ActorModule>();

		var entity = await module.Spawn();
		
		entity.Name = this.Name.IsNullOrEmpty() ? $"Actor #{entity.Actor.ObjectIndex}" : this.Name;

		if (this.Appearance != null)
			await this.Scene.Context.Characters.ApplyCharaFile(entity, this.Appearance, gameState: true);

		// create and destroy a dummy to force GameData.ObjectManager updates to cutsceneparentindex associations
		// TODO: less hacky
		var entity2 = await module.Spawn();
		module.Delete(entity2);

		return entity;
	}
}
