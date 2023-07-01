using System.Threading.Tasks;

using Ktisis.Core;
using Ktisis.Config.Bones;

namespace Ktisis.Config;

public class ConfigFile {
	// Config values

	public Categories? Categories;

	// Loading and creation

	public async static Task<ConfigFile> Load() {
		// TODO
		var cfg = new ConfigFile();

		cfg.Categories ??= await Services.Data.Schema.ReadBoneCategories();

		return cfg;
	}
}
