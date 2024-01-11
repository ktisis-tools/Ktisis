using Dalamud.Interface;

using GLib.Popups.Context;
using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Scene;
using Ktisis.Scene.Modules.Actors;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class SceneTreeButtons {
	private readonly GuiManager _gui;
	
	public SceneTreeButtons(
		GuiManager gui
	) {
		this._gui = gui;
	}
	
	public void Draw(ISceneManager scene) {
		if (Buttons.IconButton(FontAwesomeIcon.Plus))
			this.OpenContextMenu(scene);
	}

	private void OpenContextMenu(ISceneManager scene) {
		var popup = new ContextMenuBuilder()
			.Action("Create Actor", () => this.CreateActor(scene))
			.Build("##SceneObjectContext");
		
		this._gui.AddPopup(popup).Open();
	}
	
	// Handlers

	private void CreateActor(ISceneManager scene) {
		scene.GetModule<ActorModule>()
			.Spawn()
			.ConfigureAwait(false);
	}
}
