using System.IO;

using GLib.Popups.Context;

using Ktisis.Common.Extensions;
using Ktisis.Editor.Context.Types;
using Ktisis.Scene.Factory.Builders;
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
		sub.Action(Ktisis.Locale.Translate("workspace.create_menu.actor.create"), () => this.Factory.CreateActor().Spawn())
			.Action(Ktisis.Locale.Translate("workspace.create_menu.actor.file"), this.ImportCharaFromFile)
			.Action(Ktisis.Locale.Translate("workspace.create_menu.actor.mcdf"), this.ImportCharaFromMcdf)
			.Action(Ktisis.Locale.Translate("workspace.create_menu.actor.overworld"), this._ctx.Interface.OpenOverworldActorList);
	}
	
	private void BuildLightGroup(ContextMenuBuilder sub)
		=> sub.SubMenu(Ktisis.Locale.Translate("workspace.create_menu.light.create"), this.BuildLightMenu);
	
	private void BuildLightMenu(ContextMenuBuilder sub) {
		sub.Action(Ktisis.Locale.Translate("workspace.create_menu.light.point"), () => SpawnLight(LightType.PointLight))
			.Action(Ktisis.Locale.Translate("workspace.create_menu.light.spot"), () => SpawnLight(LightType.SpotLight))
			.Action(Ktisis.Locale.Translate("workspace.create_menu.light.area"), () => SpawnLight(LightType.AreaLight))
			.Action(Ktisis.Locale.Translate("workspace.create_menu.light.directional"), () => SpawnLight(LightType.Directional))
			.Action(Ktisis.Locale.Translate("workspace.create_menu.light.file"), () => this.ImportLightFromFile());
		
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
		sub.SubMenu("Add new overlay...", this.BuildOverlayGroup);
		sub.Action(Ktisis.Locale.Translate("workspace.create_menu.reference"), this.OpenReferenceImage);
	}

	private void BuildOverlayGroup(ContextMenuBuilder sub) {
		sub.Action("Dialog", () => this.Factory.BuildOverlay(OverlayTypes.Talk).Add())
			.Action("Balloon", () => this.Factory.BuildOverlay(OverlayTypes.Balloon).Add())
			.Action("Status", () => this.Factory.BuildOverlay(OverlayTypes.Status).Add());
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
