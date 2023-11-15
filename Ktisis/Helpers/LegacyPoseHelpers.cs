using System;
using System.Linq;

using Ktisis.Data.Files;

namespace Ktisis.Helpers {
	public static class LegacyPoseHelpers {
		public static string ConvertLegacyPose(string file) {
			var  result = "{\n";
			result += "\t\"FileExtension\": \".pose\",\n";
			result += "\t\"TypeName\": \"Anamnesis Pose\",\n";
			result += "\t\"Position\": \"0, 0, 0\",\n";
			result += "\t\"Rotation\": \"0, 0, 0, 1\",\n";
			result += "\t\"Scale\": \"1, 1, 1\",\n";
			result += "\t\"Bones\": {\n";
			
			var lines = file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.TrimEntries);
			
			for (var i = 7; i < lines.Length - 1; i++) {
				result += ConvertLegacyBone(lines[i]);
			}
			
			// Length - 2 removes the trailing comma and, incorrectly, an extra curly bracket. Length - 1 doesn't remove the comma at all. Beats me.
			result = result.Substring(0, result.Length - 2);
			result += "}\n\t}";
			return result;
		}

		private static string? ConvertLegacyBone(string bone) {
			var boneString = "";
			var boneName = bone.Split(new char[] {':'}, 2)[0].Replace("\"", "");
			var boneRotation = bone.Split(new char[] {':'}, 2)[1].Replace("\"", "").Replace(",", "").Replace(" ", "");
			
			if (!PoseFile.LegacyConversions.ContainsKey(boneName) || boneRotation.Contains("null")) return null;
			
			var boneRotationValues = new float[4];
			for (var i = 0; i < 4; i++) {
				var axisValue = Convert.ToInt32(boneRotation.Substring(i * 8, 8), 16);
				boneRotationValues[i] = BitConverter.ToSingle(BitConverter.GetBytes(axisValue).Reverse().ToArray(), 0);
			}
			
			boneString += "\t\t\"" + boneName + "\": {\n";
			boneString += "\t\t\t\"Position\": \"0, 0, 0\",\n";
			boneString += "\t\t\t\"Rotation\": \"" + boneRotationValues[0] + ", " + boneRotationValues[1] + ", " + boneRotationValues[2] + ", " + boneRotationValues[3] + "\",\n";
			boneString += "\t\t\t\"Scale\": \"1, 1, 1\"\n";
			boneString += "\t\t},\n";
			return boneString;
		}
	}
}