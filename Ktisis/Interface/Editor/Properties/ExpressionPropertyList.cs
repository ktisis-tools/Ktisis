using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Posing;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Editor.Properties;

public class ExpressionPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly ExpressionEditorPanel _panel;

	public ExpressionPropertyList(
		IEditorContext ctx,
		ExpressionEditorPanel panel
	) {
		this._ctx = ctx;
		this._panel = panel;
	}

	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (ResolveActor(entity) is not { } actor) return;
		if (actor.Pose is not { IsValid: true }) return;

		builder.AddHeader("Expressions", () => this.Draw(actor), priority: 2);
	}

	private void Draw(ActorEntity actor) {
		var isEnabled = !this._ctx.Posing.IsEnabled;
		
		if (isEnabled) {
			ImGui.TextDisabled("Enable posing to edit facial expressions.");
		}
		
		using var _ = ImRaii.Disabled(isEnabled);

		//TODO: Better UI
		this._panel.Draw(this._ctx.Expressions.GetEditor(actor));
	}

	private static ActorEntity? ResolveActor(SceneEntity entity) => entity switch {
		BoneNode node => node.Pose.Parent as ActorEntity,
		BoneNodeGroup group => group.Pose.Parent as ActorEntity,
		EntityPose pose => pose.Parent as ActorEntity,
		ActorEntity actor => actor,
		_ => null
	};
}
