using System.Linq;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;

using Ktisis.Data.Config.Bones;

namespace Ktisis.Data.Config.Sections;

public class CategoryConfig {
	public readonly List<BoneCategory> CategoryList = new();
	public BoneCategory? Default { get; internal set; }

	public void AddCategory(BoneCategory category) {
		if (category.IsDefault)
			this.Default = category;
		category.SortPriority ??= this.CategoryList.Count;
		this.CategoryList.Add(category);
	}
	
	// Category names

	public BoneCategory? GetByName(string name)
		=> this.CategoryList.Find(category => category.Name == name);

	public BoneCategory? GetByNameOrDefault(string name)
		=> this.GetByName(name) ?? this.Default;
	
	// Category from bones

	public BoneCategory? GetForBoneName(string name)
		=> this.CategoryList.Find(category => category.Bones.Any(bone => bone.Name == name));

	public BoneCategory? GetForBoneNameOrDefault(string name)
		=> this.GetForBoneName(name) ?? this.Default;

	public unsafe BoneCategory? ResolveBestCategory(hkaSkeleton* skeleton, int index) {
		// TODO: SEPARATION OF CONCERNS. MOVE THIS ELSEWHERE.
		
		if (skeleton == null)
			return null;

		while (index > -1) {
			var bone = skeleton->Bones[index];

			var name = bone.Name.String;
			if (name == null) break;

			var category = this.GetForBoneName(name);
			if (category != null)
				return category;

			if (name.StartsWith("j_ex_h"))
				return this.GetByNameOrDefault("Hair"); // TODO: DO NOT LOOK THIS UP BY NAME

			index = skeleton->ParentIndices[index];
		}

		return this.Default;
	}
}
