using System;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Dalamud.Bindings.ImGui;

using GLib.Popups;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Editor.Popup;

public class OverworldActorPopup : KtisisPopup {
	private readonly ActorService _actors;
	private readonly IEditorContext _ctx;
	private readonly PopupList<IGameObject> _list;

	public OverworldActorPopup(
		ActorService actors,
		IEditorContext ctx
	) : base("##OverworldActorPopup") {
		this._actors = actors;
		this._ctx = ctx;
		this._list = new PopupList<IGameObject>(
			"##OverworldActorList",
			DrawActorName
		).WithSearch(MatchQuery);
	}

	protected override void OnDraw() {
		if (!this._ctx.IsValid) {
			this.Close();
			return;
		}
		if (!this._list.IsOpen)
			this._list.Open();
		
		var actors = this._actors.GetOverworldActors().OrderBy(a => a.CurrentDistance).ToList();
		if (this._list.Draw(actors, out var selected) && selected!.IsEnabled())
			this.AddActor(selected!);
	}
	
	private async void AddActor(IGameObject actor) {
		var module = this._ctx.Scene.GetModule<ActorModule>();
		await module.AddFromOverworld(actor);
	}
	
	private bool DrawActorName(IGameObject actor, bool isFocus)
		=> ImGui.Selectable(actor.GetNameOrFallback(this._ctx) + (actor.CurrentDistance > 0 ? $" ({actor.CurrentDistance:#y})" : ""), isFocus);

	private static bool MatchQuery(IGameObject actor, string query)
		=> actor.Name.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);
}
