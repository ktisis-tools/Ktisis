using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ExpressionBaker;

// Bakes the Ktisis FACS default catalogs from the Anamnesis expression-pose library.
//
// Usage (from repo root):
//   dotnet run --project tools/ExpressionBaker [expressionsRoot] [outputDir]
//
// It walks <root>/<Race>/<Gender>/[<Clan>/]<Face N>/ and emits one catalog per
// race+gender+clan into <outputDir>/<Race>_<Gender>[_<Clan>].json. Expression bone
// deltas are identical across face numbers within a clan (the face skeleton is
// shared), so one representative face folder per variant is enough — Ktisis picks
// the matching file from the actor's customize at runtime.
//
// Deltas are HEAD-RELATIVE (relative to j_kao), matching ExpressionEditor.
internal static class Program {
	private const string DefaultRoot =
		@"Anamnesis/StandardLibrary/Poses/Expressions";

	private const string NeutralName = "Straight Face";
	private const float AngleThresholdDeg = 1.0f;
	private const float PosThreshold = 0.0005f; // ~0.5mm head-relative

	// A bone's head-relative transform (position + rotation). Scale is not baked.
	private record Bone(Vector3 Pos, Quaternion Rot);

	private static readonly string[] ExcludeSubstr = { "noanim", "iris", "eyeprm", "eyepuru" };
	private static readonly string[] ExcludeExact = { "j_f_face", "j_f_eye_l", "j_f_eye_r" };

	// Id, Label, source expression, bone-name regex, split L/R, bidirectional, scale,
	// invertRight. invertRight negates the captured right-side delta: per-side capture
	// of the mirrored right-hand bones yields the INVERSE of the rig-correct mirror of
	// the left (Ktisis FlipPose mirrors a model rotation as (-x,-y,z,w)), so without
	// this the right slider drives the opposite way from the left. Only matters for
	// rotation-led bidirectional controls where the reversal is visible (e.g. brow).
	private record Atomic(string Id, string Label, string Source, string Pattern, bool Split, bool Bidirectional, float Scale = 1f, bool InvertRight = false);

	private static readonly Atomic[] Atomics = {
		new("BrowUp",     "Brow Up",     "Alert",     @"^j_f_(mayu|mmayu)_",                       true,  true,  1f, InvertRight: true),
		new("BrowFurrow", "Brow Furrow", "Furrow",    @"^j_f_(miken|dmiken|memoto|dmemoto)",       true,  false),
		new("Blink",      "Blink",       "Shut Eyes", @"^j_f_mab",                                 true,  false),
		new("EyeWide",    "Eye Wide",    "Amazed",    @"^j_f_mab",                                 true,  false),
		new("CheekRaise", "Cheek Raise", "Beam",      @"^j_f_(hoho|shoho|dhoho)_",                 true,  false),
		new("Smile",      "Smile",       "Smile",     @"^j_f_(ulip|dlip|umlip|dmlip|uslip|dslip)_", true,  false, 1.2f),
		new("Frown",      "Frown",       "Sad",       @"^j_f_(ulip|dlip|umlip|dmlip|uslip|dslip)_", true,  false, 1.2f),
		new("Sneer",      "Sneer",       "Sneer",     @"^j_f_(ulip|umlip|uslip)_|^j_f_hana_",      true,  false),
		new("JawOpen",    "Jaw Open",    "Ouch",      @"^j_f_(ago|dago|haguki)",                   false, false, 3f),
		new("UpperLipOpen", "Upper Lip Open", "Ouch", @"^j_f_(ulip|umlip)_",                       false, true,  5f),
		new("LowerLipOpen", "Lower Lip Open", "Ouch", @"^j_f_(dlip|dmlip)_",                       false, true,  5f),
		new("LipPucker",  "Lip Pucker",  "Pucker Up", @"^j_f_(ulip|dlip|umlip|dmlip|uslip|dslip)", false, false),
	};

	private static int Main(string[] args) {
		var root = args.Length > 0 ? args[0] : Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultRoot);
		var outDir = args.Length > 1
			? args[1]
			: Path.Combine(Directory.GetCurrentDirectory(), "Ktisis", "Data", "Library", "Expressions");

		if (!Directory.Exists(root)) {
			Console.Error.WriteLine($"Expressions root not found: {root}");
			return 1;
		}

		Directory.CreateDirectory(outDir);
		foreach (var stale in Directory.EnumerateFiles(outDir, "*.json"))
			File.Delete(stale);

		var variants = FindVariants(root).ToList();
		if (variants.Count == 0) {
			Console.Error.WriteLine("No variants found (expected <Race>/<Gender>/[<Clan>/]<Face N>/).");
			return 1;
		}

		var total = 0;
		foreach (var (key, faceFolder) in variants) {
			var units = BakeVariant(faceFolder);
			if (units == null) { Console.Error.WriteLine($"  ! {key}: no Straight Face / j_kao — skipped"); continue; }
			var root_ = new JsonObject {
				["Groups"] = new JsonArray { new JsonObject { ["Name"] = "Face", ["Units"] = units } }
			};
			var path = Path.Combine(outDir, key + ".json");
			File.WriteAllText(path, root_.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
			Console.WriteLine($"  {key,-40} {units.Count} AUs");
			total++;
		}

		Console.WriteLine($"Baked {total} variant catalog(s) -> {outDir}");
		return 0;
	}

	// Enumerate (key, representativeFaceFolder) for every race+gender[+clan].
	private static IEnumerable<(string key, string faceFolder)> FindVariants(string root) {
		foreach (var raceDir in Directory.GetDirectories(root).OrderBy(d => d)) {
			var race = Path.GetFileName(raceDir);
			foreach (var genderDir in Directory.GetDirectories(raceDir).OrderBy(d => d)) {
				var gender = Path.GetFileName(genderDir);
				var children = Directory.GetDirectories(genderDir).OrderBy(d => d).ToList();

				var faceDirs = children.Where(d => Path.GetFileName(d).StartsWith("Face ", StringComparison.OrdinalIgnoreCase)).ToList();
				if (faceDirs.Count > 0) {
					var rep = PickRepresentative(faceDirs);
					if (rep != null) yield return ($"{race}_{gender}", rep);
					continue;
				}

				foreach (var clanDir in children) {
					var clan = Path.GetFileName(clanDir);
					var clanFaces = Directory.GetDirectories(clanDir)
						.Where(d => Path.GetFileName(d).StartsWith("Face ", StringComparison.OrdinalIgnoreCase));
					var rep = PickRepresentative(clanFaces);
					if (rep != null) yield return ($"{race}_{gender}_{clan}", rep);
				}
			}
		}
	}

	private static string? PickRepresentative(IEnumerable<string> faceDirs)
		=> faceDirs.OrderBy(d => d).FirstOrDefault(d => File.Exists(Path.Combine(d, NeutralName + ".pose")));

	private static JsonArray? BakeVariant(string faceFolder) {
		var neutral = HeadRelative(LoadBones(Path.Combine(faceFolder, NeutralName + ".pose")));
		if (neutral == null) return null;

		var exprs = new Dictionary<string, Dictionary<string, Bone>>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in Directory.EnumerateFiles(faceFolder, "*.pose")) {
			var name = Path.GetFileNameWithoutExtension(file);
			if (name.Equals(NeutralName, StringComparison.OrdinalIgnoreCase)) continue;
			var rel = HeadRelative(LoadBones(file));
			if (rel != null) exprs[name] = rel;
		}

		var units = new JsonArray();
		foreach (var a in Atomics) {
			if (!exprs.TryGetValue(a.Source, out var rel)) continue;
			var re = new Regex(a.Pattern, RegexOptions.IgnoreCase);
			if (a.Split) {
				foreach (var (suffix, idSfx, lblSfx) in new[] { ("_l", "L", " (L)"), ("_r", "R", " (R)") }) {
					var bones = DeltaBones(neutral, rel, a.Scale, n => re.IsMatch(n) && n.EndsWith(suffix, StringComparison.Ordinal));
					if (a.InvertRight && idSfx == "R")
						bones = bones.ToDictionary(kv => kv.Key, kv => new Bone(-kv.Value.Pos, Quaternion.Inverse(kv.Value.Rot)));
					if (bones.Count > 0) units.Add(Unit(a.Id + idSfx, a.Label + lblSfx, bones, a.Bidirectional));
				}
			} else {
				var bones = DeltaBones(neutral, rel, a.Scale, n => re.IsMatch(n));
				if (bones.Count > 0) units.Add(Unit(a.Id, a.Label, bones, a.Bidirectional));
			}
		}
		return units;
	}

	private static JsonObject Unit(string id, string label, Dictionary<string, Bone> bones, bool bidir) {
		var bonesObj = new JsonObject();
		var usePos = false;
		foreach (var (name, b) in bones) {
			var entry = new JsonObject {
				["Rotation"] = new JsonObject {
					["X"] = Round(b.Rot.X), ["Y"] = Round(b.Rot.Y), ["Z"] = Round(b.Rot.Z), ["W"] = Round(b.Rot.W)
				}
			};
			if (b.Pos.LengthSquared() > 0f) {
				entry["Position"] = new JsonObject {
					["X"] = Round(b.Pos.X), ["Y"] = Round(b.Pos.Y), ["Z"] = Round(b.Pos.Z)
				};
				usePos = true;
			}
			bonesObj[name] = entry;
		}
		var u = new JsonObject { ["Id"] = id, ["Label"] = label };
		if (bidir) u["Bidirectional"] = true;
		if (usePos) u["UsePosition"] = true;
		u["Bones"] = bonesObj;
		return u;
	}

	private static double Round(float v) => Math.Round(v, 6);

	// Computes per-bone head-relative deltas (rotation + position) from neutral to
	// expr, filtered by name. A bone is included if either channel exceeds its
	// threshold; the unused channel is left at identity/zero. Rotation is scaled via
	// slerp extrapolation, position linearly, by the same factor.
	private static Dictionary<string, Bone> DeltaBones(
		Dictionary<string, Bone> neutral,
		Dictionary<string, Bone> expr,
		float scale,
		Func<string, bool> filter
	) {
		var thresh = AngleThresholdDeg * MathF.PI / 180f;
		var result = new Dictionary<string, Bone>();
		foreach (var (name, relExpr) in expr) {
			if (!name.StartsWith("j_f_", StringComparison.Ordinal)) continue;
			if (ExcludeExact.Contains(name) || ExcludeSubstr.Any(name.Contains)) continue;
			if (!filter(name)) continue;
			if (!neutral.TryGetValue(name, out var relNeutral)) continue;

			var d = Quaternion.Normalize(relExpr.Rot * Quaternion.Inverse(relNeutral.Rot));
			var angle = 2f * MathF.Acos(Math.Clamp(MathF.Abs(d.W), 0f, 1f));
			var posDelta = relExpr.Pos - relNeutral.Pos;

			var hasRot = angle >= thresh;
			var hasPos = posDelta.Length() >= PosThreshold;
			if (!hasRot && !hasPos) continue;

			if (MathF.Abs(scale - 1f) > 1e-4f) {
				d = Quaternion.Normalize(Quaternion.Slerp(Quaternion.Identity, d, scale));
				posDelta *= scale;
			}

			result[name] = new Bone(hasPos ? posDelta : Vector3.Zero, hasRot ? d : Quaternion.Identity);
		}
		return result;
	}

	private static Dictionary<string, Bone>? HeadRelative(Dictionary<string, Bone> bones) {
		if (!bones.TryGetValue("j_kao", out var head)) return null;
		var headRot = Quaternion.Normalize(head.Rot);
		var inv = Quaternion.Inverse(headRot);
		var result = new Dictionary<string, Bone>();
		foreach (var (n, b) in bones) {
			var relRot = Quaternion.Normalize(inv * Quaternion.Normalize(b.Rot));
			var relPos = Vector3.Transform(b.Pos - head.Pos, inv);
			result[n] = new Bone(relPos, relRot);
		}
		return result;
	}

	private static Dictionary<string, Bone> LoadBones(string path) {
		using var doc = JsonDocument.Parse(File.ReadAllText(path));
		var result = new Dictionary<string, Bone>();
		if (!doc.RootElement.TryGetProperty("Bones", out var bonesEl)) return result;
		foreach (var prop in bonesEl.EnumerateObject()) {
			var v = prop.Value;
			var rot = v.TryGetProperty("Rotation", out var r) && r.ValueKind == JsonValueKind.String
				? ParseQuat(r.GetString()!) : Quaternion.Identity;
			var pos = v.TryGetProperty("Position", out var p) && p.ValueKind == JsonValueKind.String
				? ParseVec(p.GetString()!) : Vector3.Zero;
			result[prop.Name] = new Bone(pos, rot);
		}
		return result;
	}

	private static Quaternion ParseQuat(string s) {
		var p = s.Split(',', StringSplitOptions.TrimEntries);
		return new Quaternion(
			float.Parse(p[0], CultureInfo.InvariantCulture),
			float.Parse(p[1], CultureInfo.InvariantCulture),
			float.Parse(p[2], CultureInfo.InvariantCulture),
			float.Parse(p[3], CultureInfo.InvariantCulture));
	}

	private static Vector3 ParseVec(string s) {
		var p = s.Split(',', StringSplitOptions.TrimEntries);
		return new Vector3(
			float.Parse(p[0], CultureInfo.InvariantCulture),
			float.Parse(p[1], CultureInfo.InvariantCulture),
			float.Parse(p[2], CultureInfo.InvariantCulture));
	}
}
