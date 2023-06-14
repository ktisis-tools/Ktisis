using System;

namespace Ktisis.Data.Files {
	[Serializable]
	public class JsonFile {
		public string FileExtension { get; set; } = ".json";
		public string TypeName { get; set; } = "Json File";

		public int Version { get; set; } = 1;
	}
}
