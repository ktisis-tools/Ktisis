using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.System.Memory;

using Ktisis.Structs;
using Ktisis.Structs.Common;
using Ktisis.Structs.Lights;
using Ktisis.Interop.Hooking;
using Ktisis.Structs.Camera;

namespace Ktisis.Scene.Modules.Lights;

public class LightSpawner : HookModule {
	private readonly IFramework _framework;
	
	public LightSpawner(
		IHookMediator hook,
		IFramework framework
	) : base(hook) {
		this._framework = framework;
	}
	
	// Initialization
	
	public void TryInitialize() {
		try {
			this.Initialize();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize light spawner:\n{err}");
		}
	}
	
	// Created lights

	private readonly HashSet<nint> _created = new();
	
	// Construction
	
	[Signature("E8 ?? ?? ?? ?? 48 89 84 FB ?? ?? ?? ?? 48 85 C0 0F 84 ?? ?? ?? ?? 48 8B C8")]
	private SceneLightCtorDelegate _sceneLightCtor = null!;
	private unsafe delegate SceneLight* SceneLightCtorDelegate(SceneLight* self);
	
	[Signature("E8 ?? ?? ?? ?? 48 8B 94 FB ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ??")]
	private SceneLightInitializeDelegate _sceneLightInit = null!;
	private unsafe delegate bool SceneLightInitializeDelegate(SceneLight* self);
	
	[Signature("F6 41 38 01")]
	private SceneLightSetupDelegate _sceneLightSpawn = null!;
	private unsafe delegate nint SceneLightSetupDelegate(SceneLight* self);

	public unsafe SceneLight* Create() {
		var light = (SceneLight*)IMemorySpace.GetDefaultSpace()->Malloc<SceneLight>();
		this._sceneLightCtor(light);
		this._sceneLightInit(light);
		this._sceneLightSpawn(light);

		var activeCamera = GameCameraEx.GetActive();
		if (activeCamera != null) {
			light->Transform.Position = activeCamera->Position;
			light->Transform.Rotation = activeCamera->CalcPointDirection();
		}

		*(ulong*)((nint)light + 56) |= 2u;

		var render = light->RenderLight;
		if (render != null) {
			render->Flags = LightFlags.Reflection;
			render->LightType = LightType.PointLight;
			render->Transform = &light->Transform;
			render->Color = new ColorHDR();
			render->ShadowNear = 0.1f;
			render->ShadowFar = 15.0f;
			render->FalloffType = FalloffType.Quadratic;
			render->AreaAngle = Vector2.Zero;
			render->Falloff = 1.1f;
			render->LightAngle = 45.0f;
			render->FalloffAngle = 0.5f;
			render->Range = 100.0f;
			render->CharaShadowRange = 100.0f;
		}

		this._created.Add((nint)light);
		return light;
	}
	
	// Destruction
	
	private unsafe delegate void CleanupRenderDelegate(SceneLight* light);
	private unsafe delegate void DestructorDelegate(SceneLight* light);

	public unsafe void Destroy(SceneLight* light) {
		this._created.Remove((nint)light);
		this._framework.RunOnFrameworkThread(() => {
			this.InvokeDtor(light);
		});
	}

	private unsafe void DestroyAll() {
		if (this._framework.IsFrameworkUnloading) return;
		this._framework.RunOnFrameworkThread(() => {
			foreach (var address in this._created)
				this.InvokeDtor((SceneLight*)address);
			this._created.Clear();
		});
	}

	private unsafe void InvokeDtor(SceneLight* light) {
		GetVirtualFunc<CleanupRenderDelegate>(light, 1)(light);
		GetVirtualFunc<DestructorDelegate>(light, 0)(light);
	}
	
	// Marshalling
	
	private unsafe static T GetVirtualFunc<T>(SceneLight* light, int index)
		=> Marshal.GetDelegateForFunctionPointer<T>(light->_vf[index]);
	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		Ktisis.Log.Verbose("Disposing light spawn manager...");
		this.DestroyAll();
		GC.SuppressFinalize(this);
	}
}
