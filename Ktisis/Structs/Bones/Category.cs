using System.Numerics;

namespace Ktisis.Structs.Bones
{
	public class Category
	{
		public string Name { get; set; }
		public Vector4 DefaultColor { get; set; }
		public Category(string name, Vector4 defaultColor)
		{
			this.Name = name;
			DefaultColor = defaultColor;
		}

		internal void Deconstruct(out string name, out Vector4 defaultColor)
		{
			name = Name;
			defaultColor = DefaultColor;
		}
		internal void Deconstruct(out string name) => name = Name;

	}
}