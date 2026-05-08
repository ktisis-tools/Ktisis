using System;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Types;
using System.Threading.Tasks;

using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules;
using Ktisis.Editor.Lights;
using Ktisis.Data.Files;
using Ktisis.Scene.Entities.World;
using Ktisis.Services.Game;
using Ktisis.Services.Data;

namespace Ktisis.Scene.Types;

public interface ISceneManager : IComposite, IDisposable {
	public bool IsValid { get; }
	
	public IEditorContext Context { get; }
	public IEntityFactory Factory { get; }
	public OverlayService Overlay { get; }
	public SceneDataService Data { get; }

	public T GetModule<T>() where T : SceneModule;
	public bool TryGetModule<T>(out T? module) where T : SceneModule;
	
	public double UpdateTime { get; }

	public void Initialize();
	public void Update();
	public void Refresh();

	public ActorEntity? GetEntityForActor(IGameObject actor);
	public ActorEntity? GetEntityForIndex(uint objectIndex);
	public ActorEntity GetFirstActor();
	public Task ApplyLightFile(LightEntity light, LightFile file);
	public Task<LightFile> SaveLightFile(LightEntity light);
	public Vector3 GetSceneOrigin();
	public Vector3 GetActorRelativePosition(Vector3 Position);
}
