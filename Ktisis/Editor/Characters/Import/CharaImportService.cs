using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Interface;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Import;

[Singleton]
public class CharaImportService {
	private readonly FileDialogManager _dialog;
	private readonly IFramework _framework;
	
	public CharaImportService(
		FileDialogManager dialog,
		IFramework framework
	) {
		this._dialog = dialog;
		this._framework = framework;
	}
	
	// Interface
	// TODO: This service should become the factory for CharaFile imports/exports.
	// FileDialogManager should then only be responsible for creating the dialog and maintaining state.

	public void OpenCharaFile(CharaFileOpenedHandler handler)
		=> this._dialog.OpenCharaFile(handler);
	
	// Handling

	public Task ApplyCharaFile(ActorEntity entity, CharaFile file, SaveModes mode = SaveModes.All) {
		var loader = new EntityCharaConverter(entity);
		return this._framework.RunOnFrameworkThread(() => {
			loader.Apply(file, mode);
		});
	}
}
