using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interop;
using Ktisis.Scene;
using Ktisis.Scene.Factory;
using Ktisis.Services;

namespace Ktisis.Editor.Context;

[Singleton]
public class ContextBuilder {
	private readonly IFramework _framework;
	private readonly InteropService _interop;
	private readonly ActionBuilder _actions;
	private readonly NamingService _naming;
	
	public ContextBuilder(
		IFramework framework,
		InteropService interop,
		ActionBuilder actions,
		NamingService naming
	) {
		this._framework = framework;
		this._interop = interop;
		this._actions = actions;
		this._naming = naming;
	}

	public IEditorContext Initialize(IContextMediator mediator) {
		var scope = this._interop.CreateScope();
		
		var actions = this._actions.Initialize(mediator, scope);
		
		var cameras = new CameraManager(mediator, scope);
		var chara = new CharacterManager(mediator, scope, this._framework);

		var posing = new PosingManager(mediator, scope);

		var nameResolver = this._naming.GetResolver();
		var factory = new EntityFactory(mediator, nameResolver);
		var scene = new SceneManager(mediator, scope, factory)
			.SetupModules();
		
		var select = new SelectManager(mediator);
		var transform = new TransformHandler(actions, select);

		var context = new EditorContext(mediator, scope) {
			Actions = actions,
			Cameras = cameras,
			Characters = chara,
			Posing = posing,
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
