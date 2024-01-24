using System.Threading.Tasks;

using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Scene.Types;
using Ktisis.Structs.Lights;

namespace Ktisis.Scene.Factory.Creators;

public interface ILightCreator : IEntityCreator<LightEntity, ILightCreator> {
	public ILightCreator SetType(LightType type);
}

public sealed class LightCreator : EntityCreator<LightEntity, ILightCreator>, ILightCreator {
	private LightType Type = LightType.SpotLight;
	
	public LightCreator(
		ISceneManager scene
	) : base(scene) {
		this.Name = "Light";
	}
	
	protected override ILightCreator Builder => this;

	public ILightCreator SetType(LightType type) {
		this.Type = type;
		return this;
	}

	public async Task<LightEntity> Spawn() {
		var light = await this.Scene.GetModule<LightModule>().Spawn();
		light.Name = this.Name;
		light.SetType(this.Type);
		return light;
	}
}
