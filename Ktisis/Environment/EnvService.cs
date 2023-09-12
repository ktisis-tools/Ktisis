using Ktisis.Interop;
using Ktisis.Core.Impl;
using Ktisis.Core.Services;

namespace Ktisis.Environment; 

[KtisisService]
public class EnvService : IServiceInit {
	// Constructor

	private readonly InteropService _interop;
    
	private EnvHooks? EnvHooks;
	
	public EnvService(InteropService _interop, GPoseService _gpose) {
		this._interop = _interop;

		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}

	public void PreInit() {
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
