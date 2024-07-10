using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Structs.Lights;
using Ktisis.Scene.Factory.Builders;
using Ktisis.Scene.Factory.Creators;

namespace Ktisis.Scene.Factory.Types;

public interface IEntityFactory {
	public IActorBuilder BuildActor(IGameObject actor);
	public ILightBuilder BuildLight();
	public IObjectBuilder BuildObject();
	public IPoseBuilder BuildPose();

	public IActorCreator CreateActor();
	public ILightCreator CreateLight();
	public ILightCreator CreateLight(LightType type);
}
