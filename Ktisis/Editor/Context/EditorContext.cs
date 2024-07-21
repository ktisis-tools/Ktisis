using System;

using Ktisis.Core.Types;
using Ktisis.Data.Config;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Animation.Types;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Types;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Interface;
using Ktisis.Interface.Editor.Types;
using Ktisis.Localization;
using Ktisis.Scene.Types;
using Ktisis.Services.Game;

namespace Ktisis.Editor.Context;

public class EditorContext : IEditorContext {
	private readonly GPoseService _gpose;
	
	private EditorState? _state;
	
	public bool IsValid => this._state is { IsValid: true };

	public EditorContext(
		GPoseService gpose,
		IPluginContext plugin
	) {
		this._gpose = gpose;
		this.Plugin = plugin;
	}
	
	// Wrappers
	
	public bool IsGPosing => this._gpose.IsGPosing;
	
	// State management
	
	public IPluginContext Plugin { get; }
	private EditorState State {
		get {
			if (this._state == null)
				throw new Exception("Attempting to access invalid context.");
			return this._state;
		}
	}

	public void Setup(EditorState state) {
		if (this._state != null)
			throw new Exception("Attempted double initialization of editor context!");
		this._state = state;
	}
	
	// Plugin wrappers

	public Configuration Config => this.Plugin.Config.File;
	public GuiManager Gui => this.Plugin.Gui;
	public LocaleManager Locale => this.Gui.Locale;
	
	// State wrappers
	
	public IActionManager Actions => this.State.Actions;
	public IAnimationManager Animation => this.State.Animation;
	public ICharacterManager Characters => this.State.Characters;
	public ICameraManager Cameras => this.State.Cameras;
	public IEditorInterface Interface => this.State.Interface;
	public IPosingManager Posing => this.State.Posing;
	public ISceneManager Scene => this.State.Scene;
	public ISelectManager Selection => this.State.Selection;
	public ITransformHandler Transform => this.State.Transform;
	
	public void Initialize() => this._state?.Initialize();
	public void Update() => this._state?.Update();
	
	// Disposal
	
	public void Dispose() {
		try {
			this._state?.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to destroy editor context:\n{err}");
		} finally {
			this._state = null;
		}
		GC.SuppressFinalize(this);
	}
}
