using System.IO;
using System.Xml;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.Utility;

using Ktisis.Config.Bones;

namespace Ktisis.Data; 

public class SchemaReader {
	// Manifest resources

	private Stream GetManifestResource(string path) {
		var assembly = Assembly.GetExecutingAssembly();
		var name = assembly.GetName().Name!;
		path = $"{name}.{path}";
		
		var stream = assembly.GetManifestResourceStream(path);
		if (stream == null)
			throw new FileNotFoundException(path);

		return stream;
	}
	
	// Categories
	
	private const string BonesTag = "Bones";
	private const string CategoryTag = "Category";
	
	private const string CategorySchemaPath = "Data.Schema.Categories.xml";

	public async Task<Categories> ReadBoneCategories() {
		var result = new Categories();
		
		var stream = GetManifestResource(CategorySchemaPath);
		var settings = new XmlReaderSettings { Async = true };

		using var reader = XmlReader.Create(stream, settings);
		while (await reader.ReadAsync()) {
			if (reader.NodeType != XmlNodeType.Element || reader.Name != CategoryTag)
				continue;
			await RecursiveReadCategory(result, reader);
		}

		return result;
	}

	private async Task<BoneCategory> RecursiveReadCategory(Categories result, XmlReader reader) {
		var name = reader.GetAttribute("Id") ?? "Unknown";
		var category = new BoneCategory(name) {
			IsNsfw = reader.GetAttribute("IsNsfw") == "true",
			IsDefault = reader.GetAttribute("IsDefault") == "true"
		};

		if (category.IsDefault)
			result.Default = category;
		result.AddCategory(category);
		
		while (await reader.ReadAsync()) {
			switch (reader.NodeType) {
				case XmlNodeType.Element when reader.Name is CategoryTag:
					var sub = await RecursiveReadCategory(result, reader);
					sub.ParentCategory = category.Name;
					continue;
				case XmlNodeType.Element when reader.Name is BonesTag:
					await reader.ReadAsync();
					if (reader.NodeType != XmlNodeType.Text)
						continue;

					var innerText = await reader.GetValueAsync();
					var bones = innerText.Split(null)
						.Select(ln => ln.Trim())
						.Where(ln => !ln.IsNullOrEmpty());
					
					foreach (var bone in bones)
						category.Bones.Add(new BoneInfo(bone));
					
					continue;
				case XmlNodeType.EndElement when reader.Name is CategoryTag:
					return category;
				default:
					continue;
			}
		}
		
		return category;
	}
}
