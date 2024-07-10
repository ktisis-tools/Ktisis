using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;

using Ktisis.GameData.Chara;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

using Ktisis.Services.Data;
using Ktisis.Structs.Characters;

using CharaMakeType = Ktisis.GameData.Excel.CharaMakeType;
using Tribe = Ktisis.Structs.Characters.Tribe;

namespace Ktisis.Editor.Characters.Make;

public class MakeTypeData {
	private readonly Dictionary<(Tribe, Gender), MakeTypeRace> MakeTypes = new();

	private CommonColors Colors = new();

	public MakeTypeRace? GetData(Tribe tribe, Gender gender) {
		lock (this.MakeTypes) {
			return this.MakeTypes.GetValueOrDefault((tribe, gender));
		}
	}

	public async Task Build(
		IDataManager data,
		CustomizeService discover
	) {
		var stop = new Stopwatch();
		stop.Start();
		await this.BuildMakeType(data);
		Ktisis.Log.Debug($"Built MakeType data in {stop.Elapsed.TotalMilliseconds:00.00}ms");
		await Task.WhenAll(
			this.PopulateDiscoveryData(discover),
			this.BuildColors(data)
		);
		stop.Stop();
		Ktisis.Log.Debug($"Total {stop.Elapsed.TotalMilliseconds:00.00}ms");
	}

	private async Task BuildMakeType(IDataManager data) {
		await Task.Yield();
		
		var stop = new Stopwatch();
		stop.Start();
		
		var sheet = data.GetExcelSheet<CharaMakeType>()!;
		foreach (var row in sheet)
			this.BuildRowCustomize(row);
		
		Ktisis.Log.Debug($"Built customize data in {stop.Elapsed.TotalMilliseconds:00.00}ms");
		stop.Restart();
		
		this.PopulateCustomizeIcons(data);
		
		stop.Stop();
		Ktisis.Log.Debug($"Populated customize icons in {stop.Elapsed.TotalMilliseconds:00.00}ms");
	}

	// Color utility

	public uint[] GetColors(CustomizeIndex index) => index switch {
		CustomizeIndex.EyeColor or CustomizeIndex.EyeColor2 => this.Colors.EyeColors,
		CustomizeIndex.HairColor2 => this.Colors.HighlightColors,
		CustomizeIndex.LipColor => this.Colors.LipColors,
		CustomizeIndex.FaceFeaturesColor => this.Colors.FaceFeatureColors,
		CustomizeIndex.FacepaintColor => this.Colors.FacepaintColors,
		_ => throw new Exception($"Invalid index {index} for color lookup.")
	};

	public uint[] GetColors(CustomizeIndex index, Tribe tribe, Gender gender) => index switch {
		CustomizeIndex.SkinColor => this.GetData(tribe, gender)?.Colors.SkinColors ?? [],
		CustomizeIndex.HairColor => this.GetData(tribe, gender)?.Colors.HairColors ?? [],
		_ => this.GetColors(index)
	};

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
			var start = feat.Params[0].Graphic - 2;
			var count = feat.Index == CustomizeIndex.HairStyle ? 99u : 49u;
			feat.Params = BuildParamFromCustomize(custom, start, count).ToArray();
		}
	}

	private static IEnumerable<MakeTypeParam> BuildParamFromCustomize(ExcelSheet<CharaMakeCustomize> custom, uint start, uint count) {
		for (var i = start; i < start + count; i++) {
			var row = custom.GetRow(i);
			if (row is null or { FeatureID: 0, Icon: 0 }) continue;
			yield return new MakeTypeParam {
				Value = row.FeatureID,
				Graphic = row.Icon
			};
		}
	}
	
	// Build color data
	
	private async Task BuildColors(IDataManager dataMgr) {
		await Task.Yield();

		var stop = new Stopwatch();
		stop.Start();

		var reader = CharaCmpReader.Open(dataMgr);
		this.Colors = reader.ReadCommon();

		IEnumerable<MakeTypeRace> makeTypes;
		lock (this.MakeTypes)
			makeTypes = this.MakeTypes.Values.ToList();

		foreach (var data in makeTypes)
			data.Colors = reader.ReadTribeData(data.Tribe, data.Gender);
		
		stop.Stop();
		Ktisis.Log.Debug($"Built color data in {stop.Elapsed.TotalMilliseconds:00.00}ms");
	}
	
	// Discover customize data

	private async Task PopulateDiscoveryData(CustomizeService discover) {
		await Task.Yield();

		var stop = new Stopwatch();
		stop.Start();
		
		IEnumerable<MakeTypeRace> makeTypes;
		lock (this.MakeTypes)
			makeTypes = this.MakeTypes.Values.ToList();

		foreach (var data in makeTypes) {
			var dataId = discover.CalcDataIdFor(data.Tribe, data.Gender);
			
			var face = data.GetFeature(CustomizeIndex.FaceType);
			if (face != null) {
				var faceIds = discover.GetFaceTypes(dataId)
					.Except(face.Params.Select(param => param.Value));
				if (data.Tribe is Tribe.Dunesfolk or Tribe.Hellsguard or Tribe.MoonKeeper)
					faceIds = faceIds.Except(face.Params.Select(param => (byte)(param.Value + 100)));
				ConcatFeatIds(face, faceIds);
			}

			var hair = data.GetFeature(CustomizeIndex.HairStyle);
			if (hair != null) {
				var hairIds = discover.GetHairTypes(dataId)
					.Except(hair.Params.Select(param => param.Value));
				ConcatFeatIds(hair, hairIds);
			}
		}
		
		stop.Stop();
		Ktisis.Log.Debug($"Populated discovery data in {stop.Elapsed.TotalMilliseconds:00.00}ms");
	}

	private static void ConcatFeatIds(MakeTypeFeature feat, IEnumerable<byte> ids) {
		feat.Params = feat.Params.Concat(
			ids.Select(id => new MakeTypeParam { Value = id, Graphic = 0 })
		).ToArray();
	}
}
