using System.Collections.Generic;

using Ktisis.Services;
using Ktisis.Scene.Objects.World;
using Ktisis.Interop.Structs.Event;
using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Scene.Handlers;

public class LightHandler {
	// Constructor

	private readonly GPoseService _gpose;

	private readonly SceneGraph Scene;

	public LightHandler(GPoseService _gpose, SceneGraph scene) {
		this._gpose = _gpose;
		this.Scene = scene;
		scene.OnSceneUpdate += OnSceneUpdate;
	}

	// Update handler

	private readonly Dictionary<int, SceneLight> GPoseLights = new();

	private unsafe void OnSceneUpdate(SceneGraph scene) {
		var module = this._gpose.GetEventModule();
		if (module == null) return;

		for (var i = 0; i < GPoseModule.LightCount; i++) {
			var light = module->GetLight(i);

			if (this.GPoseLights.TryGetValue(i, out var prev)) {
				if (light.IsNull || prev.Address != light.Address) {
					this.Scene.Remove(prev);
					this.GPoseLights.Remove(i);
				} else continue;
			}

			if (light.IsNull) continue;

			var lightObj = this.AddLight(light.Data);
			lightObj.Name = $"Camera Light {i + 1}";
			this.GPoseLights.Add(i, lightObj);
		}
	}

	// Lights

	public unsafe SceneLight AddLight(Light* ptr) {
		var light = new SceneLight((nint)ptr);
		this.Scene.AddChild(light);
		return light;
	}
}
