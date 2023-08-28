using System.Threading.Tasks;

using Ktisis.Data.Config.Bones;

namespace Ktisis.Data.Config; 

public class ConfigFile {
	// Config values

	public Categories? Categories;
	
	// Load

	public async static Task<ConfigFile> Load(SchemaReader schema) {
		// TODO
		var cfg = new ConfigFile();

		cfg.Categories ??= await schema.ReadBoneCategories();

		return cfg;
	}
}