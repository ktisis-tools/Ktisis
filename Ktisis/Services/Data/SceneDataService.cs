using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Enums;
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
using Ktisis.Data.Json.Converters;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Creators;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Types;
using Ktisis.Structs.Env;

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

	public void WriteFile(string path) {
		try {
			var file = this.Save();
			var serializer = new JsonFileSerializer();
			serializer.GetConverter<Vector3>();
			serializer.GetConverter<Transform>();
			File.WriteAllText(path, serializer.Serialize(file));
		} catch {
			Ktisis.Log.Warning("Failed to write Scene file");
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
			
			
			//Environment info 
			var module = this._ctx.Scene.GetModule<EnvModule>();
			var flags = (uint)module.Override;

			var env = new SceneFile.EnvironmentInfo();
			env.Override = flags;

			var marshalled = Marshal.PtrToStructure<EnvManagerEx>((IntPtr) EnvManagerEx.Instance());
			
			env.State = marshalled.EnvState;

			env.Day = module.Day;
			env.Time = module.Time;
			env.Weather = module.Weather;
			
			scene.Environment = env;
			return scene;
	}
	
	public SceneFile LoadFile(String path) {
			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);
			return scene!;
	}
	
	public async Task Load(SceneFile scene, bool autoSaveLoading = true, bool loadActors = true, bool loadLights = true, bool loadCameras = true, bool loadEnv = true) {
		
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
				foreach (var loaded in scene!.Actors.Where(info => info.Chara.ModelType == 0)) {
					loaded.Location.Position += sceneOrigin;
					await this._framework.RunOnFrameworkThread(() => SetupActor(loaded));
					await this._framework.DelayTicks(10);
				}
				await this._framework.DelayTicks(30);
				foreach (var loaded in scene!.Actors.Where(info => info.Chara.ModelType != 0)) {
					loaded.Location.Position += sceneOrigin;
					await this._framework.RunOnFrameworkThread(() => SetupActor(loaded));
					await this._framework.DelayTicks(10);
					if (loaded.Chara.ObjectKind == ObjectKind.Ornament) {
						var orn = this._idMap[loaded.Index];
						orn.Appearance.ModelId = loaded.Chara.ModelType;
						orn.Redraw();
					}
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
			
			//Env stuff
			if (loadEnv) {
				unsafe {
					var flags = scene.Environment.Override;

					var module = this._ctx.Scene.GetModule<EnvModule>();
					module.Override = (EnvOverride)flags;

					if (flags > 0) {
						Marshal.StructureToPtr<EnvState>(scene.Environment.State, (IntPtr)EnvManagerEx.Instance()+0x058, false);
					}
					if (module.Override.HasFlag(EnvOverride.TimeWeather)) {
						module.Day = scene.Environment.Day;
						module.Time =  scene.Environment.Time;
						module.Weather = scene.Environment.Weather;
					}
				}
			}
	}
	
	//Map data
	public unsafe uint GetCurrentMapID() => AgentMap.Instance()->CurrentMapId;
	
	//Actor functions
	internal bool ValidMCDFPath(SceneFile.ActorInfo a) => a.MCDF != String.Empty && Path.Exists(a.MCDF);

	private void SetupActor(SceneFile.ActorInfo actor) {
		IActorCreator t = this._ctx!.Scene.Factory.CreateActor();
		if (this.ValidMCDFPath(actor))
			t = t.WithMcdf(actor.MCDF);
		else if (actor.MCDF != String.Empty) {
			t = t.WithAppearance(actor.Chara);
			Ktisis.WarningNotification($"Couldn't find the MCDF linked to the actor {actor.Chara.Nickname}, please try and load it manually.");
		}else
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
	private unsafe void SetupActorPosition(SceneFile.ActorInfo loaded, ActorEntity actor) { 		//I hate my life so much lol
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
	}
}