using System;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Dalamud.Bindings.ImGui;
using Dalamud.Utility.Numerics;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using GLib.Popups;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

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

	private unsafe bool DrawActorName(IGameObject actor, bool isFocus) {
		var style = ImGui.GetStyle();
		var fontSize = ImGui.GetFontSize();

		var result = ImGui.Selectable("##", isFocus, 0, ImGui.GetContentRegionAvail().WithY(fontSize));
		if (ImGui.IsItemHovered()) {
			var csPtr = (CSGameObject*)actor.Address;
			if (csPtr != null && csPtr->DrawObject != null && actor.GetDrawObject()->OutlineColor != ObjectHighlightColor.Yellow)
				csPtr->Highlight(ObjectHighlightColor.Yellow);
		} else {
			var csPtr = (CSGameObject*)actor.Address;
			if (csPtr != null && csPtr->DrawObject != null && actor.GetDrawObject()->OutlineColor != ObjectHighlightColor.None)
				csPtr->Highlight(ObjectHighlightColor.None);
		}

		ImGui.SameLine(style.ItemInnerSpacing.X, 0);
		ImGui.Text(actor.GetNameOrFallback(this._ctx));
		if (actor.CurrentDistance > 0) {
			ImGui.SameLine(0, style.ItemInnerSpacing.X);
			ImGui.Text($"({actor.CurrentDistance:#y})");
		}

		return result;
	}

	private static bool MatchQuery(IGameObject actor, string query)
		=> actor.Name.ToString().Contains(query, StringComparison.OrdinalIgnoreCase);
}
