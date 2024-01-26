using System;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
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
}
