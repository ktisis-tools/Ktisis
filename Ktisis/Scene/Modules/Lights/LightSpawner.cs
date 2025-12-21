using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Resource;

using InteropGenerator.Runtime;

using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Camera.Types;
using Ktisis.Structs.Common;
using Ktisis.Structs.Lights;
using Ktisis.Interop.Hooking;
using Ktisis.Common.Utility;

namespace Ktisis.Scene.Modules.Lights;

public class LightSpawner : HookModule {
	private readonly IFramework _framework;
	private readonly IEditorContext _context;
	
	public LightSpawner(
		IHookMediator hook,
		IFramework framework,
		IEditorContext context
	) : base(hook) {
		this._framework = framework;
		this._context = context;
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

	[Signature("40 53 48 83 EC ?? 48 8B D9 C7 44 24 ?? ?? ?? ?? ?? 33 C9")]
	private SceneLightTextureDelegate _sceneLightTexture = null!;
	private unsafe delegate bool SceneLightTextureDelegate(SceneLight* self, ResourceCategory* category, CStringPointer path);

	[Signature("F6 41 38 01")]
	private SceneLightSetupDelegate _sceneLightSpawn = null!;
	private unsafe delegate nint SceneLightSetupDelegate(SceneLight* self);

	public unsafe SceneLight* Create() {
		var light = (SceneLight*)IMemorySpace.GetDefaultSpace()->Malloc<SceneLight>();
		Ktisis.Log.Info($"{(nint)light:X}");
		this._sceneLightCtor(light);
		this._sceneLightInit(light);
		this._sceneLightSpawn(light);


		// use freecam values if applicable, else point to scenecamera
		var editorCamera = this._context.Cameras.Current;
		if (editorCamera is WorkCamera freeCam) {
			light->Transform.Position = freeCam.Position;
			light->Transform.Rotation = freeCam.CalculateLookDirection().EulerAnglesToQuaternion();
		} else {
			light->Transform.Position = editorCamera.Camera->Position;
			light->Transform.Rotation = editorCamera.Camera->CalcPointDirection();
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


		string path = "bgcommon/hou/indoor/general/1133/texture/fun_b0_m1133_0b_i.tex\0";
		byte* texPtr = stackalloc byte[path.Length];
		for (int i = 0; i < path.Length; ++i) {
			texPtr[i] = (byte)path[i];
		}
		ResourceCategory* resourceCat = stackalloc ResourceCategory[1];
		resourceCat[0] = ResourceCategory.BgCommon;

		this._sceneLightTexture(light, resourceCat, texPtr);
		this._created.Add((nint)light);

		Ktisis.Log.Info($"{(nint)light:X}");
		return light;
	}
	
	// Destruction
	
	private unsafe delegate void CleanupRenderDelegate(SceneLight* light);
	private unsafe delegate void DestructorDelegate(SceneLight* light, bool a2);

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
		GetVirtualFunc<DestructorDelegate>(light, 0)(light, false);
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
