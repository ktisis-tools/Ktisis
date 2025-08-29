using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Dalamud.Bindings.ImGui;

using GLib.Lists;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Editor.Popup;

public class OverworldActorPopup : KtisisPopup {
	private readonly ActorService _actors;
	private readonly IEditorContext _ctx;
	private readonly ListBox<IGameObject> _list;

	public OverworldActorPopup(
		ActorService actors,
		IEditorContext ctx
	) : base("##OverworldActorPopup") {
		this._actors = actors;
		this._ctx = ctx;
		this._list = new ListBox<IGameObject>(
			"##OverworldActorList",
			DrawActorName
		);
	}

	protected override void OnDraw() {
		if (!this._ctx.IsValid) {
			this.Close();
			return;
		}
		
		var actors = this._actors.GetOverworldActors().ToList();
		if (this._list.Draw(actors, out var selected) && selected!.IsEnabled())
			this.AddActor(selected!);
	}
	
	private async void AddActor(IGameObject actor) {
		var module = this._ctx.Scene.GetModule<ActorModule>();
		await module.AddFromOverworld(actor);
	}
	
	// TODO: runs every frame the popup is open
	private bool DrawActorName(IGameObject actor, bool isFocus)
		=> ImGui.Selectable(actor.GetNameOrFallback(this._ctx.Config.Editor.IncognitoPlayerNames), isFocus);
}
