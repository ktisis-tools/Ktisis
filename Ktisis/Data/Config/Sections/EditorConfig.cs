using System.Collections.Generic;

using Ktisis.Data.Config.Entity;
using Ktisis.Scene.Entities.Utility;
using Ktisis.Scene.Types;

namespace Ktisis.Data.Config.Sections;

public class EditorConfig {
	// Workspace

	public bool OpenOnEnterGPose = true;

	public bool ToggleEditorOnSelect = true;

	public bool UseLegacyWindowBehavior = false;
	public bool UseLegacyPoseViewTabs = false;
	public bool UseLegacyLightEditor = false;
	
	public Dictionary<EntityType, EntityDisplay> Display = EntityDisplay.GetDefaults();

	public bool LinkedGaze = false;
	
	// Reference images
	
	public List<ReferenceImage.SetupData> ReferenceImages = new();
	
	// Transform Window

	public bool TransformHide = false;
	
	// Animation Tab

	public bool PlayEmoteStart = true;
	public bool ForceLoop = true;
	
	// Helpers

	public EntityDisplay GetDisplayForType(EntityType type)
		=> this.Display.GetValueOrDefault(type, new EntityDisplay());
}
