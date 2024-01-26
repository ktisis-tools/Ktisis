using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using ImGuiNET;

using GLib.Lists;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Editor.Popup;

public class OverworldActorPopup : KtisisPopup {
	private readonly ActorService _actors;
	private readonly IEditorContext _ctx;
	private readonly ListBox<GameObject> _list;

	public OverworldActorPopup(
		ActorService actors,
		IEditorContext ctx
	) : base("##OverworldActorPopup") {
		this._actors = actors;
		this._ctx = ctx;
		this._list = new ListBox<GameObject>(
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
	
	private async void AddActor(GameObject actor) {
		var module = this._ctx.Scene.GetModule<ActorModule>();
		await module.AddFromOverworld(actor);
	}
	
	private static bool DrawActorName(GameObject actor, bool isFocus)
		=> ImGui.Selectable(actor.GetNameOrFallback(), isFocus);
}
