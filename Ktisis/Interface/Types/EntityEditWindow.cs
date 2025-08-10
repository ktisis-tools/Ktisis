using System;
using System.Linq;

using Dalamud.Bindings.ImGui;

using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Entities;

namespace Ktisis.Interface.Types;

public abstract class EntityEditWindow<T> : KtisisWindow where T : SceneEntity {
	protected readonly IEditorContext Context;

	private T? _target;
	protected T Target {
		get => this._target!;
		private set => this._target = value;
	}

	protected EntityEditWindow(
		string name,
		IEditorContext ctx,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None
	) : base(name, flags) {
		this.Context = ctx;
	}
	
	public override void PreDraw() {
		if (this.Context.IsValid && this._target is { IsValid: true }) return;
		Ktisis.Log.Verbose($"State for {this.GetType().Name} is stale, closing...");
		this.Close();
	}

	public virtual void SetTarget(T target) {
		if (!target.IsValid)
			throw new Exception("Attempted to set invalid target.");
		this.Target = target;
	}

	protected void UpdateTarget() {
		if (this.Context.Config.Editor.UseLegacyWindowBehavior) return;

		var target = (T?)this.Context.Selection.GetSelected()
			.FirstOrDefault(entity => entity is T);

		if (target != null && this.Target != target)
			this.SetTarget(target);
	}
}
