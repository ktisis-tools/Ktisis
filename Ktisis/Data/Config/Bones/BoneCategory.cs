using System.Collections.Generic;

namespace Ktisis.Data.Config.Bones;

public class BoneCategory {
	public readonly string? Name;

	public bool IsNsfw;
	public bool IsDefault;

	public string? ParentCategory;
	public int? SortPriority;

	public readonly List<BoneInfo> Bones = new();

	public BoneCategory(string name) => this.Name = name;
}
