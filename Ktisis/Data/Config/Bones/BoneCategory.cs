using System.Collections.Generic;

namespace Ktisis.Data.Config.Bones;

public class BoneCategory(string name) {
	public readonly string Name = name;

	public bool IsNsfw;
	public bool IsDefault;
	
	public string? ParentCategory;
	public int? SortPriority;

	public readonly List<CategoryBone> Bones = new();
	
	public TwoJointsIkGroup? TwoJointsGroup;
}
