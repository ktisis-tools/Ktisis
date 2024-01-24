using System;
using System.Linq;

using ImGuiNET;

using Dalamud.Game.ClientState.Objects.Types;

using GLib.Popups;

using Ktisis.Common.Extensions;
using Ktisis.Services;

namespace Ktisis.Interface.Menus.Actors;

public delegate void ActorSelectedHandler(GameObject actor);

public class OverworldActorList : IPopup {
	private readonly ActorService _actors;
	private readonly PopupList<GameObject> _popup;

	private event ActorSelectedHandler Selected;

	public OverworldActorList(
		ActorService actors,
		ActorSelectedHandler handler
	) {
		this._actors = actors;
		this._popup = new PopupList<GameObject>(
			"##OverworldActorList",
			DrawItem
		).WithSearch(SearchItemPredicate);
		
		this.Selected = handler;
	}
	
	// Popup state

	private bool _isOpening;
	
	public void Open() {
		this._isOpening = true;
	}

	public bool IsOpen => this._isOpening || this._popup.IsOpen;

	public bool Draw() {
		if (this._isOpening) {
			this._isOpening = false;
			this._popup.Open();
			return true;
		}

		if (!this._popup.IsOpen)
			return false;

		var actors = this._actors.GetOverworldActors().ToList();
		if (this._popup.Draw(actors, out var selected) && selected?.IsEnabled() == true)
			this.Selected.Invoke(selected);

		return true;
	}
	
	// Predicates

	private static bool DrawItem(GameObject actor, bool isFocus)
		=> ImGui.Selectable(actor.GetNameOrFallback(), isFocus);

	private static bool SearchItemPredicate(GameObject actor, string query)
		=> actor.GetNameOrFallback().Contains(query, StringComparison.OrdinalIgnoreCase);
}
