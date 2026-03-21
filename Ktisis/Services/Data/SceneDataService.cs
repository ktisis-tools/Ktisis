using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using System.Threading.Tasks;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Creators;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;

using Lumina.Excel.Sheets;


namespace Ktisis.Services.Data;

[Singleton]
public class SceneDataService {
	
	private IEditorContext? _ctx;
	private IObjectTable _objectTable;
	private IFramework _framework;
	private IDataManager _data;
	
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

		_idMap = new Dictionary<ushort, ActorEntity>();
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
			scene.MapID = this.GetCurrentMapID();
			
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
				var defaultRotation = ((ActorEntity)chara).CsGameObject->Rotation;
				
				scene.Actors.Add(new SceneFile.ActorInfo() {
					Chara = charaFile,
					Pose = poseFile,
					Location = location,
					MCDF = this._ctx.Characters.Mcdf.LoadedMCDFPath(((ActorEntity)chara).Actor),
					DefaultRotation = defaultRotation,
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

				c.isDelmited = camera.IsDelimited;
				c.Angle = new Vector3(camera.Camera->Angle.X, camera.Camera->Angle.Y, camera.Camera->Distance);
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

	public SceneFile LoadFile(String path) {
			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);
			return scene!;
		
	}
	
	public async Task Load(SceneFile scene, bool autoSaveLoading = true, bool loadActors = true, bool loadLights = true, bool loadCameras = true) {

			

			this._idMap	= new Dictionary<ushort, ActorEntity>();

			if (loadActors) {
				foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is CharaEntity).ToList()) {
					var e = (ActorEntity)sceneEntity;
					this.Scene.GetModule<ActorModule>().Delete(e, true);
					this.Scene.Remove(e);
				}
			}
			Vector3 sceneOrigin;
			if (!autoSaveLoading) {
				sceneOrigin = this._objectTable.LocalPlayer!.Position;
			} else {
				sceneOrigin = scene!.SceneOrigin;
			}

			if (loadActors) {
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
			}
			//spawn non humans after?

			if (loadLights) {
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
			}
			
			//always at least one camera (I hope....)
			if (loadCameras) {
				var primaryCamera = this._ctx!.Cameras.Current;
				primaryCamera!.ResetState();
				var primaryInfo = scene.Cameras.Find(c => c.IsActive);
				if (primaryInfo.isDelmited) {
					primaryCamera.FixedPosition = primaryInfo.FixedPosition + sceneOrigin;
				} else {
					unsafe {
						primaryCamera.Camera->Angle.X = primaryInfo.Angle.Value.X;
						primaryCamera.Camera->Angle.Y = primaryInfo.Angle.Value.Y;
						primaryCamera.Camera->Distance = primaryInfo.Angle.Value.Z;
					}
					
				}

				primaryCamera.OrthographicZoom = primaryInfo.OrthographicZoom;
				primaryCamera.Flags = (CameraFlags)primaryInfo.Flags;
				if (primaryInfo.OrbitTarget != 0) {
					primaryCamera.OrbitTarget = (this._idMap[primaryInfo.OrbitTarget].Actor.ObjectIndex);
					this._idMap[primaryInfo.OrbitTarget].Actor.SetGPoseTarget();
				}
			}
			
		

	}


	public void GetVitalStats(string path) {
		var file = File.ReadAllText(path);
		var serializer = new JsonFileSerializer();
		var scene = serializer.Deserialize<SceneFile>(file);
		
		
		
	}
	
	
	
	//Map data
	public unsafe uint GetCurrentMapID() => AgentMap.Instance()->CurrentMapId;

	private Map GetMapSheetData(uint mapId) {
		ExcelSheet<Map> map = null!;
		map = this._data.GetExcelSheet<Map>();
		map.TryGetRow(mapId, out var mapRow);
		return mapRow;
	}
	
	//Actor functions
	private bool ValidMCDFPath(SceneFile.ActorInfo a) => a.MCDF != String.Empty && Path.Exists(a.MCDF);

	private void SetupActor(SceneFile.ActorInfo actor) {
		IActorCreator t = this._ctx!.Scene.Factory.CreateActor();
		if (this.ValidMCDFPath(actor))
			t = t.WithMcdf(actor.MCDF);
		else
			t = t.WithAppearance(actor.Chara);

		t.Spawn().ContinueWith(async (p) => {
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
		var bc = (BattleChara*)actor.CsGameObject;
		bc->DefaultPosition = loaded.Location.Position;
		bc->DefaultRotation = loaded.DefaultRotation;
		bc->SetPosition(loaded.Location.Position.X,  loaded.Location.Position.Y, loaded.Location.Position.Z);
		bc->SetRotation(loaded.DefaultRotation);
		
		
		
		var draw = actor.GetCharacter();
		
		draw->Position = loaded.Location.Position;
		draw->Rotation = loaded.Location.Rotation;
		draw->Scale = loaded.Location.Scale;
		
		//i hate my life so much lol


	}



}