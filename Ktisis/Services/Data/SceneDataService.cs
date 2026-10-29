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
using Ktisis.Interface.KTK;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Factory.Builders;
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
	
	private Task? _task;
	private Dictionary<ushort, ActorEntity> _idMap;
	private Dictionary<Guid, SceneEntity> _guidMap;
	
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
	public unsafe SceneFile Save(bool saveActors = true,
		bool saveLights = true,
		bool saveCameras = true,
		bool saveEnv = true,
		bool saveOverlays = true,
		bool saveObjects = true) {


			var scene = new SceneFile();
			scene.SceneOrigin = this._ctx!.Scene.GetSceneOrigin();
			scene.MapID = this.GetCurrentMapID();

			scene.Folders = new List<SceneFile.Folder>();
			scene.Folders.Add(new SceneFile.Folder("Scene Root"){Guid = Guid.NewGuid()});

			Dictionary<FolderEntity, Guid> lookup = new Dictionary<FolderEntity, Guid>();
			foreach (var folder in this.Scene.Recurse() //what a fucked up for each syntax lol
				.Where(entity => entity is FolderEntity)
				.Distinct()
				.Cast<FolderEntity>()
				.ToList()) {
				
				var f = new SceneFile.Folder(folder.Name);

				f.Guid = Guid.NewGuid();
				ushort depth = 1;
				
				ushort DepthCalc(SceneEntity fld) {
					if (fld.Parent?.Type != EntityType.Folder)
						return depth;
					depth++;
					return DepthCalc(fld.Parent);
				}

				f.Depth = DepthCalc(folder);
				
				lookup.Add(folder, f.Guid);
				scene.Folders.Add(f);
			}
			foreach (var folder in this.Scene.Recurse()  //Just in case we somehow get a folder who shows up before its parent
				.Distinct()	
				.Where(entity => entity is FolderEntity)
				.Cast<FolderEntity>()
				.ToList()) {
				if(folder.Parent?.Type != EntityType.Folder)
					scene.Folders[0].Children.Add(lookup[folder]);
				else
					scene.Folders.Find(f => f.Guid == lookup[(FolderEntity)folder.Parent]).Children.Add(lookup[folder]);
			}

			
			var entities = this.Scene.Recurse()
				.Distinct()
				.Where(entity => entity is ActorEntity)
				.Cast<ActorEntity>()
				.ToList();

			var lights = this.Scene.Recurse()
				.Distinct()
				.Where(entity => entity is LightEntity)
				.Cast<LightEntity>()
				.ToList();
		
			var overlays = this.Scene.Recurse()
				.Distinct()
				.OfType<OverlayEntity>()
				.ToList();
			
			var objects = this.Scene.Children.OfType<ObjectEntity>()
				.ToList();

			Ktisis.Log.Debug("Collected all entity lists");
			//TODO: Attaches
			if(saveActors)
				foreach (var chara in entities) {
					Ktisis.Log.Debug($"Exporting {chara.Name}");
					Ktisis.Log.Debug($"Data: {chara}");
					var actor = ((ActorEntity)chara).Actor.GetDrawObject();
						
					var location = new Transform(this.Scene.GetActorRelativePosition(actor->Position), actor->Rotation, actor->Scale);
					var charaFile = this._ctx.Characters.SaveCharaFile((ActorEntity)chara).GetResultSafely();
					var poseFile = new EntityPoseConverter(chara.Pose!).SaveFile();
					var defaultRotation = ((ActorEntity)chara).CsGameObject->Rotation;
					var ipc = this._ctx.Plugin.Ipc;
					Guid penumCollection = Guid.Empty;
					Guid cPlus = Guid.Empty;
					string? state = null;
					if (ipc.IsPenumbraActive && ((ActorEntity)chara).MCDF == null) {
						var penum = ipc.GetPenumbraIpc().GetCollectionForObject(((ActorEntity)chara).Actor);
						if (penum.Id != Guid.Empty) {
							penumCollection = penum.Id;
						}
					}
					if (ipc.IsCustomizeActive && ((ActorEntity)chara).MCDF == null && ((ActorEntity)chara).AssignedProfile != Guid.Empty) {
						var assignedProfile = ((ActorEntity)chara).AssignedProfile;
						if (assignedProfile != null) cPlus = (Guid)assignedProfile;
					}

					if (ipc.IsGlamourerActive && ((ActorEntity)chara).MCDF == null)
					{
						var tempState = ipc.GetGlamourerIpc().GetState(((ActorEntity)chara).Actor.ObjectIndex);
						if (tempState != null)
							state = tempState;

					}

					var a = new SceneFile.ActorInfo() {
						Chara = charaFile,
						Pose = poseFile,
						Location = location,
						MCDF = this._ctx.Characters.Mcdf.LoadedMCDFPath(((ActorEntity)chara).Actor),
						DefaultRotation = defaultRotation,
						Index = ((ActorEntity)chara).Actor.ObjectIndex,
						PenumbraCollection = penumCollection,
						CustomizePlus = cPlus,
						Guid = Guid.NewGuid(),
						GlamourerState = state
					};

					scene.Actors.Add(a);
					if (chara.Parent == this.Scene.SceneRoot)
						scene.Folders[0].Children.Add(a.Guid);
					else {
						if (chara.Parent?.Type == EntityType.Folder) {
							scene.Folders.Find(f => f.Guid ==lookup[(FolderEntity)chara.Parent]).Children.Add(a.Guid);
						}
					}
				}

			if(saveLights)
				foreach (var light in lights) {
					var lightFile = Scene.SaveLightFile(light).Result;
					var l = light.GetObject()->Transform;
					var location = new Transform(this.Scene.GetActorRelativePosition(l.Position), l.Rotation, l.Scale);
					var lightObj = new SceneFile.LightInfo(lightFile, location, light.Name, !light.IsHidden, Guid.NewGuid());

					scene.Lights.Add(lightObj);
					if (light.Parent == this.Scene.SceneRoot)
						scene.Folders[0].Children.Add(lightObj.Guid);
					else {
						if (light.Parent?.Type == EntityType.Folder) {
							scene.Folders.Find(f => f.Guid ==lookup[(FolderEntity)light.Parent]).Children.Add(lightObj.Guid);
						}
					}
				}
			
			if(saveCameras)
				foreach (var camera in this._ctx.Cameras.GetCameras()) {
					var c = new SceneFile.CameraInfo();
					c.OrbitTarget = camera.OrbitTarget ?? camera.GameCamera->GetCameraTargetObject()->ObjectIndex;

					c.IsDelmited = camera.IsDelimited;
					c.Angle = new Vector3(camera.Camera->Angle.X, camera.Camera->Angle.Y, camera.Camera->Distance);
					c.FixedPosition = this.Scene.GetActorRelativePosition((Vector3)camera.GetPosition());
					c.Flags = (uint)camera.Flags;
					c.IsActive = (this._ctx.Cameras.Current ==  camera);
					c.Name = camera.Name;
					c.OrthographicZoom = camera.OrthographicZoom;
					scene.Cameras.Add(c);
				}

			if(saveOverlays)
				foreach (var overlay in overlays) {
					SceneFile.OverlayInfo.Type type = SceneFile.OverlayInfo.Type.None;
					string dialog = string.Empty;
					if (overlay.Type == EntityType.BalloonOverlay) {
						type = SceneFile.OverlayInfo.Type.Balloon;
						dialog = ((BalloonOverlay)overlay).Dialog;

					}else if (overlay.Type == EntityType.StatusOverlay) {
						type = SceneFile.OverlayInfo.Type.Status;
						dialog = ((StatusOverlay)overlay).StatusText;
					}
					else if (overlay.Type == EntityType.TalkOverlay) {
						type = SceneFile.OverlayInfo.Type.Talk;
						dialog = ((TalkOverlay)overlay).Dialog;
					}

					var file = new SceneFile.OverlayInfo() {
						OverlayType = type,
						Dialog = dialog,
						Position = overlay.Position,
						Opacity = overlay.Alpha,
						Scale = overlay.Scale,
						Visible = overlay.Visible,
						Name = overlay.Name,
					};
					
					switch (overlay.Type)
					{
						case EntityType.BalloonOverlay:
							file.ArrowPosition = ((BalloonOverlay)overlay).ArrowX;
							file.ShowArrow = ((BalloonOverlay)overlay).Arrow;
							file.BalloonBackground = ((BalloonOverlay)overlay).Background;
							file.BalloonColor = ((BalloonOverlay)overlay).Color;
							file.FontSize = ((BalloonOverlay)overlay).FontSize;
							break;
						case EntityType.TalkOverlay:
							file.TalkBackground = ((TalkOverlay)overlay).Background;
							file.TalkCursor = ((TalkOverlay)overlay).Cursor;
							file.Speaker = ((TalkOverlay)overlay).Speaker;
							file.FontSize = ((TalkOverlay)overlay).FontSize;
							break;
						case EntityType.StatusOverlay:
							file.StatusIcon = ((StatusOverlay)overlay).IconPath;
							file.StatusType = ((StatusOverlay)overlay).StatusType;
							break;
					}

					file.Guid = Guid.NewGuid();
					
					scene.Overlays.Add(file);
					
					if (overlay.Parent == this.Scene.SceneRoot)
						scene.Folders[0].Children.Add(file.Guid);
					else {
						if (overlay.Parent?.Type == EntityType.Folder) {
							scene.Folders.Find(f => f.Guid ==lookup[(FolderEntity)overlay.Parent]).Children.Add(file.Guid);
						}
					}
				}
			
			if (saveObjects) {
				foreach (var obj in objects) {
					var o = new SceneFile.ObjectInfo(){IsHidden = obj.IsHidden, Name = obj.Name, Path = obj.GetPath(), Transform = obj.GetTransform(), Guid = Guid.NewGuid()};
					if (obj.Parent == this.Scene.SceneRoot)
						scene.Folders[0].Children.Add(o.Guid);
					else {
						if (obj.Parent?.Type == EntityType.Folder) {
							scene.Folders.Find(f => f.Guid ==lookup[(FolderEntity)obj.Parent]).Children.Add(o.Guid);
						}
					}
					scene.Objects.Add(o);
				}
			}
			
			//Environment info 
			if (saveEnv) {
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
			}

			return scene;
	}
	
	public SceneFile LoadFile(String path) {
			var file = File.ReadAllText(path);
			var serializer = new JsonFileSerializer();
			var scene = serializer.Deserialize<SceneFile>(file);
			return scene!;
	}
	
	public async Task Load(SceneFile scene,
		bool autoSaveLoading = true,
		bool loadActors = true,
		bool loadLights = true,
		bool loadCameras = true,
		bool loadEnv = true,
		bool loadOverlays = true,
		bool loadObjects = true,
		bool preserveExistingActors = false,
		FolderHandling folderHandling = FolderHandling.Ignore) {
		
			this._idMap	= new Dictionary<ushort, ActorEntity>();
			this._guidMap = new Dictionary<Guid, SceneEntity>();
			

			//this is broken and needs to be fixed
			if (folderHandling == FolderHandling.Overwrite)
				while (this.Scene.Children.OfType<FolderEntity>().Any()) {
					this.Scene.Children.OfType<FolderEntity>().First().Dissolve();
				}
			else if (folderHandling == FolderHandling.Append) {
				preserveExistingActors = true;
			}
			if (loadActors && !preserveExistingActors) {
				foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is CharaEntity).ToList()) {
					var e = (ActorEntity)sceneEntity;
					this.Scene.GetModule<ActorModule>().Delete(e, true);
					this.Scene.Remove(e);
				}
			}
			if(loadOverlays)
				foreach (var sceneEntity in this.Scene.Children.Where(entity => entity is OverlayEntity).ToList()) {
					sceneEntity.Remove();
				}


			var startIndex = 0;
			if(folderHandling == FolderHandling.Append)
				startIndex = this.Scene.SceneRoot.Children.Count();
			
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
					if(loaded.Guid != Guid.Empty)
						this._guidMap.TryAdd(loaded.Guid, light);
					loaded.Location.Position += sceneOrigin;
					light.SetTransform(loaded.Location);
					if(!loaded.State)
						light.ToggleHidden();
				}
			}
			
			


			if (loadOverlays) {
				foreach (var overlayInfo in scene.Overlays) {
					this._framework.RunOnFrameworkThread(() => {
						OverlayEntity? overlay = null;
						switch (overlayInfo.OverlayType) {
							case SceneFile.OverlayInfo.Type.Balloon:
								overlay = this._ctx!.Scene.Factory.BuildOverlay(OverlayTypes.Balloon).SetName(overlayInfo.Name).Add();
								((BalloonOverlay)overlay).Arrow = overlayInfo.ShowArrow;
								((BalloonOverlay)overlay).ArrowX = overlayInfo.ArrowPosition;
								((BalloonOverlay)overlay).Background = overlayInfo.BalloonBackground;
								((BalloonOverlay)overlay).Dialog = overlayInfo.Dialog;
								((BalloonOverlay)overlay).FontSize = (overlayInfo.FontSize != 0) ? overlayInfo.FontSize : 12;
								break;
							case SceneFile.OverlayInfo.Type.Talk:
								overlay = this._ctx!.Scene.Factory.BuildOverlay(OverlayTypes.Talk).SetName(overlayInfo.Name).Add();
								((TalkOverlay)overlay).Background = overlayInfo.TalkBackground;
								((TalkOverlay)overlay).Cursor = overlayInfo.TalkCursor;
								((TalkOverlay)overlay).Dialog = overlayInfo.Dialog;
								((TalkOverlay)overlay).Speaker = overlayInfo.Speaker;
								((TalkOverlay)overlay).FontSize = (overlayInfo.FontSize != 0) ? overlayInfo.FontSize : 14;
								break;
							case SceneFile.OverlayInfo.Type.Status:
								overlay = this._ctx!.Scene.Factory.BuildOverlay(OverlayTypes.Status).SetName(overlayInfo.Name).Add();
								((StatusOverlay)overlay).IconPath = overlayInfo.StatusIcon;
								((StatusOverlay)overlay).StatusText = overlayInfo.Dialog;
								((StatusOverlay)overlay).StatusType = overlayInfo.StatusType;
								break;
						}

						if (overlay != null) {
							overlay.Alpha = overlayInfo.Opacity / 255.0f;
							overlay.Position = overlayInfo.Position;
							overlay.Scale = overlayInfo.Scale;
							overlay.Visible = overlayInfo.Visible;
							if(overlayInfo.Guid != Guid.Empty)
								this._guidMap.TryAdd(overlayInfo.Guid, overlay);
						}
					});
				}
			}
			
			if (loadObjects) {
				unsafe {
					foreach (var obj in scene.Objects) {
						var ptr = this._ctx!.Scene.World.BuildObject(obj.Path);
						var entity = this._ctx!.Scene.Factory.BuildObject().SetAddress(ptr).SetName(obj.Name).Add();
						entity.Visible = obj.IsHidden;
						entity.SetTransform(obj.Transform);
						entity.Update();
						if(obj.Guid != Guid.Empty)
							this._guidMap.TryAdd(obj.Guid, entity);
					}
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

			if (folderHandling != FolderHandling.Ignore && scene.Folders != null && this._guidMap.Count > 0) {
				foreach (var folder in scene.Folders.OrderByDescending(c => c.Depth)) {
					if (folder.Depth > 0) {
						var f = new FolderEntity(this._ctx!.Scene);
						f.Name = folder.Name;
						this._guidMap.Add(folder.Guid, f);
						if (folder.Depth == 1)
							f.Parent = this.Scene.SceneRoot;
						this._ctx!.Scene.Add(f);

						foreach (var child in folder.Children) {
							if (this._guidMap.TryGetValue(child, out var entity)) {
								entity.Parent?.Remove(entity);
								f.AddAtIndex(entity, folder.Children.IndexOf(child));
							}
						}

					}

				}
				foreach (var c in scene.Folders[0].Children) {
					if (this._guidMap.TryGetValue(c, out var entity)) {
						this.Scene.SceneRoot.SetChildsIndex(entity, scene.Folders[0].Children.IndexOf(c) + startIndex);

					}
				}
			}

			// cameras
			// having this further up didnt give enough time for all actors to spawn TODO: build a task to do this after all actors are done
			if (loadCameras) {
				var defaultCam =   this._ctx!.Cameras.GetCameras().First(c => c.IsDefault);
				this._ctx!.Cameras.SetCurrent(defaultCam);

				var diff = scene.Cameras.Count - this._ctx!.Cameras.GetCameras().Count();

				if (diff > 0) {
					for(var i = 0; i < diff; i++)
						this._ctx!.Cameras.Create(setActive: false);
				} else if( diff < 0 ) {
					diff *= -1;
					for (var i = 0; i < diff; i++)
						this._ctx!.Cameras.DeleteCurrent();
				}



				var currentCam = scene.Cameras.First(c => c.IsActive);
				var currentKtCam = this._ctx.Cameras.Current;

				var zipped = scene.Cameras.Where(c=> !c.IsActive).Zip(this._ctx.Cameras.GetCameras().Where(c=> c != currentKtCam));
				ApplyCamera(currentCam, currentKtCam!, sceneOrigin);
				foreach (var set in zipped) 
					ApplyCamera(set.First, set.Second, sceneOrigin);
			}
	}

	public void ApplyCamera(SceneFile.CameraInfo camera, EditorCamera ktCam, Vector3 sceneOrigin) {
		if (camera.IsDelmited) {
			ktCam.FixedPosition = camera.FixedPosition + sceneOrigin;
		} else {
			unsafe {
				ktCam.Camera->Angle.X = camera.Angle.Value.X;
				ktCam.Camera->Angle.Y = camera.Angle.Value.Y;
				ktCam.Camera->Distance = camera.Angle.Value.Z;
			}
		}
		ktCam.OrthographicZoom = camera.OrthographicZoom;
		ktCam.Flags = (CameraFlags)camera.Flags;
		if (camera.OrbitTarget != 0) {
			unsafe {
				ktCam.OrbitTarget = (this._idMap[camera.OrbitTarget].CsGameObject->ObjectIndex);
				this._idMap[camera.OrbitTarget].Actor.SetGPoseTarget();
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
		}else if (actor.GlamourerState != null && this._ctx.Plugin.Ipc.IsGlamourerActive)
			t = t.WithGlamourerState(actor.GlamourerState);
		else
			t = t.WithAppearance(actor.Chara);

		t.Spawn().ContinueWith(async (p) => {
			var a = p.GetResultSafely();
			this._idMap.Add(actor.Index, a);
			
			if(actor.Guid != Guid.Empty)
				this._guidMap.TryAdd(actor.Guid, a);
			await this._framework.DelayTicks(15);
			a.Name = actor.Chara.Nickname!;
			var act = a.Actor;
			
			if (actor.PenumbraCollection != Guid.Empty && this._ctx.Plugin.Ipc.IsPenumbraActive)
				if (this._ctx.Plugin.Ipc.GetPenumbraIpc().GetCollections().ContainsKey(actor.PenumbraCollection)) {
					this._ctx.Plugin.Ipc.GetPenumbraIpc().SetCollectionForObject(act, actor.PenumbraCollection);
					this._ctx.Plugin.Ipc.GetPenumbraIpc().Redraw(act.ObjectIndex);
				}

			
			if (actor.CustomizePlus != Guid.Empty && this._ctx.Plugin.Ipc.IsCustomizeActive) {
				var profile = this._ctx.Plugin.Ipc.GetCustomizeIpc().GetProfileByUniqueId(actor.CustomizePlus);
				if (profile.Data != string.Empty) {
					this._ctx.Plugin.Ipc.GetCustomizeIpc().SetTemporaryProfile(a.Actor.ObjectIndex, profile.Data);
					a.AssignedProfile = actor.CustomizePlus;
				}
			}

			this.SetupActorPosition(actor, a);
			this._task?.Wait();
			await this._framework.DelayTicks(45);  //these delay ticks are unfortunately required or things start to go bad

			
			this._task = this._ctx?.Posing.ApplyPoseFile(a.Pose!, actor.Pose, PoseMode.All,(PoseTransforms)0xF)!;

		});
	}
	
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

public enum FolderHandling {
	Ignore,
	Append,
	Overwrite
}