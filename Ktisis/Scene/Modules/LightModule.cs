using System;
using System.Linq;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using Ktisis.Interop.Hooking;
using Ktisis.Interop.Structs.GPose;
using Ktisis.Interop.Structs.Lights;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Scene.Modules;

public class LightModule : SceneModule {
	public LightModule(
		IHookMediator hook,
		ISceneManager scene
	) : base(hook, scene) { }

	public override void Setup() {
		this.EnableAll();
		this.CreateLights();
	}

	private unsafe void CreateLights() {
		var state = this.GetGPoseState();
		if (state == null) return;

		var lights = state->GetLights();
		for (var i = 0; i < lights.Length; i++) {
			var ptr = lights[i].Value;
			if (ptr == null) continue;
			this.AddLight(lights[i].Value, (uint)i);
		}
	}
	
	// GPoseState

	public unsafe GPoseState* GetGPoseState()
		=> this._getGPoseState != null ? this._getGPoseState() : null;

	[Signature("E8 ?? ?? ?? ?? 0F B7 57 3C")]
	private GetGPoseStateDelegate? _getGPoseState = null;
	private unsafe delegate GPoseState* GetGPoseStateDelegate();
	
	// Hooks

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

	private unsafe void AddLight(SceneLight* light, uint index) {
		this.Scene.Factory.CreateLight()
			.SetName($"Camera Light {index + 1}")
			.SetAddress(light)
			.Add();
	}

	private unsafe void RemoveLight(SceneLight* light) {
		this.Scene.Children
			.FirstOrDefault(entity => entity is LightEntity lightEntity && lightEntity.Address == (nint)light)?
			.Remove();
	}
}
