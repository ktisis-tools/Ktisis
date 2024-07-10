using System;
using System.IO;

using Dalamud.Plugin.Services;

using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Chara;

public class CharaCmpReader(BinaryReader br) {
	private const string HumanCmpPath = "chara/xls/charamake/human.cmp";

	public static CharaCmpReader Open(IDataManager data) {
		var file = data.GetFile(HumanCmpPath);
		if (file == null)
			throw new Exception("Failed to open human.cmp");
		var stream = new MemoryStream(file.Data);
		var reader = new BinaryReader(stream);
		return new CharaCmpReader(reader);
	}
	
	// Features with alpha toggles use the full block size, everything else uses 192.
	private const int BlockLength = 256;
	private const int DataLength = 192;
	// Since we don't care about the transparent values, only read the ones with full alpha.
	private const int AlphaLength = 128;
	
	// 5 blocks each for colorsets and UI values.
	private const int CommonBlockCount = 5;
	private const int CommonBlockSize = CommonBlockCount * 2;
	
	// 3 blocks for colorsets followed by 2 blocks for UI values.
	private const int TribeBlockSkipCount = 3;
	private const int TribeBlockCount = 2;
	
	// Repeated twice for masc and femme models.
	private const int GenderBlockSize = sizeof(uint) * BlockLength * (TribeBlockSkipCount + TribeBlockCount);
	private const int TribeBlockSize = GenderBlockSize * 2;
	
	// Skips to values intended for display in UI.
	private const int CommonSeekTo = sizeof(uint) * BlockLength * CommonBlockCount;
	private const int TribesSeekTo = sizeof(uint) * BlockLength * (CommonBlockSize + TribeBlockSkipCount);

	// Special customize colors added for Dawntrail NPCs.
	// Extended hair colors not applicable to hrothgar.
	private const uint ExtendedDataLength = 208;
	
	// Read common data
	
	public CommonColors ReadCommon() {
		this.SeekTo(CommonSeekTo);

		var eyeColors = this.ReadArray(DataLength);
		this.SeekNextBlock();
		var highlightColors = this.ReadArray(ExtendedDataLength);
		this.SeekNextBlock();
		var lipColors = this.ReadArray(AlphaLength);
		this.SeekNextBlock();
		var raceFeatColors = this.ReadArray(ExtendedDataLength);
		this.SeekNextBlock();
		var facePaintColors = this.ReadArray(AlphaLength);
		this.SeekNextBlock();

		return new CommonColors {
			EyeColors = eyeColors,
			HighlightColors = highlightColors,
			LipColors = lipColors,
			FaceFeatureColors = raceFeatColors,
			FacepaintColors = facePaintColors
		};
	}
	
	// Read tribe data

	public TribeColors ReadTribeData(Tribe tribe, Gender gender) {
		this.SeekTo(TribesSeekTo + TribeBlockSize * (uint)(tribe - 1) + GenderBlockSize * (uint)gender);

		var skinColors = this.ReadArray(DataLength);
		this.SeekNextBlock();

		var isHairExtended = tribe is not (Tribe.Lost or Tribe.Helion);
		var hairColors = this.ReadArray(isHairExtended ? ExtendedDataLength : DataLength);

		return new TribeColors {
			SkinColors = skinColors,
			HairColors = hairColors
		};
	}
	
	// Read utilities
	
	private void SeekTo(uint offset) => br.BaseStream.Seek(offset, SeekOrigin.Begin);

	private uint[] ReadArray(uint length) {
		var result = new uint[length];
		for (var i = 0; i < length; i++)
			result[i] = br.ReadUInt32();
		return result;
	}

	private void SeekNextBlock() {
		const uint blockSize = BlockLength * sizeof(uint);
		var cursor = br.BaseStream.Position % blockSize;
		br.BaseStream.Seek(blockSize - cursor, SeekOrigin.Current);
	}
}
