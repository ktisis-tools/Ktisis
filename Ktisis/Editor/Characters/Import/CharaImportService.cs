using System.IO;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Interface;
using Ktisis.Interface.Menus.Actors;
using Ktisis.Scene;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Services;

namespace Ktisis.Editor.Characters.Import;

// TODO: This needs some serious cleanup

[Singleton]
public class CharaImportService {
	private readonly ActorService _actors;
	private readonly GuiManager _gui;
	private readonly FileDialogManager _dialog;
	private readonly IFramework _framework;
	
	public CharaImportService(
		ActorService actors,
		GuiManager gui,
		FileDialogManager dialog,
		IFramework framework
	) {
		this._actors = actors;
		this._gui = gui;
		this._dialog = dialog;
		this._framework = framework;
	}
	
	// Interface
	// TODO: This service should become the factory for CharaFile imports/exports.
	// FileDialogManager should then only be responsible for creating the dialog and maintaining state.

	public void OpenSpawnImport(ISceneManager scene) {
		this._dialog.OpenCharaFile((path, file) => {
			if (path.IsNullOrEmpty()) return;
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			this.CreateFromCharaFile(scene, name, file);
		});
	}

	public void OpenOverworldActorsList(ISceneManager scene) {
		var popup = new OverworldActorList(this._actors);
		popup.Selected += actor => this.AddOverworldActor(scene, actor);
		this._gui.AddPopupSingleton(popup).Open();
	}
	
	// Handling

	public Task ApplyCharaFile(ActorEntity entity, CharaFile file, SaveModes mode = SaveModes.All) {
		var loader = new EntityCharaConverter(entity);
		return this._framework.RunOnFrameworkThread(() => {
			loader.Apply(file, mode);
		});
	}

	public void CreateFromCharaFile(ISceneManager scene, string name, CharaFile file) {
		scene.GetModule<ActorModule>()
			.Spawn(name)
			.ContinueWith(async task => {
				var entity = task.Result;
				await this.ApplyCharaFile(entity, file);
				scene.Context.Characters.ApplyStateToGameObject(entity);
			}, TaskContinuationOptions.OnlyOnRanToCompletion)
			.ContinueWith(task => {
				if (task.Exception != null)
					Ktisis.Log.Error($"Failed to spawn imported actor:\n{task.Exception}");
			}, TaskContinuationOptions.OnlyOnFaulted);
	}

	public void AddOverworldActor(ISceneManager scene, GameObject actor) {
		scene.GetModule<ActorModule>()
			.AddFromOverworld(actor)
			.ConfigureAwait(false);
	}
}
