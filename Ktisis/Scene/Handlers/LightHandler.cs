using System.Collections.Generic;

using Ktisis.Services;
using Ktisis.Scene.Objects.World;
using Ktisis.Interop.Structs.Event;
using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scene.Handlers; 

public class LightHandler {
	// Constructor
	
	private readonly GPoseService _gpose;
	
	private readonly SceneManager Manager;

	public LightHandler(SceneManager manager, GPoseService _gpose) {
        this._gpose = _gpose;
        
        this.Manager = manager;
        manager.OnSceneChanged += OnSceneChanged;
	}
	
	// Events

	private void OnSceneChanged(SceneGraph? scene) {
		if (scene is not null)
			scene.OnSceneUpdate += OnSceneUpdate;
		else
			this.GPoseLights.Clear();
	}
	
	// Update handler

	private readonly Dictionary<int, SceneLight> GPoseLights = new();

	private unsafe void OnSceneUpdate(SceneGraph scene, SceneContext _ctx) {
		var module = this._gpose.GetEventModule();
		if (module == null) return;

		for (var i = 0; i < GPoseModule.LightCount; i++) {
			var light = module->GetLight(i);
            
			if (this.GPoseLights.TryGetValue(i, out var prev)) {
				if (light.IsNullPointer || prev.Address != light.Address) {
					scene.Remove(prev);
					this.GPoseLights.Remove(i);
				} else continue;
			}

			if (light.IsNullPointer) continue;

			var lightObj = this.AddLight(light.Data);
			lightObj.Name = $"Camera Light {i + 1}";
			this.GPoseLights.Add(i, lightObj);
		}
	}
	
	// Lights

	public unsafe SceneLight AddLight(Light* ptr) {
		var light = new SceneLight((nint)ptr);
		this.Manager.Scene?.AddChild(light);
		return light;
	}
}