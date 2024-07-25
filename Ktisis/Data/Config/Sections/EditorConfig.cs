using System.Collections.Generic;

using Ktisis.Data.Config.Entity;
using Ktisis.Scene.Types;

namespace Ktisis.Data.Config.Sections;

public class EditorConfig {
	// Values

	public bool OpenOnEnterGPose = true;
	
	public Dictionary<EntityType, EntityDisplay> Display = EntityDisplay.GetDefaults();
	
	// Transform Window

	public bool TransformHide = false;
	
	// Animation Tab

	public bool PlayEmoteStart = true;
	public bool ForceLoop = false;
	
	// Helpers

	public EntityDisplay GetDisplayForType(EntityType type)
		=> this.Display.GetValueOrDefault(type, new EntityDisplay());
}
