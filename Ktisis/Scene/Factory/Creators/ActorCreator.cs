using System.Threading.Tasks;

using Dalamud.Utility;

using Ktisis.Data.Files;
using Ktisis.Data.Mcdf;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Factory.Creators;

public interface IActorCreator : IEntityCreator<ActorEntity, IActorCreator> {
	public IActorCreator WithAppearance(CharaFile file);
	public IActorCreator WithMcdf(string McdfPath);
}

public sealed class ActorCreator : EntityCreator<ActorEntity, IActorCreator>, IActorCreator {
	private CharaFile? Appearance;
	private string? McdfFile;
	
	private McdfManager McdfManager { get; init; }


	public ActorCreator(
		ISceneManager scene,
		McdfManager mcdfManager
	) : base(scene) {
		this.McdfManager = mcdfManager;
	}
	
	protected override IActorCreator Builder => this;

	public IActorCreator WithAppearance(CharaFile file) {
		this.Appearance = file;
		return this;
	}

	public IActorCreator WithMcdf(string mcdfPath) {
		this.McdfFile = mcdfPath;
		return this;
	}

	public async Task<ActorEntity> Spawn() {
		var module = this.Scene.GetModule<ActorModule>();

		var entity = await module.Spawn();
		
		entity.Name = this.Name.IsNullOrEmpty() ? $"Actor #{entity.Actor.ObjectIndex}" : this.Name;

		if (this.Appearance != null)
			await this.Scene.Context.Characters.ApplyCharaFile(entity, this.Appearance, gameState: true);

		if (this.McdfFile != null)
			this.McdfManager.LoadAndApplyTo(this.McdfFile, entity.Actor);

		entity.EnsurePresetVisibility();

		return entity;
	}
}
