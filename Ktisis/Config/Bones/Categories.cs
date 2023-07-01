using System.Linq;
using System.Collections.Generic;

namespace Ktisis.Config.Bones;

public class Categories {
	public readonly List<BoneCategory> CategoryList = new();
	public readonly BoneCategory OtherCategory = new("Other");

	public void AddCategory(BoneCategory cat) {
		cat.SortPriority ??= CategoryList.Count;

		var prio = cat.SortPriority + 1;
		for (var i = 0; i < cat.Bones.Count; i++)
			cat.Bones[i].SortPriority ??= prio + i;

		CategoryList.Add(cat);
	}

	public BoneCategory? GetByName(string name)
		=> CategoryList.FirstOrDefault(cat => cat!.Name == name, null);
}
