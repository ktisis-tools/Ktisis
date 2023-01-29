using System.Collections.Generic;

namespace Ktisis.Posing {
	public class BoneCategory {
		public string Name = "";

		public bool IsNsfw = false;

		public List<string> Bones = new();
		public List<BoneCategory> SubCategories = new();

		public BoneCategory? ParentCategory = null;

		internal int Order = 0;
	}
}