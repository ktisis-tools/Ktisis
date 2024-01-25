using System.IO;

using GLib.Popups.Context;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context;
using Ktisis.Interface.Editor.Types;
using Ktisis.Scene.Factory.Types;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Editor.Context;

public class SceneCreateMenuBuilder {
	private readonly IEditorInterface _editor;
	private readonly IEditorContext _context;

	private IEntityFactory Factory => this._context.Scene.Factory;

	public SceneCreateMenuBuilder(
		IEditorInterface editor,
		IEditorContext context
	) {
		this._editor = editor;
		this._context = context;
	}

	public ContextMenu Create() {
		return new ContextMenuBuilder()
			.Group(this.BuildActorGroup)
			.Separator()
			.Group(this.BuildLightGroup)
			.Build($"##SceneCreateMenu_{this.GetHashCode():X}");
	}

	private void BuildActorGroup(ContextMenuBuilder sub) {
		sub.Action("Create new actor", () => this.Factory.CreateActor().Spawn())
			.Action("Import actor from file", this.ImportCharaFromFile)
			.Action("Add overworld actor", this._editor.OpenOverworldActorList);
	}
	
	private void BuildLightGroup(ContextMenuBuilder sub)
		=> sub.SubMenu("Create new light", this.BuildLightMenu);
	
	private void BuildLightMenu(ContextMenuBuilder sub) {
		sub.Action("Point", () => SpawnLight(LightType.PointLight))
			.Action("Spot", () => SpawnLight(LightType.SpotLight))
			.Action("Area", () => SpawnLight(LightType.AreaLight))
			.Action("Sun", () => SpawnLight(LightType.Directional));
		
		void SpawnLight(LightType type) => this.Factory.CreateLight(type).Spawn();
	}
	
	// Actor handling

	private void ImportCharaFromFile() {
		this._editor.OpenCharaFile((path, file) => {
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			this.Factory.CreateActor()
				.WithAppearance(file)
				.SetName(name)
				.Spawn();
		});
	}
}
