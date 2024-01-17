using System;

using Ktisis.Data.Config;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Context;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interop.Hooking;
using Ktisis.Localization;
using Ktisis.Scene;

namespace Ktisis.Editor;

public class EditorContext : IEditorContext {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;
	
	public bool IsValid => this.IsInit && this._mediator.IsGPosing && !this.IsDisposing;

	public Configuration Config => this._mediator.Config;
	public LocaleManager Locale => this._mediator.Locale;
	
	public required IActionManager Actions { get; init; }
	public required IAppearanceManager Appearance { get; init; }
	public required ICameraManager Cameras { get; init; }
	public required ISceneManager Scene { get; init; }
	public required ISelectManager Selection { get; init; }
	public required ITransformHandler Transform { get; init; }
    
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
			this.Actions.Initialize();
			this.Appearance.Initialize();
			this.Cameras.Initialize();
		} catch {
			this.Dispose();
			throw;
		}
		return this;
	}

	public void Update() {
		this.Scene.Update();
		this.Selection.Update();
	}
	
	// Disposal

	private bool IsDisposing;

	public void Dispose() {
		this.IsDisposing = true;
		this._mediator.Destroy();
		this._scope.Dispose();
		this.Scene.Dispose();
		this.Cameras.Dispose();
		GC.SuppressFinalize(this);
	}
}
