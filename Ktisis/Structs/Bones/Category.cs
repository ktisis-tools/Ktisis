using System.Collections.Generic;
using System.Numerics;

namespace Ktisis.Structs.Bones
{
	public class Category
	{
		public string Name { get; set; }
		public Vector4 DefaultColor { get; set; }
		public readonly List<string> Bones = new();


		public static readonly Dictionary<string,Category> Categories = new() {
			{"body"      ,new Category("body"      , new Vector4(1.0F, 0.0F, 0.0F, 0.5647059F))},
			{"head"      ,new Category("head"      , new Vector4(0.0F, 1.0F, 0.0F, 0.5647059F))},
			{"hair"      ,new Category("hair"      , new Vector4(0.0F, 0.0F, 1.0F, 0.5647059F))},
			{"clothes"   ,new Category("clothes"   , new Vector4(1.0F, 1.0F, 0.0F, 0.5647059F))},
			{"right hand",new Category("right hand", new Vector4(1.0F, 0.0F, 1.0F, 0.5647059F))},
			{"left hand" ,new Category("left hand" , new Vector4(0.0F, 1.0F, 1.0F, 0.5647059F))},
			{"tail"      ,new Category("tail"      , new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F))},
			{"ears"      ,new Category("ears"      , new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F))},
			{"feet"      ,new Category("feet"      , new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F))},
		};

		public Category(string name, Vector4 defaultColor)
		{
			this.Name = name;
			DefaultColor = defaultColor;
		}

		public bool IsEmpty() => Bones.Count == 0;

		public void RegisterBone(string boneName)
		{
			if(!Bones.Contains(boneName))
				Bones.Add(boneName);
			//PluginLog.Debug($"Bone count:{Bones.Count}");
		}

		internal void Deconstruct(out string name, out Vector4 defaultColor)
		{
			name = Name;
			defaultColor = DefaultColor;
		}
		internal void Deconstruct(out string name) => name = Name;

	}
}