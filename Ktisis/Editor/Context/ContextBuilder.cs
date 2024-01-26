using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interface;
using Ktisis.Interface.Editor;
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
	private readonly GuiManager _gui;
	
	public ContextBuilder(
		IFramework framework,
		InteropService interop,
		ActionBuilder actions,
		NamingService naming,
		GuiManager gui
	) {
		this._framework = framework;
		this._interop = interop;
		this._actions = actions;
		this._naming = naming;
		this._gui = gui;
	}

	public IEditorContext Initialize(IContextMediator mediator) {
		var scope = this._interop.CreateScope();
		
		var actions = this._actions.Initialize(mediator, scope);
		
		var cameras = new CameraManager(mediator, scope);
		
		var chara = new CharacterManager(mediator, scope, this._framework);
		
		var ui = new EditorInterface(mediator, this._gui);

		var posing = new PosingManager(mediator, scope, this._framework);

		var nameResolver = this._naming.GetResolver();
		var factory = new EntityFactory(mediator, nameResolver);
		var scene = new SceneManager(mediator, scope, factory)
			.SetupModules();
		
		var select = new SelectManager(mediator);
		var transform = new TransformHandler(mediator, actions, select);

		var context = new EditorContext(mediator, scope) {
			Actions = actions,
			Cameras = cameras,
			Characters = chara,
			Interface = ui,
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
