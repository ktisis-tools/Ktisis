using System.Collections.Generic;

using Dalamud.Interface;

namespace Ktisis.Config.Display;

public enum ItemType {
	Default = 0,
	Actor,
	Armature,
	BoneGroup,
	BoneNode,
	Models,
	ModelSlot,
	Weapon,
	Light
}

public enum DisplayMode {
	None,
	Dot,
	Icon
}

public class ItemDisplay {
	public uint Color;
	public FontAwesomeIcon Icon;
	public DisplayMode Mode;

	public ItemDisplay(
		uint color = 0xFFFFFFFF,
		FontAwesomeIcon icon = FontAwesomeIcon.None,
		DisplayMode mode = DisplayMode.Icon
	) {
		this.Color = color;
		this.Icon = icon;
		this.Mode = mode;
	}

	// Technically this could be a Dict<Type, _>, but I'm doing this in case we want objects to share an ItemDisplay in future.

	private const uint BoneBlue = 0xFFFF9F68;
	private const uint ModelMint = 0xFFBAFFB2;

	// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident
	public static Dictionary<ItemType, ItemDisplay> GetDefaults() => new() {
		{ ItemType.Default,		new() },
		{ ItemType.Actor,		new(icon: FontAwesomeIcon.Child) },
		{ ItemType.Armature,	new(color: BoneBlue, icon: FontAwesomeIcon.CircleNodes) },
		{ ItemType.BoneGroup,	new(color: BoneBlue, mode: DisplayMode.None) },
		{ ItemType.BoneNode,	new(mode: DisplayMode.Dot) }, // May deprecate this in future for display of category colors.
		{ ItemType.Models,		new(color: ModelMint, icon: FontAwesomeIcon.CubesStacked) },
		{ ItemType.ModelSlot,	new(color: ModelMint) },
		{ ItemType.Weapon,		new(icon: FontAwesomeIcon.Magic) },
		{ ItemType.Light,		new(color: 0xFF68EDFF, icon: FontAwesomeIcon.Lightbulb) }
	};
}
