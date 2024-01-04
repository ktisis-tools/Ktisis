using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;

namespace Ktisis.Editor.Actions.Input;

public interface IInputManager {
	
}

public class InputManager : IInputManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;
	
	public InputManager(
		IContextMediator mediator,
		HookScope scope
	) {
		this._mediator = mediator;
		this._scope = scope;
	}
}
