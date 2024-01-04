using Ktisis.Actions;
using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interop;
using Ktisis.Interop.Hooking;
using Ktisis.Scene;
using Ktisis.Scene.Modules;

namespace Ktisis.Editor.Context;

public interface IContextFactory {
	public IContextBuilder Build(IContextMediator mediator);
}

public interface IContextBuilder {
	public IContextBuilder WithModule<T>() where T : SceneModule;
	public IEditorContext Initialize();
}

[Singleton]
public class ContextFactory : IContextFactory {
	private readonly InteropService _interop;
	
	public ContextFactory(
		DIBuilder di,
		InteropService interop
	) {
		this._interop = interop;
	}

	public IContextBuilder Build(IContextMediator mediator) {
		var scope = this._interop.CreateScope();
		var scene = new SceneManager(mediator, scope);
		var actions = new ActionManager();
		return new ContextBuilder(mediator, scope, scene, actions);
	}

	private class ContextBuilder : IContextBuilder {
		private readonly IContextMediator _mediator;
		private readonly HookScope _scope;
		private readonly SceneManager _scene;
		private readonly ActionManager _actions;
		
		public ContextBuilder(
			IContextMediator mediator,
			HookScope scope,
			SceneManager scene,
			ActionManager actions
		) {
			this._mediator = mediator;
			this._scope = scope;
			this._scene = scene;
			this._actions = actions;
		}

		public IContextBuilder WithModule<T>() where T : SceneModule {
			this._scene.AddModule<T>();
			return this;
		}

		public IEditorContext Initialize() {
			var context = this.Create();
			try {
				this._mediator.Initialize(context);
			} catch {
				context.Dispose();
				throw;
			}
			return context;
		}

		private IEditorContext Create() {
			var select = this.CreateSelection();
			var transform = this.CreateTransform(select);
			return new EditorContext(
				this._mediator,
				this._scope,
				this._actions,
				this._scene,
				select,
				transform
			);
		}

		private ISelectManager CreateSelection()
			=> new SelectManager(this._mediator);

		private ITransformHandler CreateTransform(ISelectManager select)
			=> new TransformHandler(this._actions, select);
	}
}
