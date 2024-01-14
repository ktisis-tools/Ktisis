using System;

using ImGuiNET;

using Ktisis.Editor;
using Ktisis.Editor.Context;
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
		IEditorContext context,
		ImGuiWindowFlags flags = ImGuiWindowFlags.None
	) : base(name, flags) {
		this.Context = context;
	}

	public virtual void SetTarget(T target) {
		if (!target.IsValid)
			throw new Exception("Attempted to set invalid target.");
		this.Target = target;
	}
	
	public override void PreDraw() {
		if (this.Context.IsValid && this._target is { IsValid: true }) return;
		Ktisis.Log.Verbose($"State for {this.GetType().Name} is stale, closing...");
		this.Close();
	}
}
