using System.Collections.Generic;

namespace Ktisis.Data.Config.Bones;

public class BoneCategory(string name) {
	public readonly string Name = name;
	
	public uint GroupColor = 0xFFFF9F68;
	public uint BoneColor = 0xFFFFFFFF;
	public bool LinkedColors;

	public bool IsNsfw;
	public bool IsDefault;
	
	public string? ParentCategory;
	public int? SortPriority;

	public readonly List<CategoryBone> Bones = new();
	
	public TwoJointsGroupParams? TwoJointsGroup;
	public CcdGroupParams? CcdGroup;
}
