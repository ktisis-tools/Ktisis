using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ktisis.Structs.Bones {
	public class BoneCategories {


		private static Category FindCategory(string categoryName)
		{
			if(!Category.Categories.TryGetValue(categoryName, out Category? category))
			{
				Category.Categories.Add(categoryName, new Category(categoryName, new Vector4(1.0F, 1.0F, 1.0F, 0.5647059F)));
				category = Category.Categories[categoryName];
			}
			return category;
		}

		public static Category DefaultCategory => FindCategory("custom");


		public static readonly Dictionary<string, string> BonesCategoriesAssociation = new() {
			{ "n_root"          , "body"}, // Root
			{ "n_hara"          , "body"}, // Abdomen
			{ "n_throw"         , "body"}, // Throw
			{ "j_kosi"          , "body"}, // Waist
			{ "j_sebo_a"        , "body"}, // SpineA
			{ "j_asi_a_l"       , "body"}, // LegLeft
			{ "j_asi_a_r"       , "body"}, // LegRight
			{ "j_sebo_b"        , "body"}, // SpineB
			{ "j_asi_b_l"       , "body"}, // KneeLeft
			{ "j_asi_b_r"       , "body"}, // KneeRight
			{ "j_mune_l"        , "body"}, // BreastLeft
			{ "j_mune_r"        , "body"}, // BreastRight
			{ "j_sebo_c"        , "body"}, // SpineC
			{ "j_asi_c_l"       , "body"}, // CalfLeft
			{ "j_asi_c_r"       , "body"}, // CalfRight
			{ "j_kubi"          , "body"}, // Neck
			{ "j_sako_l"        , "body"}, // ClavicleLeft
			{ "j_sako_r"        , "body"}, // ClavicleRight
			{ "j_asi_d_l"       , "body"}, // FootLeft
			{ "j_asi_d_r"       , "body"}, // FootRight
			//{ "j_kao"         , "body"}, // Head
			{ "j_ude_a_l"       , "body"}, // ArmLeft
			{ "j_ude_a_r"       , "body"}, // ArmRight
			{ "j_asi_e_l"       , "body"}, // ToesLeft
			{ "j_asi_e_r"       , "body"}, // ToesRight
			{ "j_ude_b_l"       , "body"}, // ForearmLeft
			{ "j_ude_b_r"       , "body"}, // ForearmRight
			{ "n_hkata_l"       , "body"}, // ShoulderLeft
			{ "n_hkata_r"       , "body"}, // ShoulderRight
			{ "j_te_l"          , "body"}, // HandLeft
			{ "j_te_r"          , "body"}, // HandRight
			{ "n_hhiji_l"       , "body"}, // ElbowLeft
			{ "n_hhiji_r"       , "body"}, // ElbowRight

			{ "j_sk_b_b_l"      , "clothes"}, // ClothBackBLeft
			{ "j_sk_b_b_r"      , "clothes"}, // ClothBackBRight
			{ "j_sk_f_b_l"      , "clothes"}, // ClothFrontBLeft
			{ "j_sk_f_b_r"      , "clothes"}, // ClothFrontBRight
			{ "j_sk_s_b_l"      , "clothes"}, // ClothSideBLeft
			{ "j_sk_s_b_r"      , "clothes"}, // ClothSideBRight
			{ "j_buki_sebo_l"   , "clothes"}, // ScabbardLeft
			{ "j_buki_sebo_r"   , "clothes"}, // ScabbardRight
			{ "j_buki2_kosi_l"  , "clothes"}, // HolsterLeft
			{ "j_buki2_kosi_r"  , "clothes"}, // HolsterRight
			{ "j_buki_kosi_l"   , "clothes"}, // SheatheLeft
			{ "j_buki_kosi_r"   , "clothes"}, // SheatheRight
			{ "j_sk_b_a_l"      , "clothes"}, // ClothBackALeft
			{ "j_sk_b_a_r"      , "clothes"}, // ClothBackARight
			{ "j_sk_f_a_l"      , "clothes"}, // ClothFrontALeft
			{ "j_sk_f_a_r"      , "clothes"}, // ClothFrontARight
			{ "j_sk_s_a_l"      , "clothes"}, // ClothSideALeft
			{ "j_sk_s_a_r"      , "clothes"}, // ClothSideARight
			{ "j_sk_b_c_l"      , "clothes"}, // ClothBackCLeft
			{ "j_sk_b_c_r"      , "clothes"}, // ClothBackCRight
			{ "j_sk_f_c_l"      , "clothes"}, // ClothFrontCLeft
			{ "j_sk_f_c_r"      , "clothes"}, // ClothFrontCRight
			{ "j_sk_s_c_l"      , "clothes"}, // ClothSideCLeft
			{ "j_sk_s_c_r"      , "clothes"}, // ClothSideCRight
			{ "n_hizasoubi_l"   , "clothes"}, // PoleynLeft
			{ "n_hizasoubi_r"   , "clothes"}, // PoleynRight
			{ "n_kataarmor_l"   , "clothes"}, // PauldronLeft
			{ "n_kataarmor_r"   , "clothes"}, // PauldronRight
			{ "n_buki_tate_l"   , "clothes"}, // ShieldLeft
			{ "n_buki_tate_r"   , "clothes"}, // ShieldRight
			{ "n_hijisoubi_l"   , "clothes"}, // CouterLeft
			{ "n_hijisoubi_r"   , "clothes"}, // CouterRight
			{ "n_ear_a_l"       , "clothes"}, // EarringALeft
			{ "n_ear_a_r"       , "clothes"}, // EarringARight
			{ "n_ear_b_l"       , "clothes"}, // EarringBLeft
			{ "n_ear_b_r"       , "clothes"}, // EarringBRight

			{ "j_kami_a"        , "hair"}, // HairA
			{ "j_kami_f_l"      , "hair"}, // HairFrontLeft
			{ "j_kami_f_r"      , "hair"}, // HairFrontRight
			{ "j_kami_b"        , "hair"}, // HairB
			{ "j_ex_met_va"     , "hair"}, // HairB

			// hands
			{ "n_hte_r"         , "right hand"}, // WristRight
			{ "j_hito_a_r"      , "right hand"}, // IndexARight
			{ "j_ko_a_r"        , "right hand"}, // PinkyARight
			{ "j_kusu_a_r"      , "right hand"}, // RingARight
			{ "j_naka_a_r"      , "right hand"}, // MiddleARight
			{ "j_oya_a_r"       , "right hand"}, // ThumbARight
			{ "n_buki_r"        , "right hand"}, // WeaponRight
			{ "j_hito_b_r"      , "right hand"}, // IndexBRight
			{ "j_ko_b_r"        , "right hand"}, // PinkyBRight
			{ "j_kusu_b_r"      , "right hand"}, // RingBRight
			{ "j_naka_b_r"      , "right hand"}, // MiddleBRight
			{ "j_oya_b_r"       , "right hand"}, // ThumbBRight


			{ "n_hte_l"         , "left hand"}, // WristLeft
			{ "j_hito_a_l"      , "left hand"}, // IndexALeft
			{ "j_ko_a_l"        , "left hand"}, // PinkyALeft
			{ "j_kusu_a_l"      , "left hand"}, // RingALeft
			{ "j_naka_a_l"      , "left hand"}, // MiddleALeft
			{ "j_oya_a_l"       , "left hand"}, // ThumbALeft
			{ "n_buki_l"        , "left hand"}, // WeaponLeft
			{ "j_hito_b_l"      , "left hand"}, // IndexBLeft
			{ "j_ko_b_l"        , "left hand"}, // PinkyBLeft
			{ "j_kusu_b_l"      , "left hand"}, // RingBLeft
			{ "j_naka_b_l"      , "left hand"}, // MiddleBLeft
			{ "j_oya_b_l"       , "left hand"}, // ThumbBLeft

			// tail
			{ "n_sippo_a"       , "tail"}, // TailA
			{ "n_sippo_b"       , "tail"}, // TailB
			{ "n_sippo_c"       , "tail"}, // TailC
			{ "n_sippo_d"       , "tail"}, // TailD
			{ "n_sippo_e"       , "tail"}, // TailE

			// Head
			{ "j_kao"           , "head"}, // RootHead
			{ "j_ago"           , "head"}, // Jaw
			{ "j_f_dmab_l"      , "head"}, // EyelidLowerLeft
			{ "j_f_dmab_r"      , "head"}, // EyelidLowerRight
			{ "j_f_eye_l"       , "head"}, // EyeLeft
			{ "j_f_eye_r"       , "head"}, // EyeRight
			{ "j_f_hana"        , "head"}, // Nose
			{ "j_f_hoho_l"      , "head"}, // CheekLeft
			{ "j_f_hoho_r"      , "head"}, // CheekRight
			{ "j_f_lip_l"       , "head"}, // LipsLeft
			{ "j_f_lip_r"       , "head"}, // LipsRight
			{ "j_f_mayu_l"      , "head"}, // EyebrowLeft
			{ "j_f_mayu_r"      , "head"}, // EyebrowRight
			{ "j_f_memoto"      , "head"}, // Bridge
			{ "j_f_miken_l"     , "head"}, // BrowLeft
			{ "j_f_miken_r"     , "head"}, // BrowRight
			{ "j_f_ulip_a"      , "head"}, // LipUpperA
			{ "j_f_umab_l"      , "head"}, // EyelidUpperLeft
			{ "j_f_umab_r"      , "head"}, // EyelidUpperRight
			{ "j_f_dlip_a"      , "head"}, // LipLowerA
			{ "j_f_ulip_b"      , "head"}, // LipUpperB
			{ "j_f_dlip_b"      , "head"}, // LipLowerB
			// Hrothgar Faces
			{ "j_f_hige_l"      , "head"}, // HrothWhiskersLeft
			{ "j_f_hige_r"      , "head"}, // HrothWhiskersRight
			//{ "j_f_mayu_l"    , "head"}, // HrothEyebrowLeft
			//{ "j_f_mayu_r"    , "head"}, // HrothEyebrowRight
			//{ "j_f_memoto"    , "head"}, // HrothBridge
			//{ "j_f_miken_l"   , "head"}, // HrothBrowLeft
			//{ "j_f_miken_r"   , "head"}, // HrothBrowRight
			{ "j_f_uago"        , "head"}, // HrothJawUpper
			{ "j_f_ulip"        , "head"}, // HrothLipUpper
			//{ "j_f_umab_l"    , "head"}, // HrothEyelidUpperLeft
			//{ "j_f_umab_r"    , "head"}, // HrothEyelidUpperRight
			{ "n_f_lip_l"       , "head"}, // HrothLipsLeft
			{ "n_f_lip_r"       , "head"}, // HrothLipsRight
			{ "n_f_ulip_l"      , "head"}, // HrothLipUpperLeft
			{ "n_f_ulip_r"      , "head"}, // HrothLipUpperRight
			{ "j_f_dlip"        , "head"}, // HrothLipLower


			{ "j_mimi_l"        , "ears"}, // EarLeft
			{ "j_mimi_r"        , "ears"}, // EarRight
			{ "j_zera_a_l"      , "ears"}, // VieraEar01ALeft
			{ "j_zera_a_r"      , "ears"}, // VieraEar01ARight
			{ "j_zera_b_l"      , "ears"}, // VieraEar01BLeft
			{ "j_zera_b_r"      , "ears"}, // VieraEar01BRight
			{ "j_zerb_a_l"      , "ears"}, // VieraEar02ALeft
			{ "j_zerb_a_r"      , "ears"}, // VieraEar02ARight
			{ "j_zerb_b_l"      , "ears"}, // VieraEar02BLeft
			{ "j_zerb_b_r"      , "ears"}, // VieraEar02BRight
			{ "j_zerc_a_l"      , "ears"}, // VieraEar03ALeft
			{ "j_zerc_a_r"      , "ears"}, // VieraEar03ARight
			{ "j_zerc_b_l"      , "ears"}, // VieraEar03BLeft
			{ "j_zerc_b_r"      , "ears"}, // VieraEar03BRight
			{ "j_zerd_a_l"      , "ears"}, // VieraEar04ALeft
			{ "j_zerd_a_r"      , "ears"}, // VieraEar04ARight
			{ "j_zerd_b_l"      , "ears"}, // VieraEar04BLeft
			{ "j_zerd_b_r"      , "ears"}, // VieraEar04BRight
			//{ "j_f_dlip_a"    , "head"}, // VieraLipLowerA
			//{ "j_f_ulip_b"    , "head"}, // VieraLipUpperB
			//{ "j_f_dlip_b"    , "head"}, // VieraLipLowerB

			// IVCS bones
			// 3rd finger joints
			{ "iv_ko_c_l"       , "ivcs left hand"}, // Pinky     rotation
			{ "iv_ko_c_r"       , "ivcs right hand"}, // Pinky
			{ "iv_kusu_c_l"     , "ivcs left hand"}, // Ring
			{ "iv_kusu_c_r"     , "ivcs right hand"}, // Ring
			{ "iv_naka_c_l"     , "ivcs left hand"}, // Middle
			{ "iv_naka_c_r"     , "ivcs right hand"}, // Middle
			{ "iv_hito_c_l"     , "ivcs left hand"}, // Index
			{ "iv_hito_c_r"     , "ivcs right hand"}, // Index
			// Toes
			{ "iv_asi_oya_a_l"  , "ivcs left foot"}, // Big toe   rotation
			{ "iv_asi_oya_b_l"  , "ivcs left foot"}, // Big toe
			{ "iv_asi_oya_a_r"  , "ivcs right foot"}, // Big toe
			{ "iv_asi_oya_b_r"  , "ivcs right foot"}, // Big toe
			{ "iv_asi_hito_a_l" , "ivcs left foot"}, // Index    rotation
			{ "iv_asi_hito_b_l" , "ivcs left foot"}, // Index
			{ "iv_asi_hito_a_r" , "ivcs right foot"}, // Index
			{ "iv_asi_hito_b_r" , "ivcs right foot"}, // Index
			{ "iv_asi_naka_a_l" , "ivcs left foot"}, // Middle    rotation
			{ "iv_asi_naka_b_l" , "ivcs left foot"}, // Middle
			{ "iv_asi_naka_a_r" , "ivcs right foot"}, // Middle
			{ "iv_asi_naka_b_r" , "ivcs right foot"}, // Middle
			{ "iv_asi_kusu_a_l" , "ivcs left foot"}, // Fore toe   rotation
			{ "iv_asi_kusu_b_l" , "ivcs left foot"}, // Fore toe
			{ "iv_asi_kusu_a_r" , "ivcs right foot"}, // Fore toe
			{ "iv_asi_kusu_b_r" , "ivcs right foot"}, // Fore toe
			{ "iv_asi_ko_a_l"   , "ivcs left foot"}, // Pinky toe   rotation
			{ "iv_asi_ko_b_l"   , "ivcs left foot"}, // Pinky toe
			{ "iv_asi_ko_a_r"   , "ivcs right foot"}, // Pinky toe
			{ "iv_asi_ko_b_r"   , "ivcs right foot"}, // Pinky toe
			// Arms
			{ "iv_nitoukin_l"   , "ivcs body"}, // Biceps    rotation, scale, position
			{ "iv_nitoukin_r"   , "ivcs body"}, // Biceps

			// Control override bones (override physics for animations only)
			{ "iv_c_mune_l"     , "ivcs body"}, // Breasts    rotation, scale, position
			{ "iv_c_mune_r"     , "ivcs body"}, // Breasts
			// Genitals
			{ "iv_kougan_l"     , "ivcs penis"}, // Scrotum    rotation, scale, position
			{ "iv_kougan_r"     , "ivcs penis"}, // Scrotum
			{ "iv_ochinko_a"    , "ivcs penis"}, // Penis     rotation, scale*
			{ "iv_ochinko_b"    , "ivcs penis"}, // Penis     *if you want to adjust size
			{ "iv_ochinko_c"    , "ivcs penis"}, // Penis     you will need to adjust
			{ "iv_ochinko_d"    , "ivcs penis"}, // Penis     position and scale
			{ "iv_ochinko_e"    , "ivcs penis"}, // Penis     for all 6 penis bones
			{ "iv_ochinko_f"    , "ivcs penis"}, // Penis     individually
			{ "iv_omanko"       , "ivcs vagina"}, // Vagina    rotation, position, scale
			{ "iv_kuritto"      , "ivcs vagina"}, // Clitoris   rotation, position, scale
			{ "iv_inshin_l"     , "ivcs vagina"}, // Labia    rotation, position, scale
			{ "iv_inshin_r"     , "ivcs vagina"}, // Labia
			// Butt stuffs
			{ "iv_koumon"       , "ivcs buttlocks"}, // Anus    rotation, scale, position
			{ "iv_koumon_l"     , "ivcs buttlocks"}, // Anus
			{ "iv_koumon_r"     , "ivcs buttlocks"}, // Anus
			{ "iv_shiri_l"      , "ivcs buttlocks"}, // Buttocks   rotation, scale, position
			{ "iv_shiri_r"      , "ivcs buttlocks"}, // Buttocks

		};




		public static Category GetCategory(string? boneName)
		{
			if (boneName == null || boneName == "")
				return DefaultCategory;

			Category? category = null;
			foreach ((string categoryName, Category posibleCategory) in Category.Categories)
			{
				if (posibleCategory.PossibleBones.Contains(boneName ?? ""))
					category = posibleCategory;
			}

			category ??= DefaultCategory;

			category.RegisterBone(boneName);

			return category;
		}
		public static string GetCategoryName(string? boneName) => GetCategory(boneName).Name;

	}
}
