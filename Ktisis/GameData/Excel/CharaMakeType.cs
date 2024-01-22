// ReSharper disable all
#pragma warning disable CS8618

using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;

namespace Ktisis.GameData.Excel;

// Temporarily forking CharaMakeType while FacialFeatureOption is bugged -
// It gets parsed as an int[8] rather than an int[8,7]. I've reached out to perchbird about this issue.

[Sheet( "CharaMakeType", columnHash: 0x80d7db6d )]
public partial class CharaMakeType : ExcelRow
{
    public struct CharaMakeStructStruct
    {
    	public LazyRow< Lobby > Menu { get; internal set; }
    	public uint SubMenuMask { get; internal set; }
    	public uint Customize { get; internal set; }
    	public uint[] SubMenuParam { get; internal set; }
    	public byte InitVal { get; internal set; }
    	public byte SubMenuType { get; internal set; }
    	public byte SubMenuNum { get; internal set; }
    	public byte LookAt { get; internal set; }
    	public byte[] SubMenuGraphic { get; internal set; }
    }
    public struct EquipmentStruct
    {
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
    public LazyRow< Race > Race { get; private set; }
    public LazyRow< Tribe > Tribe { get; private set; }
    public sbyte Gender { get; private set; }
    
    public override void PopulateData( RowParser parser, Lumina.GameData gameData, Language language )
    {
        base.PopulateData( parser, gameData, language );

        this.CharaMakeStruct = new CharaMakeStructStruct[28];
        for (int i = 0; i < 28; i++)
        {
        	this.CharaMakeStruct[i].Menu = new LazyRow< Lobby >( gameData, parser.ReadOffset< uint >( (ushort) (i * 428 + 0) ), language );
        	this.CharaMakeStruct[i].SubMenuMask = parser.ReadOffset< uint >( (ushort) (i * 428 + 4));
        	this.CharaMakeStruct[i].Customize = parser.ReadOffset< uint >( (ushort) (i * 428 + 8));
        	this.CharaMakeStruct[i].SubMenuParam = new uint[100];
        	for (int SubMenuParamIndexer = 0; SubMenuParamIndexer < 100; SubMenuParamIndexer++)
        		this.CharaMakeStruct[i].SubMenuParam[SubMenuParamIndexer] = parser.ReadOffset< uint >( (ushort) ( i * 428 + 12 + SubMenuParamIndexer * 4 ) );
        	this.CharaMakeStruct[i].InitVal = parser.ReadOffset< byte >( (ushort) (i * 428 + 412));
        	this.CharaMakeStruct[i].SubMenuType = parser.ReadOffset< byte >( (ushort) (i * 428 + 413));
        	this.CharaMakeStruct[i].SubMenuNum = parser.ReadOffset< byte >( (ushort) (i * 428 + 414));
        	this.CharaMakeStruct[i].LookAt = parser.ReadOffset< byte >( (ushort) (i * 428 + 415));
        	this.CharaMakeStruct[i].SubMenuGraphic = new byte[10];
        	for (int SubMenuGraphicIndexer = 0; SubMenuGraphicIndexer < 10; SubMenuGraphicIndexer++)
        		this.CharaMakeStruct[i].SubMenuGraphic[SubMenuGraphicIndexer] = parser.ReadOffset< byte >( (ushort) ( i * 428 + 416 + SubMenuGraphicIndexer * 1 ) );
        }
        this.VoiceStruct = new byte[12];
        for (int i = 0; i < 12; i++)
        	this.VoiceStruct[i] = parser.ReadOffset< byte >( 11984 + i * 1 );
        this.FacialFeatureOption = new int[8,7];
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 7; y++)
				this.FacialFeatureOption[x, y] = parser.ReadOffset<int>( 11996 + x * 28 + y * 4 );
		}
        this.Equipment = new EquipmentStruct[3];
        for (int i = 0; i < 3; i++)
        {
        	this.Equipment[i].Helmet = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12224));
        	this.Equipment[i].Top = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12232));
        	this.Equipment[i].Gloves = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12240));
        	this.Equipment[i].Legs = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12248));
        	this.Equipment[i].Shoes = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12256));
        	this.Equipment[i].Weapon = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12264));
        	this.Equipment[i].SubWeapon = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12272));
        }
        this.Race = new LazyRow< Race >( gameData, parser.ReadOffset< int >( 12392 ), language );
        this.Tribe = new LazyRow< Tribe >( gameData, parser.ReadOffset< int >( 12396 ), language );
        this.Gender = parser.ReadOffset< sbyte >( 12400 );
    }
}