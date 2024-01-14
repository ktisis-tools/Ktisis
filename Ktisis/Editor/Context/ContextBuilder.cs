using Ktisis.Core.Attributes;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interop;
using Ktisis.Scene;

namespace Ktisis.Editor.Context;

[Singleton]
public class ContextBuilder {
	private readonly InteropService _interop;
	private readonly ActionBuilder _actions;
	
	public ContextBuilder(
		InteropService interop,
		ActionBuilder actions
	) {
		this._interop = interop;
		this._actions = actions;
	}

	public IEditorContext Initialize(IContextMediator mediator) {
		var scope = this._interop.CreateScope();
		
		var actions = this._actions.Initialize(mediator, scope);

		var cameras = new CameraManager(mediator, scope);
		
		var scene = new SceneManager(mediator, scope)
			.SetupModules();
		
		var select = new SelectManager(mediator);
		var transform = new TransformHandler(actions, select);

		var context = new EditorContext(mediator, scope) {
			Actions = actions,
			Cameras = cameras,
			Scene = scene,
			Selection = select,
			Transform = transform
		};

		try {
			mediator.Initialize(context);
		} catch {
			context.Dispose();
			throw;
		}

		return context;
	}
}
