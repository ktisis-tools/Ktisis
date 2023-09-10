using Dalamud.Logging;

using Ktisis.Interop;
using Ktisis.Core.Services;
using Ktisis.Core.Impl;

namespace Ktisis.Posing;

[KtisisService]
public class PosingService : IServiceInit {
	// Constructor

	private readonly InteropService _interop;

	private PoseHooks? Hooks;

	public PosingService(InteropService _interop, GPoseService _gpose) {
		this._interop = _interop;
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}

	public void PreInit() {
		this.Hooks = this._interop.Create<PoseHooks>().Result;
	}
	
	// Posing

	public bool IsActive => this.Hooks?.Enabled ?? false;

	public void Enable() {
		PluginLog.Verbose("Enabling posing hooks.");
		this.Hooks?.EnableAll();
	}

	public void Disable() {
		PluginLog.Verbose("Disabling posing hooks.");
		this.Hooks?.DisableAll();
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
