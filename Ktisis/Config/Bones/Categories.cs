using System.Linq;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;

namespace Ktisis.Config.Bones; 

public class Categories {
	public readonly List<BoneCategory> CategoryList = new();
	public BoneCategory? Default { get; internal set; }

	public void AddCategory(BoneCategory cat) {
		cat.SortPriority ??= CategoryList.Count;
		CategoryList.Add(cat);
	}
	
	// Category names

	public BoneCategory? GetByName(string name)
		=> CategoryList.Find(cat => cat!.Name == name);

	public BoneCategory? GetByNameOrDefault(string name)
		=> GetByName(name) ?? Default;
	
	// Category from bone name

	public BoneCategory? GetForBoneName(string name)
		=> CategoryList.Find(cat => cat.Bones.Any(bone => bone.Name == name));

	public BoneCategory? GetForBoneNameOrDefault(string name)
		=> GetForBoneName(name) ?? Default;

	public unsafe BoneCategory? ResolveBestCategory(hkaSkeleton* skeleton, int index) {
		if (skeleton == null)
			return null;

		while (index > -1) {
			var bone = skeleton->Bones[index];
			
			var name = bone.Name.String;
			if (name == null) break;

			var cat = GetForBoneName(name);
			if (cat != null) return cat;

			if (name.StartsWith("j_ex_h"))
				return GetByNameOrDefault("Hair");

			index = skeleton->ParentIndices[index];
		}

		return Default;
	}
}