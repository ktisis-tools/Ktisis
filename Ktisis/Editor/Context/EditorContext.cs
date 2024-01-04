using System;

using Ktisis.Scene;
using Ktisis.Data.Config;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Strategy;
using Ktisis.Editor.Transforms;
using Ktisis.Interop.Hooking;
using Ktisis.Localization;
using Ktisis.Scene.Entities;

namespace Ktisis.Editor.Context;

public interface IEditorContext : IDisposable {
	public bool IsValid { get; }
	
	public Configuration Config { get; }
	public LocaleManager Locale { get; }
	
	public IActionManager Actions { get; }
	public ISceneManager Scene { get; }
	public IEntityEditor Editor { get; }
	public ISelectManager Selection { get; }
	public ITransformHandler Transform { get; }
	public IPoseModule PoseModule { get; }

	public IEditorContext Initialize();
	
	public void Update();

	public BaseEditor GetEditor(SceneEntity entity);
	public T? GetEditor<T>(SceneEntity entity) where T : BaseEditor;
}

public class EditorContext : IEditorContext {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;
	
	public bool IsValid => this.IsInit && this._mediator.IsGPosing && !this.IsDisposing;

	public Configuration Config => this._mediator.Config;
	public LocaleManager Locale => this._mediator.Locale;
	
	public required IActionManager Actions { get; init; }
	public required ISceneManager Scene { get; init; }
	public required IEntityEditor Editor { get; init; }
	public required ISelectManager Selection { get; init; }
	public required ITransformHandler Transform { get; init; }
	public required IPoseModule PoseModule { get; init; }
    
	public EditorContext(
		IContextMediator mediator,
		HookScope scope
	) {
		this._mediator = mediator;
		this._scope = scope;
	}
	
	// State
	
	private bool IsInit;

	public IEditorContext Initialize() {
		try {
			this.IsInit = true;
			this.Scene.Initialize();
			this.PoseModule.Initialize();
			this.Actions.Initialize();
		} catch {
			this.Dispose();
			throw;
		}
		return this;
	}

	public void Update() {
		this.Scene.Update();
		this.Selection.Update();
		this.Editor.Update();
	}
	
	// Wrappers

	public BaseEditor GetEditor(SceneEntity entity) => this.Editor.Get(entity);
	public T? GetEditor<T>(SceneEntity entity) where T : BaseEditor => this.Editor.Get<T>(entity);
	
	// Disposal

	private bool IsDisposing;

	public void Dispose() {
		this.IsDisposing = true;
		this._mediator.Destroy();
		this._scope.Dispose();
		this.Scene.Dispose();
		GC.SuppressFinalize(this);
	}
}
