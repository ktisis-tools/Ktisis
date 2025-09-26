using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Dalamud.Bindings.ImGui;

using GLib.Lists;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Services.Game;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Editor.Popup;

public class ActorGazeTargetPopup : KtisisPopup {
	private readonly IEditorContext _ctx;
	private readonly ListBox<ActorEntity> _list;
    private ActorEntity ForActor;

	public ActorGazeTargetPopup(
		IEditorContext ctx,
        ActorEntity actor
	) : base("##ActorGazeTargetPopup") {
		this._ctx = ctx;
        this.ForActor = actor;
		this._list = new ListBox<ActorEntity>(
			"##ActorGazeTargetList",
			DrawActorName
		);
	}

	protected override void OnDraw() {
		if (!this._ctx.IsValid) {
			this.Close();
			return;
		}

        var currentActors = this._ctx.Scene.Children
            .OfType<ActorEntity>()
            .Where(actor => actor != ForActor)
            .ToList();
		if (this._list.Draw(currentActors, out var selected) && selected!.Actor.IsEnabled()) {
			ForActor.SetActorGazeTarget(selected);
            this.Close();
        }
	}


	private bool DrawActorName(ActorEntity actor, bool isFocus)
		=> ImGui.Selectable(actor.Name, isFocus);
}
