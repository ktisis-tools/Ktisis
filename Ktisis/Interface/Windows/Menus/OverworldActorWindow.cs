using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using GLib.Lists;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context;
using Ktisis.Interface.Types;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Services;

namespace Ktisis.Interface.Windows.Menus;

public class OverworldActorWindow : KtisisWindow {
	private readonly IEditorContext _context;
	private readonly ActorService _actors;

	private readonly ListBox<GameObject> _list;
	
	public OverworldActorWindow(
		IEditorContext context,
		ActorService actors
	) : base(
		"Add overworld actor",
		ImGuiWindowFlags.AlwaysAutoResize
	) {
		this._context = context;
		this._actors = actors;
		this._list = new ListBox<GameObject>(
			"##OverworldActorList",
			DrawActorName
		);
	}

	public override void OnOpen() {
		this.Position = ImGui.GetMousePos();
		this.PositionCondition = ImGuiCond.Appearing;
	}

	public override void PreOpenCheck() {
		if (this._context.IsValid) return;
		this.Close();
	}
	
	public override void Draw() {
		var actors = this._actors.GetOverworldActors().ToList();
		if (this._list.Draw(actors, out var selected) && selected!.IsEnabled())
			this.AddActor(selected!);
	}

	private async void AddActor(GameObject actor) {
		var module = this._context.Scene.GetModule<ActorModule>();
		await module.AddFromOverworld(actor);
	}

	private static bool DrawActorName(GameObject actor, bool isFocus)
		=> ImGui.Selectable(actor.GetNameOrFallback(), isFocus);
}
