using System.Collections.Generic;

namespace Ktisis.Config.Bones; 

public class BoneCategory {
	public readonly string? Name;
	
	public bool IsNsfw;
	public int? SortPriority;

	public string? ParentCategory;

	public readonly List<BoneInfo> Bones = new();

	public BoneCategory(string name) => Name = name;
}