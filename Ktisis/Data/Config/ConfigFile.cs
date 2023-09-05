using System.Collections.Generic;
using System.Threading.Tasks;

using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Display;

namespace Ktisis.Data.Config; 

public class ConfigFile {
	// Config values

	public Categories? Categories;

	public Dictionary<ItemType, ItemDisplay>? Display;
	
	// Load

	public async static Task<ConfigFile> Load(SchemaReader schema) {
		// TODO
		var cfg = new ConfigFile();
		cfg.Setup();
		
		cfg.Categories ??= await schema.ReadBoneCategories();
		
		return cfg;
	}

	private void Setup() {
		// ItemDisplay
		
		var itemsDefault = ItemDisplay.GetDefaults();
		if (this.Display is null) {
			this.Display = itemsDefault;
		} else {
			foreach (var (key, val) in itemsDefault)
				this.Display.TryAdd(key, val);
		}
	}
	
	// ItemDisplay

	public ItemDisplay GetItemDisplay(ItemType type)
		=> this.Display?.GetValueOrDefault(type) ?? new ItemDisplay();
}
