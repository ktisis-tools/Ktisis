using System;

using Dalamud.Game;
using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Services;

namespace Ktisis.Scene; 

public class SceneManager : IDisposable {
	// Service
    
	private readonly Framework _framework;
	private readonly GPoseService _gpose;
	private readonly IServiceContainer _services;

	public SceneManager(Framework _framework, GPoseService _gpose, IServiceContainer _services) {
		this._services = _services;
		this._framework = _framework;
		this._gpose = _gpose;
		
		_framework.Update += OnFrameworkUpdate;
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}
	
	// Scene state
	
	public SceneGraph? Scene { get; private set; }

	public SelectState? SelectState => this.Scene?.Select;
	
	// Events

	private void OnFrameworkUpdate(object _sender) {
		if (this.IsDisposed) return;
		
		if (this._gpose.IsInGPose)
			this.Scene?.Update();
	}

	private void OnGPoseUpdate(bool active) {
		if (this.IsDisposed) return;
		
		if (active) {
			PluginLog.Verbose("Entering gpose, setting up scene...");
			this.Scene = this._services.Inject<SceneGraph>();
			this.Scene.Build();
		} else {
			PluginLog.Verbose("Leaving gpose, cleaning up scene...");
			this.Scene = null;
		}
	}
	
	// Disposal

	private bool IsDisposed;
    
	public void Dispose() {
		if (this.IsDisposed) return;
		this.IsDisposed = true;
		
		this._framework.Update -= OnFrameworkUpdate;
		this._gpose.OnGPoseUpdate -= OnGPoseUpdate;
		
		this.Scene = null;
	}
}