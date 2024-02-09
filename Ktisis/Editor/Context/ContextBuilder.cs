using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.Core.Types;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Actions.Input;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Posing.Attachment;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interface.Editor;
using Ktisis.Interop;
using Ktisis.Scene;
using Ktisis.Scene.Factory;
using Ktisis.Services.Data;
using Ktisis.Services.Game;

namespace Ktisis.Editor.Context;

[Singleton]
public class ContextBuilder {
	private readonly GPoseService _gpose;
	private readonly InteropService _interop;
	private readonly IFramework _framework;
	private readonly IKeyState _keyState;
	private readonly NamingService _naming;
	
	public ContextBuilder(
		GPoseService gpose,
		InteropService interop,
		IFramework framework,
		IKeyState keyState,
		NamingService naming
	) {
		this._gpose = gpose;
		this._interop = interop;
		this._framework = framework;
		this._keyState = keyState;
		this._naming = naming;
	}

	public IEditorContext Create(
		IPluginContext state
	) {
		var context = new EditorContext(this._gpose, state);
		
		var scope = this._interop.CreateScope();

		var input = new InputManager(context, scope, this._keyState);
		var actions = new ActionManager(context, input);
		var factory = new EntityFactory(context, this._naming);
		var select = new SelectManager(context);
		var attach = new AttachManager();

		var editor = new EditorState(context, scope) {
			Actions = actions,
			Cameras = new CameraManager(context, scope),
			Characters = new CharacterManager(context, scope, this._framework),
			Interface = new EditorInterface(context, state.Gui),
			Posing = new PosingManager(context, scope, this._framework, attach),
			Scene = new SceneManager(context, scope, factory),
			Selection = select,
			Transform = new TransformHandler(context, actions, select)
		};
		
		context.Setup(editor);
		return context;
	}
}