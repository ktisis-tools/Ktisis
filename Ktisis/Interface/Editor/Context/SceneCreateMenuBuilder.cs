using System.IO;

using GLib.Popups.Context;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Factory.Types;
using Ktisis.Structs.Lights;

namespace Ktisis.Interface.Editor.Context;

public class SceneCreateMenuBuilder {
	private readonly IEditorContext _ctx;

	private IEntityFactory Factory => this._ctx.Scene.Factory;

	public SceneCreateMenuBuilder(
		IEditorContext ctx
	) {
		this._ctx = ctx;
	}

	public ContextMenu Create() {
		return new ContextMenuBuilder()
			.Group(this.BuildActorGroup)
			.Separator()
			.Group(this.BuildLightGroup)
			.Separator()
			.Group(this.BuildUtilityGroup)
			.Build($"##SceneCreateMenu_{this.GetHashCode():X}");
	}

	private void BuildActorGroup(ContextMenuBuilder sub) {
		sub.Action("Create new actor", () => this.Factory.CreateActor().Spawn())
			.Action("Import actor from file", this.ImportCharaFromFile)
			.Action("Import actor from Mcdf", this.ImportCharaFromMcdf)
			.Action("Add overworld actor", this._ctx.Interface.OpenOverworldActorList);
	}
	
	private void BuildLightGroup(ContextMenuBuilder sub)
		=> sub.SubMenu("Create new light", this.BuildLightMenu);
	
	private void BuildLightMenu(ContextMenuBuilder sub) {
		sub.Action("Point", () => SpawnLight(LightType.PointLight))
			.Action("Spot", () => SpawnLight(LightType.SpotLight))
			.Action("Area", () => SpawnLight(LightType.AreaLight))
			.Action("Sun (Directional)", () => SpawnLight(LightType.Directional))
			.Action("From file... (.ktlight)", () => this.ImportLightFromFile());
		
		void SpawnLight(LightType type) => this.Factory.CreateLight(type).Spawn();
	}

	private async void ImportLightFromFile() {
		this._ctx.Interface.OpenLightFile(async (path, file) => {
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			var newLight = await this.Factory.CreateLight().Spawn();
			await this._ctx.Scene.ApplyLightFile(newLight, file);
		});
	}

	private void BuildUtilityGroup(ContextMenuBuilder sub) {
		sub.Action("Add reference image", this.OpenReferenceImage);
	}
	
	// Actor handling

	private void ImportCharaFromFile() {
		this._ctx.Interface.OpenCharaFile((path, file) => {
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			this.Factory.CreateActor()
				.WithAppearance(file)
				.SetName(name)
				.Spawn();
		});
	}
	
	private void ImportCharaFromMcdf() {
		this._ctx.Interface.OpenMcdfFile((path) => {
			var name = Path.GetFileNameWithoutExtension(path).Truncate(32);
			this.Factory.CreateActor()
				.WithMcdf(path)
				.SetName(name)
				.Spawn();
		});
	}
	
	// Reference image loading

	private void OpenReferenceImage() {
		this._ctx.Interface.OpenReferenceImages(path => {
			this.Factory.BuildRefImage()
				.SetPath(path)
				.Add()
				.Save();
		});
	}
}
