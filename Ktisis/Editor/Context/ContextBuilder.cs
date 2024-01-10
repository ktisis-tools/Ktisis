using Ktisis.Core.Attributes;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interop;
using Ktisis.Scene;
using Ktisis.Scene.Modules;

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
		
		var scene = new SceneManager(mediator, scope)
			.AddModule<ActorModule>()
			.AddModule<LightModule>()
			.AddModule<EnvModule>();
		
		var select = new SelectManager(mediator);
		var transform = new TransformHandler(actions, select);

		var poseModule = scope.Create<PoseHooks>();

		var context = new EditorContext(mediator, scope) {
			Actions = actions,
			Scene = scene,
			Selection = select,
			Transform = transform,
			PoseModule = poseModule
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
