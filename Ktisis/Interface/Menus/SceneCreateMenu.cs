using System.Threading.Tasks;

using GLib.Popups.Context;

using Ktisis.Scene;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Menus;

public class SceneCreateMenu(ISceneManager scene) {
	public static ContextMenu Build(ISceneManager scene)
		=> new SceneCreateMenu(scene).Build();
    
	private ContextMenu Build() {
		return new ContextMenuBuilder()
			.Action("Create actor", this.CreateActor)
			.SubMenu("Create light", sub => {
				sub.Action("Point", this.CreatePoint)
					.Action("Spot", this.CreateSpot)
					.Action("Area", this.CreateArea)
					.Action("Sun", this.CreateDirectional);
			})
			.Build("##SceneObjectContext");
	}
	
	// Actors
	
	private void CreateActor() {
		scene.GetModule<ActorModule>().Spawn()
			.ConfigureAwait(false);
	}
	
	// Light
	
	private Task<LightEntity> CreateLight() => scene.GetModule<LightModule>().Spawn();

	private void CreatePoint() => this.CreateLight().ConfigureAwait(false);

	private void CreateSpot() => this.CreateLight().ContinueWith(task => {
		task.Result.SetType(LightType.SpotLight);
	}, TaskContinuationOptions.OnlyOnRanToCompletion);
	
	private void CreateArea() => this.CreateLight().ContinueWith(task => {
		task.Result.SetType(LightType.AreaLight);
	}, TaskContinuationOptions.OnlyOnRanToCompletion);
	
	private void CreateDirectional() => this.CreateLight().ContinueWith(task => {
		task.Result.SetType(LightType.Directional);
	}, TaskContinuationOptions.OnlyOnRanToCompletion);
}
