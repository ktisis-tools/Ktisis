using System;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.World;
using Ktisis.Scene.Types;
using Ktisis.Structs.GPose;
using Ktisis.Structs.Lights;

namespace Ktisis.Scene.Modules.Lights;

public class LightModule : SceneModule {
	private readonly GroupPoseModule _gpose;
	private readonly IFramework _framework;
	private readonly LightSpawner _spawner;

	public LightModule(
		IHookMediator hook,
		ISceneManager scene,
		GroupPoseModule gpose,
		IFramework framework
	) : base(hook, scene) {
		this._gpose = gpose;
		this._framework = framework;
		this._spawner = hook.Create<LightSpawner>();
	}

	public override void Setup() {
		this.EnableAll();
		this.BuildLightEntities();
		this._spawner.TryInitialize();
	}

	private unsafe void BuildLightEntities() {
		var state = this._gpose.GetGPoseState();
		if (state == null) return;

		var lights = state->GetLights();
		for (var i = 0; i < lights.Length; i++) {
			var ptr = lights[i].Value;
			if (ptr == null) continue;
			this.AddLight(lights[i].Value, (uint)i);
		}
	}
	
	// Entities

	private unsafe void AddLight(SceneLight* light, uint index) {
		this.Scene.Factory.BuildLight()
			.SetName($"Camera Light {index + 1}")
			.SetAddress(light)
			.Add();
	}

	private unsafe void RemoveLight(SceneLight* light) {
		this.Scene.Children
			.FirstOrDefault(entity => entity is LightEntity lightEntity && lightEntity.Address == (nint)light)?
			.Remove();
	}
	
	// Update wrappers
	
	[Signature("40 53 48 83 EC 40 48 8B 99 ?? ?? ?? ??")]
	private SceneLightUpdateCullingDelegate _sceneLightUpdateCulling = null!;
	private unsafe delegate void SceneLightUpdateCullingDelegate(SceneLight* self);
	
	[Signature("40 53 48 83 EC 20 F6 81 ?? ?? ?? ?? ?? 48 8B D9 75 44 80 89 ?? ?? ?? ?? ?? B2 05")]
	private SceneLightUpdateMaterialsDelegate _sceneLightUpdateMaterials = null!;
	private unsafe delegate void SceneLightUpdateMaterialsDelegate(SceneLight* self);

	public unsafe void UpdateLightObject(LightEntity entity) {
		if (!this.IsInit || !entity.IsValid) return;
		var ptr = entity.GetObject();
		if (ptr != null) {
			this._sceneLightUpdateCulling(ptr);
			this._sceneLightUpdateMaterials(ptr);
		}
		entity.Flags &= ~LightEntityFlags.Update;
	}
	
	// Camera light hooks

	[Signature("48 83 EC 28 4C 8B C1 83 FA 03", DetourName = nameof(ToggleLightDetour))]
	private Hook<ToggleLightDelegate>? ToggleLightHook = null;
	private unsafe delegate bool ToggleLightDelegate(GPoseState* state, uint index);

	private unsafe bool ToggleLightDetour(GPoseState* state, uint index) {
		var result = false;
		
		try {
			var valid = this.CheckValid();
			
			var prev = valid ? state->GetLight(index) : null;

			result = this.ToggleLightHook!.Original(state, index);
			if (valid && result) {
				var light = state->GetLight(index);
				if (light != null && light != prev)
					this.AddLight(light, index);
				else if (light == null && prev != null)
					this.RemoveLight(prev);
			}
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle light toggle:\n{err}");
		}

		return result;
	}
	
	// Creation

	public async Task<LightEntity> Spawn() {
		return await this._framework.RunOnFrameworkThread(() => {
			var entity = this.CreateLight();
			if (entity == null)
				throw new Exception("Failed to create light entity.");
			return entity;
		});
	}

	private unsafe LightEntity? CreateLight() {
		var light = this._spawner.Create();
		if (light == null) return null;
		return this.Scene.Factory
			.BuildLight()
			.SetName("Light")
			.SetAddress(light)
			.Add();
	}
	
	// Removal

	public unsafe void Delete(LightEntity light) {
		var ptr = (SceneLight*)light.Address;
		light.Address = nint.Zero;
		light.Remove();
		if (ptr != null) this._spawner.Destroy(ptr);
	}
	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		this._spawner.Dispose();
		GC.SuppressFinalize(this);
	}
}
