using Dalamud.Interface;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Characters.Import;
using Ktisis.Interface.Menus;
using Ktisis.Scene;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class SceneTreeButtons {
	private readonly CharaImportService _chara;
	private readonly GuiManager _gui;
	
	public SceneTreeButtons(
		CharaImportService chara,
		GuiManager gui
	) {
		this._chara = chara;
		this._gui = gui;
	}
	
	public void Draw(ISceneManager scene) {
		if (Buttons.IconButton(FontAwesomeIcon.Plus)) {
			var menu = SceneCreateMenu.Build(this._chara, scene);
			this._gui.AddPopup(menu).Open();
		}
	}
}
