using System.IO;

using Dalamud.Interface;
using Dalamud.Utility;

using GLib.Popups.Context;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Interface.Menus.Actors;
using Ktisis.Scene;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Types;
using Ktisis.Services;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class SceneTreeButtons {
	private readonly GuiManager _gui;
	private readonly FileDialogManager _dialog;
	private readonly ActorService _actors;
	
	public SceneTreeButtons(
		GuiManager gui,
		FileDialogManager dialog,
		ActorService actors
	) {
		this._gui = gui;
		this._dialog = dialog;
		this._actors = actors;
	}
	
	public void Draw(ISceneManager scene) {
		if (Buttons.IconButton(FontAwesomeIcon.Plus))
			this.OpenCreateMenu(scene.Factory);
	}

	private void OpenCreateMenu(IEntityFactory factory) {
		var menu = this.BuildCreateMenu(factory);
		this._gui.AddPopup(menu).Open();
	}
	
	// Scene creation menu

	private ContextMenu BuildCreateMenu(IEntityFactory factory) {
		return new ContextMenuBuilder()
			.Group(builder => this.BuildActorMenu(factory, builder))
			.Separator()
			.SubMenu("Create light", builder => this.BuildLightMenu(factory, builder))
			.Build("##SceneObjectContext");
	}
	
	// Actor options

	private void BuildActorMenu(IEntityFactory factory, ContextMenuBuilder sub) {
		sub.Action("Create new actor", () => factory.CreateActor().Spawn())
			.Action("Add overworld actor", () => this.AddOverworldActor(factory))
			.Action("Import actor from file", () => this.ImportCharaFromFile(factory));
	}

	private void AddOverworldActor(IEntityFactory factory) {
		var popup = new OverworldActorList(this._actors, actor => {
			factory.CreateActor()
				.FromOverworld(actor)
				.Spawn();
		});
		
		this._gui.AddPopupSingleton(popup).Open();
	}

	private void ImportCharaFromFile(IEntityFactory factory) {
		this._dialog.OpenCharaFile((path, file) => {
			if (path.IsNullOrEmpty()) return;
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			factory.CreateActor()
				.WithAppearance(file)
				.SetName(name)
				.Spawn();
		});
	}
	
	// Light options

	private void BuildLightMenu(IEntityFactory factory, ContextMenuBuilder sub) {
		sub.Action("Point", () => factory.CreateLight().Spawn())
			.Action("Spot", () => SpawnLight(LightType.SpotLight))
			.Action("Area", () => SpawnLight(LightType.AreaLight))
			.Action("Sun", () => SpawnLight(LightType.Directional));
		
		void SpawnLight(LightType type) => factory.CreateLight(type).Spawn();
	}
}
