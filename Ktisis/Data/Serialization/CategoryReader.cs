using System;
using System.IO;
using System.Linq;
using System.Xml;

using Dalamud.Utility;

using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Sections;

namespace Ktisis.Data.Serialization;

public static class CategoryReader {
	private const string BonesTag = "Bones";
	private const string CategoryTag = "Category";
	private const string TwoJointsIkTag = "TwoJointsIK";
	private const string CcdIkTag = "CcdIK";
	
	public static CategoryConfig ReadStream(Stream stream) {
		var categories = new CategoryConfig();
		
		using var reader = XmlReader.Create(stream);
		while (reader.Read()) {
			if (reader.NodeType != XmlNodeType.Element || reader.Name != CategoryTag)
				continue;
			ReadCategory(reader, categories);
		}
		
		return categories;
	}
	
	private static BoneCategory ReadCategory(XmlReader reader, CategoryConfig categories) {
		var name = reader.GetAttribute("Id") ?? "Unknown";
		var category = new BoneCategory(name) {
			IsNsfw = reader.GetAttribute("IsNsfw") == "true",
			IsDefault = reader.GetAttribute("IsDefault") == "true"
		};
		
		categories.AddCategory(category);

		while (reader.Read()) {
			switch (reader.NodeType) {
				case XmlNodeType.Element when reader.Name is CategoryTag:
					var sub = ReadCategory(reader, categories);
					sub.ParentCategory = category.Name;
					continue;
				case XmlNodeType.Element when reader.Name is BonesTag:
					ReadBone(reader, category);
					continue;
				case XmlNodeType.Element when reader.Name is TwoJointsIkTag:
					category.TwoJointsGroup = ReadTwoJointsIkGroup(reader);
					continue;
				case XmlNodeType.Element when reader.Name is CcdIkTag:
					category.CcdGroup = ReadCcdIkGroup(reader);
					continue;
				case XmlNodeType.EndElement when reader.Name is CategoryTag:
					return category;
				default:
					continue;
			}
		}

		return category;
	}
	
	// Individual bone names

	private static void ReadBone(XmlReader reader, BoneCategory category) {
		reader.Read();
		if (reader.NodeType != XmlNodeType.Text)
			return;

		var innerText = reader.Value;
		var bones = innerText.Split(null)
			.Select(ln => ln.Trim())
			.Where(ln => !ln.IsNullOrEmpty())
			.Select(bone => new CategoryBone(bone));
		
		category.Bones.AddRange(bones);
	}
	
	// IK groups

	private static TwoJointsGroupParams ReadTwoJointsIkGroup(XmlReader reader) {
		var group = new TwoJointsGroupParams {
			Type = reader.GetAttribute("Type") switch {
				"Arm" => TwoJointsType.Arm,
				"Leg" => TwoJointsType.Leg,
				_ => TwoJointsType.None
			}
		};
		
		while (reader.Read()) {
			if (reader is { NodeType: XmlNodeType.EndElement, Name: TwoJointsIkTag })
				break;

			if (reader.NodeType != XmlNodeType.Element)
				continue;
			
			var name = reader.Name;
			reader.Read();
			if (reader.NodeType != XmlNodeType.Text) continue;

			var list = name switch {
				"FirstBone" => group.FirstBone,
				"FirstTwist" => group.FirstTwist,
				"SecondBone" => group.SecondBone,
				"SecondTwist" => group.SecondTwist,
				"EndBone" => group.EndBone,
				_ => throw new Exception($"Encountered invalid IK bone parameter: {name}")
			};

			list.Add(reader.Value);
		}
		
		return group;
	}

	private static CcdGroupParams ReadCcdIkGroup(XmlReader reader) {
		var group = new CcdGroupParams();
		
		while (reader.Read()) {
			if (reader is { NodeType: XmlNodeType.EndElement, Name: CcdIkTag })
				break;

			if (reader.NodeType != XmlNodeType.Element)
				continue;
			
			var name = reader.Name;
			reader.Read();
			if (reader.NodeType != XmlNodeType.Text) continue;

			var list = name switch {
				"StartBone" => group.StartBone,
				"EndBone" => group.EndBone,
				_ => throw new Exception($"Encountered invalid IK bone parameter: {name}")
			};

			list.Add(reader.Value);
		}
		
		return group;
	}
}
