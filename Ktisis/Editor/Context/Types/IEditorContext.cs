using System;

using Ktisis.Core.Types;
using Ktisis.Data.Config;
using Ktisis.Editor.Actions;
using Ktisis.Editor.Camera;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Posing.Types;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms;
using Ktisis.Interface.Editor.Types;
using Ktisis.Localization;
using Ktisis.Scene.Types;

namespace Ktisis.Editor.Context.Types;

public interface IEditorContext : IDisposable {
	public bool IsValid { get; }
	
	public IPluginContext Plugin { get; }
	
	public bool IsGPosing { get; }
	
	public Configuration Config { get; }
	public LocaleManager Locale { get; }
	
	public IActionManager Actions { get; }
	public ICharacterManager Characters { get; }
	public ICameraManager Cameras { get; }
	public IEditorInterface Interface { get; }
	public IPosingManager Posing { get; }
	public ISceneManager Scene { get; }
	public ISelectManager Selection { get; }
	public ITransformHandler Transform { get; }

	public void Initialize();
	public void Update();
}
