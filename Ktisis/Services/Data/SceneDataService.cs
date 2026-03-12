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
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;
using Ktisis.Structs.Env;


namespace Ktisis.Services.Data;

[Singleton]
public class SceneDataService {
	
	IEditorContext? _ctx;
	IObjectTable _objectTable;
	private IFramework _framework;
	private Task? _task;
	private Dictionary<ushort, ActorEntity> _idMap;
	
	private ISceneManager Scene => this._ctx!.Scene;
	private IPosingManager Posing => this._ctx!.Posing;

	
	public SceneDataService(
		IEditorContext ctx,
		IObjectTable objectTable,
		IFramework framework
	) {
		this._ctx = ctx;
		this._objectTable = objectTable;
		this._framework = framework;
	}

	public unsafe bool WriteFile(string path) {
		try {
			var file = this.Save();
			var serializer = new JsonFileSerializer();
			serializer.GetConverter<Vector3>();
			serializer.GetConverter<Transform>();
			File.WriteAllText(path, serializer.Serialize(file));
			return true;
		} catch {
			return false;
		}
	}
	public unsafe SceneFile Save() {


			var scene = new SceneFile();
			scene.SceneOrigin = this._ctx!.Scene.GetSceneOrigin();

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
				var poseFile = new EntityPoseConverter(chara.Pose!).SaveFile();
				
				scene.Actors.Add(new SceneFile.ActorInfo() {
					Chara = charaFile,
					Pose = poseFile,
					Location = location,
					MCDF = String.Empty,
					Index = ((ActorEntity)chara).Actor.ObjectIndex
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
				c.OrbitTarget = camera.OrbitTarget ?? camera.GameCamera->GetCameraTargetObject()->ObjectIndex;
				

				c.FixedPosition = this.Scene.GetActorRelativePosition((Vector3)camera.GetPosition());
				c.Flags = (uint)camera.Flags;
				c.IsActive = (this._ctx.Cameras.Current ==  camera);
				c.Name = camera.Name;
				c.OrthographicZoom = camera.OrthographicZoom;
				scene.Cameras.Add(c);
			}
			
			/*if (this._ctx.Scene.GetModule<EnvModule>().Override != 0x000) {
				var env = new SceneFile.EnviromentInfo();
				env.Override = this._ctx.Scene.GetModule<EnvModule>().Override;
				env.State = EnvManagerEx.Instance()->EnvState;
				scene.Enviroment = env;
			}*/
			return scene;


	
	}

	public async Task Load(String path, bool autoSaveLoading = true) {


		if (File.Exists(path) && Path.GetExtension(path) == ".ktscene") { //fix this later idc

			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);
			this._idMap	= new Dictionary<ushort, ActorEntity>();
	

			foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is CharaEntity).ToList()) {
				var e = (ActorEntity)sceneEntity;
				this.Scene.GetModule<ActorModule>().Delete(e, true);
				this.Scene.Remove(e);
			}
			
			Vector3 sceneOrigin;
			if (!autoSaveLoading) {
				sceneOrigin = this._objectTable.LocalPlayer!.Position;
			} else {
				sceneOrigin = scene!.SceneOrigin;
			}


			foreach (var loaded in scene!.Actors.Where(info => info.Chara.ModelType != 0)) {
				loaded.Location.Position += sceneOrigin;
				await this._framework.RunOnFrameworkThread(() => SetupActor(loaded));
				await this._framework.DelayTicks(10);
				
			}
			await this._framework.DelayTicks(30);
			foreach (var loaded in scene!.Actors.Where(info => info.Chara.ModelType == 0)) {
				loaded.Location.Position += sceneOrigin;
				await this._framework.RunOnFrameworkThread(() => SetupActor(loaded));
				await this._framework.DelayTicks(10);
			}
			
			//spawn non humans after?

			
			foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is LightEntity).ToList()) {
				LightEntity lightEntity = (LightEntity)sceneEntity;
				lightEntity.Delete();
			}

			foreach (var loaded in scene.Lights) {
				var light = this._ctx!.Scene.Factory.CreateLight().Spawn().Result;
				_ = this._ctx.Scene.ApplyLightFile(light, loaded.Light);
				loaded.Location.Position += sceneOrigin;
				light.SetTransform(loaded.Location);
			}

			
			//always at least one camera (I hope....)

			var primaryCamera = this._ctx!.Cameras.Current;
			primaryCamera!.ResetState();
			var primaryInfo = scene.Cameras.Find(c => c.IsActive);
			primaryCamera.FixedPosition = primaryInfo.FixedPosition + sceneOrigin;
			primaryCamera.OrthographicZoom = primaryInfo.OrthographicZoom;
			primaryCamera.Flags = (CameraFlags)primaryInfo.Flags;
			if(primaryInfo.OrbitTarget != 0)
				primaryCamera.OrbitTarget = (this._idMap[primaryInfo.OrbitTarget].Actor.ObjectIndex); // might fail so it goes last
			
		}



		//surely one of these does what I want
	}

	

	private  void SetupActor(SceneFile.ActorInfo actor) {
		this._ctx!.Scene.Factory.CreateActor().WithAppearance(actor.Chara).Spawn().ContinueWith(async (p) => {
			var a = p.GetResultSafely();
			this._idMap.Add(actor.Index, a);
			await this._framework.DelayTicks(15);
			a.Name = actor.Chara.Nickname!;
			this.SetupActorPosition(actor, a);
			await this._framework.DelayTicks(45);  //these delay ticks are unfortunately required or things start to go bad
			this._task?.Wait();
			this._task =  this._ctx?.Posing.ApplyPoseFile(a.Pose!, actor.Pose, PoseMode.All,(PoseTransforms)0xF)!;
		});
	}
	
	//TODO: The BattleChara should have the position set too, its what the camera orbit bases itself off of
	private unsafe void SetupActorPosition(SceneFile.ActorInfo loaded, ActorEntity actor) {
		//this._ctx.Characters.ApplyCharaFile(actor, loaded.Chara);
		var draw = actor.GetCharacter();
		draw->Position = loaded.Location.Position;
		draw->Rotation = loaded.Location.Rotation;
		draw->Scale = loaded.Location.Scale;
	}


}