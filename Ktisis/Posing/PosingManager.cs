using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Interop;
using Ktisis.Services;

namespace Ktisis.Posing;

[DIService]
public class PosingManager {
	// Constructor

	private readonly InteropManager _interop;

	private PoseHooks? Hooks;

	public PosingManager(
		InteropManager _interop,
		GPoseService _gpose,
		InitHooksEvent _initHooks
	) {
		this._interop = _interop;
		_gpose.OnGPoseUpdate += OnGPoseUpdate;

		_initHooks.Subscribe(InitHooks);
	}
	
	private void InitHooks() {
		this.Hooks = this._interop.Create<PoseHooks>().Result;
	}
	
	// Posing

	public bool IsActive => this.Hooks?.Enabled ?? false;

	public void Enable() {
		Ktisis.Log.Verbose("Enabling posing hooks.");
		this.Hooks?.EnableAll();
	}

	public void Disable() {
		Ktisis.Log.Verbose("Disabling posing hooks.");
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
