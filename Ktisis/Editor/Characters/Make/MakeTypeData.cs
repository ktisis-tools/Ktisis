using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

using CharaMakeType = Ktisis.Data.Excel.CharaMakeType;
using Tribe = Ktisis.Structs.Characters.Tribe;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters.Make;

public class MakeTypeData {
	private readonly Dictionary<(Tribe, Gender), MakeTypeRace> MakeTypes = new();
	
	public async Task Build(IDataManager data) {
		await Task.Yield();
		var sheet = data.GetExcelSheet<CharaMakeType>()!;
		foreach (var row in sheet)
			this.BuildRowCustomize(row);
		this.PopulateCustomizeIcons(data);
	}

	public MakeTypeRace? GetData(Tribe tribe, Gender gender) {
		lock (this.MakeTypes) {
			return this.MakeTypes.GetValueOrDefault((tribe, gender));
		}
	}
	
	// Build sheet data

	private void BuildRowCustomize(CharaMakeType row) {
		var tribe = (Tribe)row.Tribe.Row;
		var gender = (Gender)row.Gender;

		var data = new MakeTypeRace(tribe, gender);
		lock (this.MakeTypes)
			this.MakeTypes[(tribe, gender)] = data;
		
		foreach (var makeStruct in row.CharaMakeStruct.Where(make => make.Customize != 0)) {
			var index = (CustomizeIndex)makeStruct.Customize;
			if (index == CustomizeIndex.FaceFeatures && data.Customize.ContainsKey(index))
				continue;
			
			var isCustomize = makeStruct is { SubMenuType: 1, SubMenuNum: > 10 };
			var paramData = BuildParamData(index, makeStruct, isCustomize);
			data.Customize[index] = new MakeTypeFeature {
				Name = makeStruct.Menu.Value?.Text ?? string.Empty,
				Index = index,
				Params = paramData.ToArray(),
				IsCustomize = isCustomize,
				IsIcon = makeStruct.SubMenuType == 1
			};
		}

		BuildRowFaceFeatures(row, data);
	}

	private static IEnumerable<MakeTypeParam> BuildParamData(CustomizeIndex index, CharaMakeType.CharaMakeStructStruct feature, bool isCustomize) {
		if (feature.SubMenuType > 1) yield break;
		
		var start = isCustomize && index == CustomizeIndex.Facepaint ? 1 : 0;
		var len = isCustomize ? start + 1 : feature.SubMenuNum;
		for (var i = start; i < len; i++) {
			var value = feature.SubMenuGraphic[i];
			var param = feature.SubMenuParam[i];
			yield return new MakeTypeParam {
				Value = value,
				Graphic = param
			};
		}
	}
	
	private static void BuildRowFaceFeatures(CharaMakeType row, MakeTypeRace data) {
		var face = data.GetFeature(CustomizeIndex.FaceType);
		if (face == null) return;

		var options = row.FacialFeatureOption;
		for (byte x = 0; x < face.Params.Length; x++) {
			var id = face.Params[x].Value;
			var icons = new uint[7];
			for (var y = 0; y < options.GetLength(1); y++)
				icons[y] = (uint)options[x, y];
			data.FaceFeatureIcons[id] = icons;
		}
	}
	
	// Populate customize data

	private void PopulateCustomizeIcons(IDataManager data) {
		var custom = data.GetExcelSheet<CharaMakeCustomize>()!;

		IEnumerable<MakeTypeFeature> features;
		lock (this.MakeTypes) {
			features = this.MakeTypes
				.SelectMany(make => make.Value.Customize.Values)
				.Where(feat => feat is { IsCustomize: true, Params.Length: > 0 })
				.ToList();
		}

		foreach (var feat in features) {
			var start = feat.Params[0].Graphic;
			var count = feat.Index == CustomizeIndex.HairStyle ? 100u : 50u;
			feat.Params = BuildParamFromCustomize(custom, start, count).ToArray();
		}
	}

	private static IEnumerable<MakeTypeParam> BuildParamFromCustomize(ExcelSheet<CharaMakeCustomize> custom, uint start, uint count) {
		for (var i = start; i < start + count; i++) {
			var row = custom.GetRow(i);
			if (row == null || row.FeatureID == 0) continue;
			yield return new MakeTypeParam {
				Value = row.FeatureID,
				Graphic = row.Icon
			};
		}
	}
}
