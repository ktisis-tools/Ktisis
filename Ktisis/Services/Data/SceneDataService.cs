using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;


using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;


namespace Ktisis.Services.Data;

[Singleton]
public class SceneDataService {
	
	IEditorContext? _ctx;
	IObjectTable _objectTable;
	private IFramework _framework;
	
	private ISceneManager Scene => this._ctx.Scene;
	private IPosingManager Posing => this._ctx.Posing;
	
	public SceneDataService(
		IEditorContext ctx,
		IObjectTable objectTable,
		IFramework framework
	) {
		this._ctx = ctx;
		this._objectTable = objectTable;
		this._framework = framework;
	}

	public unsafe bool Save(string path) {

		try {
			var scene = new SceneFile();
			var sceneOrigin = this._ctx.Scene.GetSceneOrigin();

			var entities = this.Scene.Children
				.Where(entity => entity is CharaEntity)
				.Cast<CharaEntity>()
				.ToList();

			var lights = this.Scene.Children
				.Where(entity => entity is LightEntity)
				.Cast<LightEntity>()
				.ToList();
			
			//var cameras = this._ctx.Cameras.GetCameras().ToList();
			
			foreach (var chara in entities) {
				
				var actor = ((ActorEntity)chara).Actor.GetDrawObject();

				var worldLocation = new Transform(actor->Position, actor->Rotation, actor->Scale);
	

				var relativeLocation = this.Scene.GetActorRelativePosition(worldLocation.Position);
				
				var charaFile = this._ctx.Characters.SaveCharaFile((ActorEntity)chara).GetResultSafely();
				var poseFile = new EntityPoseConverter(chara.Pose).SaveFile();
				
				scene.Actors.Add(new SceneFile.ActorInfo() {
					Chara = charaFile,
					Pose = poseFile,
					WorldRelative = worldLocation,
					RelativePosition = relativeLocation,
					MCDF = String.Empty
				});

			}

			foreach (var light in lights) {
				var lightFile = Scene.SaveLightFile(light).Result;
				var l = light.GetObject()->Transform;
				var worldLocation = new Transform(l.Position, l.Rotation, l.Scale);
				var relativeLocation = this.Scene.GetActorRelativePosition(worldLocation.Position);
				var lightObj = new SceneFile.LightInfo() {
					Light = lightFile,
					WorldRelative = worldLocation,
					RelativePosition = relativeLocation,
					Name = light.Name,
				};
				scene.Lights.Add(lightObj);
			}
			
			var serializer = new JsonFileSerializer();
			serializer.GetConverter<Vector3>();
			serializer.GetConverter<Transform>();
			File.WriteAllText(path, serializer.Serialize(scene));
		} catch (Exception a) {
			return false;
		}
		return true;
	}

	public async Task Load(String path, bool autoSaveLoading = false) {

		if (File.Exists(path) && Path.GetExtension(path) == ".ktscene") { //fix this later idc

			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);

			foreach (var e in this.Scene.Children.Where(entity => entity is CharaEntity).ToList()) {
				e.Remove();
			}



			foreach (var loaded in scene.Actors) {

				if (!autoSaveLoading) {
					loaded.Pose.Position = loaded.RelativePosition + this._objectTable.LocalPlayer.Position;
				} else {
					loaded.Pose.Position = loaded.WorldRelative.Position;
				}

				await this._framework.RunOnFrameworkThread(() => {
					this._ctx.Scene.Factory.CreateActor().Spawn().ContinueWith((p) => {
						SetupActor(loaded, p.Result);
					});
				});
				await this._framework.DelayTicks(10);
			}
			

			foreach (var loaded in scene.Lights) {
				var light = this._ctx.Scene.Factory.CreateLight().Spawn().Result;
				this._ctx.Scene.ApplyLightFile(light, loaded.Light);
				light.SetTransform(loaded.WorldRelative);
			}
			return;
		}
	}

	private async void SetupActor(SceneFile.ActorInfo loaded, ActorEntity actor) {
		this._ctx.Characters.ApplyCharaFile(actor, loaded.Chara);
		loaded.Pose.Rotation = loaded.WorldRelative.Rotation;
		await this._framework.DelayTicks(5);
		this._ctx.Posing.ApplyPoseFile(actor.Pose, loaded.Pose);
		actor.Name = loaded.Chara.Nickname;
	}
	


}