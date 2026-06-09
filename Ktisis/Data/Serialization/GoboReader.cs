using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ktisis.Data.Config.Gobos;

namespace Ktisis.Data.Serialization;

public class GoboReader {
	public static GoboSchema ReadStream(Stream stream) {
		var schema = new GoboSchema();
		schema.Gobos.AddRange(DeserializeGobos(stream));
		return schema;
	}

	private static List<GoboEntry> DeserializeGobos(Stream stream) {
		var streamReader = new StreamReader(stream, new UTF8Encoding());
		var result = new List<GoboEntry>();

		var line = streamReader.ReadLine();
		var split = line?.Split(",");
		if (line == null || split!.Length != 2) return result;

		line = streamReader.ReadLine();
		while (line != null) {
			if (line.Trim() == string.Empty) continue;

			split = line.Split(",");
			if (split.Length != 2) continue;

			result.Add(new GoboEntry{
				Path=split[0],
				Name=split[1]
			});
			line = streamReader.ReadLine();
		}

		return result;
	}
}
