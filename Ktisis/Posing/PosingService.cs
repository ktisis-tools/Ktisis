using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Services;

namespace Ktisis.Posing; 

public class PosingService {
	// Constructor

	private readonly PoseHooks Hooks;

	public PosingService(IServiceContainer _services, GPoseService _gpose) {
		this.Hooks = _services.Inject<PoseHooks>();
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}
	
	// Posing

	public bool IsActive => this.Hooks.Enabled;

	public void Enable() {
		PluginLog.Verbose("Enabling posing hooks.");
		this.Hooks.EnableAll();
	}

	public void Disable() {
		PluginLog.Verbose("Disabling posing hooks.");
		this.Hooks.DisableAll();
	}

	public void Toggle() {
		if (this.IsActive)
			Disable();
		else
			Enable();
	}
	
	// Events

	private void OnGPoseUpdate(bool active) {
		if (!active) Disable();
	}
}