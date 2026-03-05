using System;
using System.IO;
using System.Linq;
using System.Numerics;

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
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;


namespace Ktisis.Services.Data;

[Singleton]
public class SceneDataService {
	
	IEditorContext? _ctx;
	IObjectTable _objectTable;
	
	private ISceneManager Scene => this._ctx.Scene;
	private IPosingManager Posing => this._ctx.Posing;
	
	public SceneDataService(
		IEditorContext ctx,
		IObjectTable objectTable
	) {
		this._ctx = ctx;
		this._objectTable = objectTable;
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
			File.WriteAllText(path, serializer.Serialize(scene));
		} catch (Exception a) {
			return false;
		}
		return true;
	}

	public unsafe bool Load(String path, bool autoSaveLoading = false) {

		if (File.Exists(path) && Path.GetExtension(path) == ".ktscene") {
			
			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);
			
			
			foreach (var loaded in scene.Actors) {
				var actor = (ActorEntity?)this.Scene.Children
					.FirstOrDefault(entity => entity is CharaEntity && entity.Name == loaded.Chara.Nickname);
				
				if (actor == null) {
					var actorCtr = this._ctx.Scene.Factory.CreateActor();
					actorCtr.WithAppearance(loaded.Chara);
					actorCtr.SetName(loaded.Chara.Nickname);
					actor = actorCtr.Spawn().Result;
				} else {
					this._ctx.Characters.ApplyCharaFile(actor, loaded.Chara);
				}


				var actorDraw = actor.Actor.GetDrawObject();
				if (autoSaveLoading) {
					actorDraw->Position = loaded.WorldRelative.Position;
				} else {
					actorDraw->Position = loaded.RelativePosition + this._objectTable.LocalPlayer.Position;
				}

				actorDraw->Rotation = loaded.WorldRelative.Rotation;
				actorDraw->Scale = loaded.WorldRelative.Scale;

				this._ctx.Posing.ApplyPoseFile(actor.Pose, loaded.Pose);
			}

			foreach (var loaded in scene.Lights) {
				var light = this._ctx.Scene.Factory.CreateLight().Spawn().Result;
				this._ctx.Scene.ApplyLightFile(light, loaded.Light);
				light.SetTransform(loaded.WorldRelative);
			}
			return true;
		}
		return false;
	}
	
}
