using System;

using Ktisis.Editor.Actions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Types;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Interface.Editor.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Types;

namespace Ktisis.Editor;

public class EditorState : IDisposable {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;
	
	public bool IsValid => this.IsInit && this._context.IsGPosing && !this.IsDisposing;
	
	public required IActionManager Actions { get; init; }
	public required ICameraManager Cameras { get; init; }
	public required ICharacterManager Characters { get; init; }
	public required IEditorInterface Interface { get; init; }
	public required IPosingManager Posing { get; init; }
	public required ISceneManager Scene { get; init; }
	public required ISelectManager Selection { get; init; }
	public required ITransformHandler Transform { get; init; }
    
	public EditorState(
		IEditorContext context,
		HookScope scope
	) {
		this._context = context;
		this._scope = scope;
	}
	
	// Initialization
	
	private bool IsInit;

	public void Initialize() {
		try {
			this.IsInit = true;
			this.Actions.Initialize();
			this.Characters.Initialize();
			this.Cameras.Initialize();
			this.Posing.Initialize();
			this.Scene.Initialize();
		} catch {
			this.Dispose();
			throw;
		}

		try {
			this.Interface.Prepare();
		} catch (Exception err) {
			Ktisis.Log.Error($"Error preparing interface:\n{err}");
		}
	}
	
	// Update handler

	public void Update() {
		this.Scene.Update();
		this.Selection.Update();
	}
	
	// Disposal

	private bool IsDisposing;

	public void Dispose() {
		this.IsDisposing = true;
		this._scope.Dispose();
		this.Scene.Dispose();
		this.Posing.Dispose();
		this.Cameras.Dispose();
		GC.SuppressFinalize(this);
	}
}
