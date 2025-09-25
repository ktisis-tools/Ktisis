using System;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules;

namespace Ktisis.Scene.Types;

public interface ISceneManager : IComposite, IDisposable {
	public bool IsValid { get; }
	
	public IEditorContext Context { get; }
	public IEntityFactory Factory { get; }

	public T GetModule<T>() where T : SceneModule;
	public bool TryGetModule<T>(out T? module) where T : SceneModule;
	
	public double UpdateTime { get; }

	public void Initialize();
	public void Update();
	public void Refresh();

	public ActorEntity? GetEntityForActor(IGameObject actor);
	public ActorEntity? GetEntityForIndex(uint objectIndex);
}
