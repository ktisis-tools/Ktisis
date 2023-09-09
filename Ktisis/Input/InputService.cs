using Ktisis.Core;

namespace Ktisis.Input; 

public class InputService {
	// Service

	private readonly ControlHooks ControlHooks;

	public InputService(IServiceContainer _services) {
		this.ControlHooks = _services.Inject<ControlHooks>();
	}
}
