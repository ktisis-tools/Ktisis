using System.Collections.Generic;

using Ktisis.Core;
using Ktisis.Interop.Structs.Event;
using Ktisis.Interop.Structs.Objects;
using Ktisis.Scenes.Objects.World;

namespace Ktisis.Scenes.Managers;

public class LightManager : ManagerBase {
	public LightManager(Scene scene) : base(scene) { }

	// Update

	private Dictionary<int, SceneLight> GPoseLights = new();

	internal unsafe void Update() {
		var module = Services.Game.GPose.GetEventModule();
		if (module == null) return;

		for (var i = 0; i < GPoseModule.LightCount; i++) {
			var light = module->GetLight(i);

			var exists = GPoseLights.TryGetValue(i, out var prev);
			if (exists) {
				if (light == null || prev!.Address != (nint)light) {
					GPoseLights.Remove(i);
					prev!.RemoveFromParent();
				} else continue;
			}

			if (light == null) continue;

			var lightObj = AddLight(light);
			lightObj.Name = $"Camera Light {i + 1}";
			GPoseLights.Add(i, lightObj);
		}
	}

	// Lights

	public unsafe SceneLight AddLight(Light* light) {
		var result = new SceneLight((nint)light);
		Scene.AddChild(result);
		return result;
	}
}
