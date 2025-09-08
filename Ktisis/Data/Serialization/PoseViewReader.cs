using System.Globalization;
using System.IO;
using System.Numerics;
using System.Xml;

using Ktisis.Data.Config.Pose2D;

namespace Ktisis.Data.Serialization;

public static class PoseViewReader {
	private const string ViewTag = "View";
	private const string ImageTag = "Image";
	private const string BoneTag = "Bone";
	
	public static PoseViewSchema ReadStream(Stream stream) {
		var schema = new PoseViewSchema();

		using var reader = XmlReader.Create(stream);
		while (reader.Read()) {
			if (reader.NodeType != XmlNodeType.Element || reader.Name != ViewTag)
				continue;
			
			var view = ReadView(reader, schema);
			schema.Views.Add(view.Name, view);
		}
		
		return schema;
	}

	private static PoseViewEntry ReadView(XmlReader reader, PoseViewSchema schema) {
		var view = new PoseViewEntry {
			Name = reader.GetAttribute("name") ?? "INVALID"
		};

		while (reader.Read()) {
			switch (reader.NodeType) {
				case XmlNodeType.Element when reader.Name is ImageTag:
					var file = reader.GetAttribute("file");
					if (file != null) view.Images.Add(file);
					continue;
				case XmlNodeType.Element when reader.Name is BoneTag:
					if (!float.TryParse(reader.GetAttribute("x"), CultureInfo.InvariantCulture, out var x))
						x = 0.0f;
					if (!float.TryParse(reader.GetAttribute("y"), CultureInfo.InvariantCulture, out var y))
						y = 0.0f;
					var bone = new PoseViewBone {
						Label = reader.GetAttribute("label") ?? string.Empty,
						Name = reader.GetAttribute("name") ?? string.Empty,
						Position = new Vector2(x, y)
					};
					view.Bones.Add(bone);
					continue;
				case XmlNodeType.EndElement when reader.Name is ViewTag:
					return view;
				default:
					continue;
			}
		}

		return view;
	}
}
