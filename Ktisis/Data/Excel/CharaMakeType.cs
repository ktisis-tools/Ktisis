// ReSharper disable All
#pragma warning disable CS8618

using UIntSpan = System.Span<uint>;

using Lumina;
using Lumina.Text;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;
using Lumina.Data.Structs.Excel;

namespace Ktisis.Data.Excel;

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
    
    public override void PopulateData( RowParser parser, GameData gameData, Language language )
    {
        base.PopulateData( parser, gameData, language );

        CharaMakeStruct = new CharaMakeStructStruct[28];
        for (int i = 0; i < 28; i++)
        {
        	CharaMakeStruct[i].Menu = new LazyRow< Lobby >( gameData, parser.ReadOffset< uint >( (ushort) (i * 428 + 0) ), language );
        	CharaMakeStruct[i].SubMenuMask = parser.ReadOffset< uint >( (ushort) (i * 428 + 4));
        	CharaMakeStruct[i].Customize = parser.ReadOffset< uint >( (ushort) (i * 428 + 8));
        	CharaMakeStruct[i].SubMenuParam = new uint[100];
        	for (int SubMenuParamIndexer = 0; SubMenuParamIndexer < 100; SubMenuParamIndexer++)
        		CharaMakeStruct[i].SubMenuParam[SubMenuParamIndexer] = parser.ReadOffset< uint >( (ushort) ( i * 428 + 12 + SubMenuParamIndexer * 4 ) );
        	CharaMakeStruct[i].InitVal = parser.ReadOffset< byte >( (ushort) (i * 428 + 412));
        	CharaMakeStruct[i].SubMenuType = parser.ReadOffset< byte >( (ushort) (i * 428 + 413));
        	CharaMakeStruct[i].SubMenuNum = parser.ReadOffset< byte >( (ushort) (i * 428 + 414));
        	CharaMakeStruct[i].LookAt = parser.ReadOffset< byte >( (ushort) (i * 428 + 415));
        	CharaMakeStruct[i].SubMenuGraphic = new byte[10];
        	for (int SubMenuGraphicIndexer = 0; SubMenuGraphicIndexer < 10; SubMenuGraphicIndexer++)
        		CharaMakeStruct[i].SubMenuGraphic[SubMenuGraphicIndexer] = parser.ReadOffset< byte >( (ushort) ( i * 428 + 416 + SubMenuGraphicIndexer * 1 ) );
        }
        VoiceStruct = new byte[12];
        for (int i = 0; i < 12; i++)
        	VoiceStruct[i] = parser.ReadOffset< byte >( 11984 + i * 1 );
        FacialFeatureOption = new int[8,7];
		for (int x = 0; x < 8; x++)
		{
			for (int y = 0; y < 7; y++)
				FacialFeatureOption[x, y] = parser.ReadOffset<int>( 11996 + x * 28 + y * 4 );
		}
        Equipment = new EquipmentStruct[3];
        for (int i = 0; i < 3; i++)
        {
        	Equipment[i].Helmet = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12224));
        	Equipment[i].Top = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12232));
        	Equipment[i].Gloves = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12240));
        	Equipment[i].Legs = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12248));
        	Equipment[i].Shoes = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12256));
        	Equipment[i].Weapon = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12264));
        	Equipment[i].SubWeapon = parser.ReadOffset< ulong >( (ushort) (i * 56 + 12272));
        }
        Race = new LazyRow< Race >( gameData, parser.ReadOffset< int >( 12392 ), language );
        Tribe = new LazyRow< Tribe >( gameData, parser.ReadOffset< int >( 12396 ), language );
        Gender = parser.ReadOffset< sbyte >( 12400 );
    }
}