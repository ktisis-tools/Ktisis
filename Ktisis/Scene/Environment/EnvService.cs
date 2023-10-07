using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Interop;
using Ktisis.Services;

namespace Ktisis.Scene.Environment; 

[DIService]
public class EnvService {
	// Constructor

	private readonly InteropManager _interop;
	
	private EnvHooks? EnvHooks;
	
	public EnvService(
		InteropManager _interop,
		GPoseService _gpose,
		InitHooksEvent _initHooks
	) {
		this._interop = _interop;

		_gpose.OnGPoseUpdate += OnGPoseUpdate;

		_initHooks.Subscribe(InitHooks);
	}

	private void InitHooks() {
		this.EnvHooks = this._interop.Create<EnvHooks>().Result;
	}
	
	// State

	public EnvHooks.EnvOverride? GetOverride() => this.EnvHooks?.Overrides;
	
	// Events

	private void OnGPoseUpdate(bool active) {
		if (active) {
			this.EnvHooks?.CopyOverride();
			this.EnvHooks?.EnableAll();
		} else this.EnvHooks?.DisableAll();
	}
}
