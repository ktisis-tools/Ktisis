using Dalamud.Interface;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Interface.Menus;
using Ktisis.Scene;

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
			this._gui.AddPopup(SceneCreateMenu.Build(scene)).Open();
	}
}
