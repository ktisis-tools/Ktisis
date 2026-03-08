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
using Ktisis.Editor.Camera.Types;
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
			scene.SceneOrigin = this._ctx.Scene.GetSceneOrigin();

			var entities = this.Scene.Children
				.Where(entity => entity is CharaEntity)
				.Cast<CharaEntity>()
				.ToList();

			var lights = this.Scene.Children
				.Where(entity => entity is LightEntity)
				.Cast<LightEntity>()
				.ToList();
			

			//TODO: Add MCDF logic
			foreach (var chara in entities) {
				
				var actor = ((ActorEntity)chara).Actor.GetDrawObject();

				var location = new Transform(this.Scene.GetActorRelativePosition(actor->Position), actor->Rotation, actor->Scale);
				var charaFile = this._ctx.Characters.SaveCharaFile((ActorEntity)chara).GetResultSafely();
				var poseFile = new EntityPoseConverter(chara.Pose).SaveFile();
				
				scene.Actors.Add(new SceneFile.ActorInfo() {
					Chara = charaFile,
					Pose = poseFile,
					Location = location,
					MCDF = String.Empty
				});

			}

			foreach (var light in lights) {
				var lightFile = Scene.SaveLightFile(light).Result;
				var l = light.GetObject()->Transform;
				var location = new Transform(this.Scene.GetActorRelativePosition(l.Position), l.Rotation, l.Scale);
				var lightObj = new SceneFile.LightInfo() {
					Light = lightFile,
					Location = location,
					Name = light.Name,
				};
				scene.Lights.Add(lightObj);
			}
			
			//TODO: Setup actor orbit
			foreach (var camera in this._ctx.Cameras.GetCameras()) {
				var c = new SceneFile.CameraInfo();
				c.FixedPosition = camera.GetPosition();
				c.Flags = (uint)camera.Flags;
				c.IsActive = (this._ctx.Cameras.Current ==  camera);
				c.Name = camera.Name;
				c.OrthographicZoom = camera.OrthographicZoom;
				scene.Cameras.Add(c);
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

	public async Task Load(String path, bool autoSaveLoading = true) {


		if (File.Exists(path) && Path.GetExtension(path) == ".ktscene") { //fix this later idc

			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);
			
			//TODO: Fix localplayer entity not being deleted?
			foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is CharaEntity).ToList()) {
				var e = (ActorEntity)sceneEntity;
				e.Delete();
			}
			Vector3 sceneOrigin;
			if (!autoSaveLoading) {
				sceneOrigin = this._objectTable.LocalPlayer.Position;
			} else {
				sceneOrigin = scene.SceneOrigin;
			}


			foreach (var loaded in scene.Actors) {
				loaded.Location.Position += sceneOrigin;
				await this._framework.RunOnFrameworkThread(() => {
					this._ctx.Scene.Factory.CreateActor().WithAppearance(loaded.Chara).Spawn().ContinueWith(async (p) => {
						var a = p.GetResultSafely();
						await this._framework.DelayTicks(15);
						a.Name = loaded.Chara.Nickname!;
						SetupActor(loaded, a);
						await this._ctx?.Posing.ApplyPoseFile(a.Pose!, loaded.Pose, PoseMode.All,(PoseTransforms)0xF)!;
					});
				});
				await this._framework.DelayTicks(10);
			}

			foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is LightEntity).ToList()) {
				LightEntity lightEntity = (LightEntity)sceneEntity;
				lightEntity.Delete();
			}

			foreach (var loaded in scene.Lights) {
				var light = this._ctx.Scene.Factory.CreateLight().Spawn().Result;
				this._ctx.Scene.ApplyLightFile(light, loaded.Light);
				loaded.Location.Position += sceneOrigin;
				light.SetTransform(loaded.Location);
			}

			
			//always at least one camera (I hope....)

			var primaryCamera = this._ctx.Cameras.Current;
			primaryCamera.ResetState();
			var primaryInfo = scene.Cameras.Find(c => c.IsActive);
			primaryCamera.FixedPosition = primaryInfo.FixedPosition;
			primaryCamera.OrthographicZoom = primaryInfo.OrthographicZoom;
			primaryCamera.Flags = (CameraFlags)primaryInfo.Flags;
			
		}
	}

	private unsafe void SetupActor(SceneFile.ActorInfo loaded, ActorEntity actor) {
		//this._ctx.Characters.ApplyCharaFile(actor, loaded.Chara);
		var draw = actor.GetCharacter();
		draw->Position = loaded.Location.Position;
		draw->Rotation = loaded.Location.Rotation;
		draw->Scale = loaded.Location.Scale;
	}
	
}