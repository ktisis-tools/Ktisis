using System.Collections.Generic;

using Dalamud.Interface;

using Ktisis.Scene.Types;

namespace Ktisis.Data.Config.Entity;

public enum DisplayMode {
	None = 0,
	Dot,
	Icon
}

public record EntityDisplay {
	public uint Color;
	public FontAwesomeIcon Icon;
	public DisplayMode Mode;

	public EntityDisplay(
		uint color = 0xFFFFFFFF,
		FontAwesomeIcon icon = FontAwesomeIcon.None,
		DisplayMode mode = DisplayMode.Icon
	) {
		this.Color = color;
		this.Icon = icon;
		this.Mode = mode;
	}
	
	private const uint BoneBlue = 0xFFFF9F68;
	private const uint ModelMint = 0xFFBAFFB2;
	private const uint LightLemon = 0xFF68EDFF;
	
	public static Dictionary<EntityType, EntityDisplay> GetDefaults() => new() {
		{ EntityType.Invalid, new EntityDisplay() },
		{ EntityType.Actor, new EntityDisplay(icon: FontAwesomeIcon.Child) },
		{ EntityType.Armature, new EntityDisplay(color: BoneBlue, icon: FontAwesomeIcon.CircleNodes) },
		{ EntityType.BoneGroup, new EntityDisplay(color: BoneBlue, mode: DisplayMode.None) },
		{ EntityType.BoneNode, new EntityDisplay(mode: DisplayMode.Dot) }, // May deprecate this in future for display of category colors.
		{ EntityType.Models, new EntityDisplay(color: ModelMint, icon: FontAwesomeIcon.CubesStacked) },
		{ EntityType.ModelSlot, new EntityDisplay(color: ModelMint) },
		{ EntityType.Weapon, new EntityDisplay(icon: FontAwesomeIcon.Magic) },
		{ EntityType.Light, new EntityDisplay(color: LightLemon, icon: FontAwesomeIcon.Lightbulb) },
		{ EntityType.RefImage, new EntityDisplay(icon: FontAwesomeIcon.Image) }
	};
}
