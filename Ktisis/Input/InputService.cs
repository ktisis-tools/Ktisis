using Ktisis.Interop;
using Ktisis.Core.Impl;

namespace Ktisis.Input; 

[KtisisService]
public class InputService : IServiceInit {
	// Service

	private readonly InteropService _interop;

	private ControlHooks? ControlHooks;

	public InputService(InteropService _interop) {
		this._interop = _interop;
	}

	public void PreInit() {
		this.ControlHooks = this._interop.Create<ControlHooks>().Result;
	}
}
