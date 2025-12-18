// ReSharper disable all
#pragma warning disable CS8618

using System.Collections.Generic;

using Ktisis.Common.Extensions;

using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Ktisis.GameData.Excel;

// Temporarily forking CharaMakeType while FacialFeatureOption is bugged -
// It gets parsed as an int[8] rather than an int[8,7]. I've reached out to perchbird about this issue.

[Sheet( "CharaMakeType", columnHash: 0x80d7db6d)]
public partial struct CharaMakeType(ExcelPage page, uint offset, uint row) : IExcelRow<CharaMakeType> {
	public ExcelPage ExcelPage => page;
	public uint RowOffset { get; } = offset;
	public uint RowId { get; } = row;

	public struct CharaMakeStructStruct {
    	public RowRef<Lobby> Menu { get; internal set; }
    	public uint SubMenuMask { get; internal set; }
    	public uint Customize { get; internal set; }
    	public uint[] SubMenuParam { get; internal set; }
    	public byte InitVal { get; internal set; }
    	public byte SubMenuType { get; internal set; }
    	public byte SubMenuNum { get; internal set; }
    	public byte LookAt { get; internal set; }
    	public byte[] SubMenuGraphic { get; internal set; }
    }
    public struct EquipmentStruct {
    	public ulong Helmet { get; internal set; }
    	public ulong Top { get; internal set; }
    	public ulong Gloves { get; internal set; }
    	public ulong Legs { get; internal set; }
    	public ulong Shoes { get; internal set; }
    	public ulong Weapon { get; internal set; }
    	public ulong SubWeapon { get; internal set; }
    }
    
    public CharaMakeStructStruct[] CharaMakeStruct { get; private set; }
    public byte[] VoiceStruct { get; private set; }
    public int[,] FacialFeatureOption { get; private set; }
    public EquipmentStruct[] Equipment { get; private set; }
    public RowRef<Race> Race { get; private set; }
    public RowRef<Tribe> Tribe { get; private set; }
    public sbyte Gender { get; private set; }

	static CharaMakeType IExcelRow<CharaMakeType>.Create(ExcelPage page, uint offset, uint row) {
		var charaMakeStruct = new CharaMakeStructStruct[28];
		for (var i = 0; i < 28; i++) {
			charaMakeStruct[i].Menu = new RowRef<Lobby>(page.Module, page.ReadUInt32(offset + (ushort)(i * 428 + 0)), page.Language);
			charaMakeStruct[i].SubMenuMask = page.ReadUInt32(offset + (ushort)(i * 428 + 4));
			charaMakeStruct[i].Customize = page.ReadUInt32(offset + (ushort)(i * 428 + 8));
			charaMakeStruct[i].SubMenuParam = new uint[100];
			for (int SubMenuParamIndexer = 0; SubMenuParamIndexer < 100; SubMenuParamIndexer++)
				charaMakeStruct[i].SubMenuParam[SubMenuParamIndexer] = page.ReadUInt32(offset + (ushort)(i * 428 + 12 + SubMenuParamIndexer * 4));
			charaMakeStruct[i].InitVal = page.ReadUInt8(offset + (ushort)(i * 428 + 412));
			charaMakeStruct[i].SubMenuType = page.ReadUInt8(offset + (ushort)(i * 428 + 413));
			charaMakeStruct[i].SubMenuNum = page.ReadUInt8(offset + (ushort)(i * 428 + 414));
			charaMakeStruct[i].LookAt = page.ReadUInt8(offset + (ushort)(i * 428 + 415));
			charaMakeStruct[i].SubMenuGraphic = new byte[10];
			for (int SubMenuGraphicIndexer = 0; SubMenuGraphicIndexer < 10; SubMenuGraphicIndexer++)
				charaMakeStruct[i].SubMenuGraphic[SubMenuGraphicIndexer] = page.ReadUInt8(offset + (ushort)(i * 428 + 416 + SubMenuGraphicIndexer * 1));
		}

		var voiceStruct = new byte[12];
		for (var i = 0; i < 12; i++)
			voiceStruct[i] = page.ReadUInt8(offset + (ushort)(11984 + i * 1));

		var facialFeatureOption = new int[8,7];
		for (var x = 0; x < 8; x++) {
			for (var y = 0; y < 7; y++)
				facialFeatureOption[x, y] = page.ReadInt32( offset + (ushort)(11996 + x * 28 + y * 4));
		}

		var equipment = new EquipmentStruct[3];
		for (var i = 0; i < 3; i++) {
			equipment[i].Helmet = page.ReadUInt64(offset + (ushort)(i * 56 + 12224));
			equipment[i].Top = page.ReadUInt64(offset + (ushort)(i * 56 + 12232));
			equipment[i].Gloves = page.ReadUInt64(offset + (ushort)(i * 56 + 12240));
			equipment[i].Legs = page.ReadUInt64(offset + (ushort)(i * 56 + 12248));
			equipment[i].Shoes = page.ReadUInt64(offset + (ushort)(i * 56 + 12256));
			equipment[i].Weapon = page.ReadUInt64(offset + (ushort)(i * 56 + 12264));
			equipment[i].SubWeapon = page.ReadUInt64(offset + (ushort)(i * 56 + 12272));
		}
		
		return new CharaMakeType(page, offset, row) {
			CharaMakeStruct = charaMakeStruct,
			VoiceStruct = voiceStruct,
			FacialFeatureOption = facialFeatureOption,
			Equipment = equipment,
			Race = page.ReadRowRef<Race>(0, offset),
			Tribe = page.ReadRowRef<Tribe>(1, offset),
			Gender = page.ReadColumn<sbyte>(2, offset)
		};
	}
}