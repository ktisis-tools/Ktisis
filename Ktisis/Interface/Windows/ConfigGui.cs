using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

using ImGuiNET;

using Newtonsoft.Json;

using Dalamud.Logging;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Game.ClientState.Keys;

using Ktisis.Helpers;
using Ktisis.Util;
using Ktisis.Overlay;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Structs.Actor.Equip.SetSources;
using Ktisis.Interface.Components;
using Ktisis.Interop.Hooks;

namespace Ktisis.Interface.Windows {
	internal static class ConfigGui {
		public static bool Visible = false;
		public static Vector2 ButtonSize => new Vector2(ImGui.GetFontSize() * 1.50f);
		private static Vector2 WindowSizeMin => new(ImGui.GetFontSize() * 15, ImGui.GetFontSize() * 20);
		private static Vector2 WindowSizeMax => ImGui.GetIO().DisplaySize * 0.85f;

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}
		public static void Toggle() => Visible = !Visible;

		// Draw

		public static bool _isSaved = true;

		public static void Draw() {
			if (!Visible) {
				if (!_isSaved) {
					Ktisis.Log.Verbose("Saving config...");
					Services.PluginInterface.SavePluginConfig(Ktisis.Configuration);
					_isSaved = true;
				}

				return;
			} else if (_isSaved) {
				_isSaved = false;
			}

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(WindowSizeMin, WindowSizeMax);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin(Locale.GetString("Ktisis_settings"), ref Visible)) {
				if (ImGui.BeginTabBar(Locale.GetString("Settings"))) {
					var cfg = Ktisis.Configuration;
					if (ImGui.BeginTabItem(Locale.GetString("Interface")))
						DrawInterfaceTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Overlay")))
						DrawOverlayTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Gizmo")))
						DrawGizmoTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Input")))
						DrawInputTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Camera")))
						DrawCameraTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("AutoSave")))
						DrawAutoSaveTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("References")))
						DrawReferencesTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Language")))
						DrawLanguageTab(cfg);
					if (ImGui.BeginTabItem("Data"))
						DrawDataTab(cfg);

					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Interface

		public static void DrawInterfaceTab(Configuration cfg) {

			ImGui.Text(Locale.GetString("General"));

			ImGui.AlignTextToFramePadding();
			ImGui.Text(Locale.GetString("Open_plugin_load") + " ");
			ImGui.SameLine();
			var selectedOpenKtisisMethod = cfg.OpenKtisisMethod;
			ImGui.SetNextItemWidth(GuiHelpers.AvailableWidth(0));
			if (ImGui.BeginCombo("##OpenKtisisMethod", $"{selectedOpenKtisisMethod}")) {
				foreach (var openKtisisMethod in Enum.GetValues<OpenKtisisMethod>()) {
					if (ImGui.Selectable($"{openKtisisMethod}", openKtisisMethod == selectedOpenKtisisMethod))
						cfg.OpenKtisisMethod = openKtisisMethod;
				}
				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var displayCharName = !cfg.DisplayCharName;
			if (ImGui.Checkbox(Locale.GetString("Hide_char_name"), ref displayCharName))
				cfg.DisplayCharName = !displayCharName;

			var censorNsfw = cfg.CensorNsfw;
			if (ImGui.Checkbox(Locale.GetString("Censor_nsfw"), ref censorNsfw))
				cfg.CensorNsfw = censorNsfw;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text(Locale.GetString("Transform_table"));

			ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
			var transformTablePointDigits = cfg.TransformTableDigitPrecision;
			if (ImGui.DragInt(Locale.GetString("Digit_precision"), ref transformTablePointDigits, 1f, 1, 8))
				cfg.TransformTableDigitPrecision = transformTablePointDigits;

			var transformTableBaseSpeedPos = cfg.TransformTableBaseSpeedPos;
			if (ImGui.DragFloat(Locale.GetString("Base_position_speed"), ref transformTableBaseSpeedPos, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedPos = transformTableBaseSpeedPos;

			var transformTableBaseSpeedRot = cfg.TransformTableBaseSpeedRot;
			if (ImGui.DragFloat(Locale.GetString("Base_rotation_speed"), ref transformTableBaseSpeedRot, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedRot = transformTableBaseSpeedRot;

			var transformTableBaseSpeedSca = cfg.TransformTableBaseSpeedSca;
			if (ImGui.DragFloat(Locale.GetString("Base_scale_speed"), ref transformTableBaseSpeedSca, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedSca = transformTableBaseSpeedSca;

			var transformTableModifierMultCtrl = cfg.TransformTableModifierMultCtrl;
			if (ImGui.DragFloat(Locale.GetString("Ctrl_speed_multiplier"), ref transformTableModifierMultCtrl, 1f, 0.00001f, 10000f, "%.4f",ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultCtrl = transformTableModifierMultCtrl;

			var transformTableModifierMultShift = cfg.TransformTableModifierMultShift;
			if (ImGui.DragFloat(Locale.GetString("Shift_speed_multiplier"), ref transformTableModifierMultShift, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultShift = transformTableModifierMultShift;

			var displayMultiplierInputs = cfg.TransformTableDisplayMultiplierInputs;
			if (ImGui.Checkbox(Locale.GetString("Show_speed_multipler_inputs"), ref displayMultiplierInputs))
				cfg.TransformTableDisplayMultiplierInputs = displayMultiplierInputs;

			var showToolbar = cfg.ShowToolbar;
			if (ImGui.Checkbox("Show Experimental Toolbar", ref showToolbar))
				cfg.ShowToolbar = showToolbar;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text(Locale.GetString("UI Customization (Experimental)"));

			var customWidthMarginDebug = cfg.CustomWidthMarginDebug;
			if (ImGui.DragFloat("Right margin (debug)", ref customWidthMarginDebug, 0.05f, -10, 50, "%.2f"))
				cfg.CustomWidthMarginDebug = customWidthMarginDebug;
			ImGui.SameLine();
			ImGuiComponents.HelpMarker(Locale.GetString("Right margin for determining window content size (used for right-aligning and width-filling).\nIncrease this value if the UI stretches to the entire screen.\n\nNote: If this value is changed, reporting these info to Ktisis team would greatly help!\n - The edited Right margin value\n - The Dalamud theme preset in use"));

			ImGui.PopItemWidth();

			ImGui.EndTabItem();
		}

		// Overlay

		public static void DrawOverlayTab(Configuration cfg) {
			ImGui.Spacing();

			var order = cfg.OrderBoneListByDistance;
			if (ImGui.Checkbox("Order bone list by distance from camera", ref order))
				cfg.OrderBoneListByDistance = order;

			ImGui.Spacing();

			if (ImGui.CollapsingHeader(Locale.GetString("Skeleton_lines_and_dots"), ImGuiTreeNodeFlags.DefaultOpen)) {
				ImGui.Separator();
				var drawLines = cfg.DrawLinesOnSkeleton;
				if (ImGui.Checkbox(Locale.GetString("Draw_lines_on_skeleton"), ref drawLines))
					cfg.DrawLinesOnSkeleton = drawLines;

				var drawLinesGizmo = cfg.DrawLinesWithGizmo;
				if (ImGui.Checkbox(Locale.GetString("Draw_lines_with_gizmo"), ref drawLinesGizmo))
					cfg.DrawLinesWithGizmo = drawLinesGizmo;

				var drawDotsGizmo = cfg.DrawDotsWithGizmo;
				if (ImGui.Checkbox(Locale.GetString("Draw_dots_with_gizmo"), ref drawDotsGizmo))
					cfg.DrawDotsWithGizmo = drawDotsGizmo;

				var dotRadius = cfg.SkeletonDotRadius;
				if (ImGui.SliderFloat(Locale.GetString("Dot_radius"), ref dotRadius, 0.01F, 15F, "%.1f"))
					cfg.SkeletonDotRadius = dotRadius;

				var lineThickness = cfg.SkeletonLineThickness;
				if (ImGui.SliderFloat(Locale.GetString("Lines_thickness"), ref lineThickness, 0.01F, 15F, "%.1f"))
					cfg.SkeletonLineThickness = lineThickness;

				var lineOpacity = cfg.SkeletonLineOpacity;
				if (ImGui.SliderFloat(Locale.GetString("Lines_opacity"), ref lineOpacity, 0.01F, 1F, "%.2f"))
					cfg.SkeletonLineOpacity = lineOpacity;

				var lineOpacityWhileUsing = cfg.SkeletonLineOpacityWhileUsing;
				if (ImGui.SliderFloat(Locale.GetString("Lines_opacity_while_using"), ref lineOpacityWhileUsing, 0.01F, 1F, "%.2f"))
					cfg.SkeletonLineOpacityWhileUsing = lineOpacityWhileUsing;
			}
			if (ImGui.CollapsingHeader(Locale.GetString("Bone_colors"), ImGuiTreeNodeFlags.DefaultOpen)) {

				ImGui.Separator();

				bool linkBoneCategoriesColors = cfg.LinkBoneCategoryColors;
				if (GuiHelpers.IconButtonTooltip(cfg.LinkBoneCategoryColors ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink, linkBoneCategoriesColors ? Locale.GetString("Unlink_bones_colors") : Locale.GetString("Link_bones_colors")))
					cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;

				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Eraser, Locale.GetString("Hold_Control_and_Shift_to_erase_colors"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)) {
					Vector4 eraseColor = new(1.0F, 1.0F, 1.0F, 0.5647059F);
					if (linkBoneCategoriesColors) {
						cfg.LinkedBoneCategoryColor = eraseColor;
					} else {
						foreach (Category category in Category.Categories.Values) {
							if (category.ShouldDisplay || cfg.BoneCategoryColors.ContainsKey(category.Name))
								cfg.BoneCategoryColors[category.Name] = eraseColor;
						}
					}
				}

				if (linkBoneCategoriesColors) {
					Vector4 linkedBoneColor = cfg.LinkedBoneCategoryColor;
					if (ImGui.ColorEdit4(Locale.GetString("Bone_colors"), ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
						cfg.LinkedBoneCategoryColor = linkedBoneColor;
				} else {

					ImGui.SameLine();
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Rainbow, Locale.GetString("Hold_Control_and_Shift_to_reset_colors_to_their_default_values"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)) {
						foreach ((string _, Category category) in Category.Categories) {
							if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
								continue;
							cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
						}
					}

					ImGui.Text(Locale.GetString("Categories_colors"));

					if (!Components.Categories.DrawConfigList(cfg))
						ImGui.TextWrapped(Locale.GetString("Categories_will_be_added_after_bones_are_displayed_once"));

				}
			}
			if (ImGui.CollapsingHeader(Locale.GetString("Edit_bone_positions")))
				DrawBonesOffset(cfg);

			ImGui.EndTabItem();
		}

		// Gizmo

		public static void DrawGizmoTab(Configuration cfg) {
			var allowAxisFlip = cfg.AllowAxisFlip;
			if (ImGui.Checkbox(Locale.GetString("Flip_axis_to_face_camera"), ref allowAxisFlip))
				cfg.AllowAxisFlip = allowAxisFlip;

			ImGui.EndTabItem();
		}

		// AutoSave
		public static void DrawAutoSaveTab(Configuration cfg) {
			var enableAutoSave = cfg.EnableAutoSave;
			if (ImGui.Checkbox(Locale.GetString("Enable_auto_save"), ref enableAutoSave)) {
				cfg.EnableAutoSave = enableAutoSave;
				PoseHooks.AutoSave.UpdateSettings();
			}

			var clearOnExit = cfg.ClearAutoSavesOnExit;
			if (ImGui.Checkbox(Locale.GetString("Clear_auto_saves_on_exit"), ref clearOnExit))
				cfg.ClearAutoSavesOnExit = clearOnExit;
			
			ImGui.Spacing();

			var autoSaveInterval = cfg.AutoSaveInterval;
			if (ImGui.SliderInt(Locale.GetString("Auto_save_interval"), ref autoSaveInterval, 10, 600, "%d s")) {
				cfg.AutoSaveInterval = autoSaveInterval;
				PoseHooks.AutoSave.UpdateSettings();
			}

			var autoSaveCount = cfg.AutoSaveCount;
			if (ImGui.SliderInt(Locale.GetString("Auto_save_count"), ref autoSaveCount, 1, 20))
				cfg.AutoSaveCount = autoSaveCount;

			var autoSavePath = cfg.AutoSavePath;
			if (ImGui.InputText(Locale.GetString("Auto_save_path"), ref autoSavePath, 256))
				cfg.AutoSavePath = autoSavePath;

			var autoSaveFormat = cfg.AutoSaveFormat;
			if (ImGui.InputText(Locale.GetString("Auto_save_Folder_Name"), ref autoSaveFormat, 256))
				cfg.AutoSaveFormat = autoSaveFormat;

			ImGui.Text(Locale.GetString("Example_Folder_Name"));
			ImGui.TextUnformatted(PathHelper.Replace(autoSaveFormat));
			
			ImGui.Spacing();
			ImGui.BeginTable("AutoSaveFormatters", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders| ImGuiTableFlags.PadOuterX);
			
			ImGui.TableSetupScrollFreeze(0, 1);
			ImGui.TableSetupColumn(Locale.GetString("Formatter"));
			ImGui.TableSetupColumn(Locale.GetString("Example_Value"));
			ImGui.TableHeadersRow();

			foreach ((var replaceKey, var replaceFunc) in PathHelper.Replacers) {
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.TextUnformatted(replaceKey);
				ImGui.TableNextColumn();
				ImGui.TextUnformatted(replaceFunc());
			}
			
			ImGui.EndTable();

			ImGui.EndTabItem();
		}

		// Language

		public static void DrawLanguageTab(Configuration cfg) {
			ImGui.Text("Disclaimer! These settings are currently only in place to test the WIP localization system.");
			ImGui.Text("Translation strings are not currently supported in most of the UI.");

			ImGui.Spacing();

			var selected = "";
			foreach (var lang in Locale.Languages) {
				if (lang == cfg.Localization) {
					selected = $"{lang}";
					break;
				}
			}

			if (ImGui.BeginCombo(Locale.GetString("Language"), selected)) {
				foreach (var lang in Locale.Languages) {
					var name = $"{lang}";
					if (ImGui.Selectable(name, name == selected))
						cfg.Localization = lang;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var translateBones = cfg.TranslateBones;
			if (ImGui.Checkbox(Locale.GetString("Translate_bone_names"), ref translateBones))
				cfg.TranslateBones = translateBones;

			ImGui.EndTabItem();
		}


		// input selector
		public static void DrawInputTab(Configuration cfg) {
			ImGui.Spacing();
			ImGui.Text("Selection Behavior");
			ImGui.Spacing();

			var disableChangeTargetOnLeftClick = cfg.DisableChangeTargetOnLeftClick;
			if (ImGui.Checkbox(Locale.GetString("Disable_Change_Target_On_Left_Click"), ref disableChangeTargetOnLeftClick))
				cfg.DisableChangeTargetOnLeftClick = disableChangeTargetOnLeftClick;

			var disableChangeTargetOnRightClick = cfg.DisableChangeTargetOnRightClick;
			if (ImGui.Checkbox(Locale.GetString("Disable_Change_Target_On_Right_Click"), ref disableChangeTargetOnRightClick))
				cfg.DisableChangeTargetOnRightClick = disableChangeTargetOnRightClick;


			ImGui.Spacing();
			ImGui.Text(Locale.GetString("Keyboard_shortcuts"));
			ImGui.Spacing();

			// completely enable/disable keyboard shortcuts
			var enableKeybinds = cfg.EnableKeybinds;
			if(ImGui.Checkbox(Locale.GetString("Enable"), ref enableKeybinds))
				cfg.EnableKeybinds = enableKeybinds;
			if (!cfg.EnableKeybinds) {
				ImGui.EndTabItem();
				return;
			}

			// display the currently pressed keys
			List<VirtualKey> pressDemo = Input.FallbackKey;
			foreach (var key in Enum.GetValues<VirtualKey>()) {
				if (!Services.KeyState.IsVirtualKeyValid(key)) continue;
				var state = Services.KeyState[key];
				if (state && pressDemo == Input.FallbackKey)
					pressDemo = new();
				if (state)
					pressDemo.Add(key);
			}

			ImGui.Text(Locale.GetString("Pressing_keys"));
			ImGuiComponents.HelpMarker("To assign a key or key combination:\n" +
				"1. Hold the key or key combination\n" +
				"2. Click on the desired action\n\n" +
				"Do not hold any key to unassign.");
			ImGui.SameLine();
			ImGui.Text($":   {PrettyKeys(pressDemo)}");
			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginChildFrame(74, new(-1, ImGui.GetFontSize() * 20));

			// key/Action table
			ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, ImGui.GetStyle().CellPadding * 3);
			if (ImGui.BeginTable("keybinds_table", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX)) {

				// display and configureheaders
				ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
				ImGui.TableSetupColumn("Keys");
				ImGui.TableSetupColumn("Action");
				ImGui.TableHeadersRow();

				foreach (var purpose in Input.PurposesWithCategories) {
					if ((int)purpose >= Input.FirstCategoryPurposeHold)
						if (Input.PurposesCategories.TryGetValue(purpose, out var category))
							if ((!cfg.KeyBinds.Any(t => t.Key == purpose) && !category.ShouldDisplay) || (cfg.CensorNsfw && category.IsNsfw)) continue;

					// get currently configured or default keys
					if (!Input.DefaultKeys.TryGetValue(purpose, out List<VirtualKey>? defaultKeys))
						defaultKeys = Input.FallbackKey;
					if (!cfg.KeyBinds.TryGetValue(purpose, out List<VirtualKey>? configuredKeys))
						configuredKeys = defaultKeys;

					ImGui.TableNextRow();
					var clickRow = false;

					// display the current key (config or default)
					ImGui.TableNextColumn();
					var configuredKeysPretty = PrettyKeys(configuredKeys);
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetContentRegionAvail().X - GuiHelpers.WidthMargin() - ImGui.CalcTextSize(configuredKeysPretty).X) / 2));
					ImGui.Selectable($"{configuredKeysPretty}##{purpose}", false, ImGuiSelectableFlags.SpanAllColumns);
					clickRow |= ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);

					// display the purpose
					ImGui.TableNextColumn();
					ImGui.Selectable(Locale.GetInputPurposeName(purpose), false, ImGuiSelectableFlags.SpanAllColumns);
					clickRow |= ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);

					// execute the change if clicked
					if (clickRow)
						if (pressDemo == defaultKeys) cfg.KeyBinds.Remove(purpose);
						else cfg.KeyBinds[purpose] = pressDemo;

				}
				ImGui.EndTable();
			}
			ImGui.PopStyleVar();
			ImGui.EndChildFrame();
			ImGui.EndTabItem();
		}
		private static string PrettyKeys(List<VirtualKey>  keys) => string.Join(" + ", keys.Select(k => VirtualKeyExtensions.GetFancyName(k)));

		// Camera

		private static void DrawCameraTab(Configuration cfg) {
			ImGui.Spacing();

			ImGui.Text("Work camera controls");
			ImGui.PushItemWidth(ImGui.GetFontSize() * 4);

			var baseSpeed = cfg.FreecamMoveSpeed;
			if (ImGui.DragFloat("Base move speed", ref baseSpeed, 0.001f, 0, 1))
				cfg.FreecamMoveSpeed = baseSpeed;

			var shiftMuli = cfg.FreecamShiftMuli;
			if (ImGui.DragFloat("Fast speed multiplier", ref shiftMuli, 0.001f, 0, 10))
				cfg.FreecamShiftMuli = shiftMuli;

			var ctrlMuli = cfg.FreecamCtrlMuli;
			if (ImGui.DragFloat("Slow speed multiplier", ref ctrlMuli, 0.001f, 0, 10))
				cfg.FreecamCtrlMuli = ctrlMuli;

			var upDownMuli = cfg.FreecamUpDownMuli;
			if (ImGui.DragFloat("Up/down speed multiplier", ref upDownMuli, 0.001f, 0, 10))
				cfg.FreecamUpDownMuli = upDownMuli;

			ImGui.Spacing();

			var camSens = cfg.FreecamSensitivity;
			if (ImGui.DragFloat("Camera sensitivity", ref camSens, 0.001f, 0, 8))
				cfg.FreecamSensitivity = camSens;

			ImGui.Spacing();

			ImGui.PushItemWidth(ImGui.GetFontSize() * 8);

			ImGui.Text("Work camera keybinds");

			KeybindEdit.Draw("Forward##WCForward", cfg.FreecamForward);
			KeybindEdit.Draw("Left##WCLeft", cfg.FreecamLeft);
			KeybindEdit.Draw("Back##WCBack", cfg.FreecamBack);
			KeybindEdit.Draw("Right##WCRight", cfg.FreecamRight);
			KeybindEdit.Draw("Up##WCUp", cfg.FreecamUp);
			KeybindEdit.Draw("Down##WCDown", cfg.FreecamDown);

			ImGui.Spacing();

			KeybindEdit.Draw("Fast speed modifier##WCUp", cfg.FreecamFast);
			KeybindEdit.Draw("Slow speed modifier##WCUp", cfg.FreecamSlow);

			ImGui.PopItemWidth();

			ImGui.EndTabItem();
		}

		// Data

		public static void DrawDataTab(Configuration cfg) {
			ImGui.Spacing();
			var validGlamPlatesFound = GlamourDresser.CountValid();
			GuiHelpers.TextTooltip($"Glamour Plates in memory: {validGlamPlatesFound}  ", $"Found {validGlamPlatesFound} valid Glamour Plates");
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Sync, "Refresh Glamour Plate memory for the Sets lookups.\nThis memory is kept after a restart.\n\nRequirements:\n One of these windows must be opened: \"Glamour Plate Creation\" (by the Glamour Dresser) or \"Plate Selection\" (by the Glamour Plate skill)."))
				GlamourDresser.PopulatePlatesData();

			Components.Equipment.CreateGlamourQuestionPopup();

			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Trash, "Dispose of the Glamour Plates memory and remove configurations for ALL characters.")) {
				Sets.Dispose();
				cfg.GlamourPlateData = null;
			}
			ImGui.EndTabItem();
		}

		public static unsafe void DrawBonesOffset(Configuration cfg) {

			ImGui.Separator();
			var bone = Skeleton.GetSelectedBone();
			if (bone != null) {
				var targetBodyType = CustomOffset.GetRaceGenderFromActor(Ktisis.Target);
				var targetBoneOffset = CustomOffset.GetBoneOffset(bone);

				ImGui.Text($"Edit {targetBodyType}'s {bone.LocaleName}  ");

				if (!cfg.CustomBoneOffset.TryGetValue(targetBodyType, out var _))
					cfg.CustomBoneOffset.Add(targetBodyType,new());

				if (GuiHelpers.DragFloat3FillWidth($"##currentTargetOffset", false, null, ref targetBoneOffset, .00001f, "%.5f"))
					cfg.CustomBoneOffset[targetBodyType][bone.HkaBone.Name.String!] = targetBoneOffset;
			} else {
				ImGuiComponents.HelpMarker("Select a Bone to start adjusting its position.");
			}

			ImGui.Spacing();

			foreach (var bt in cfg.CustomBoneOffset) {
				var bodyType = bt.Key;
				if (ImGui.CollapsingHeader($"{bodyType}")) {
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash,$"Hold Ctrl and Shift to drop all {bodyType} bone offsets.", default, $"dropall##{bodyType}"))
						cfg.CustomBoneOffset.Remove(bodyType);
					ImGui.SameLine();
					if (GuiHelpers.IconButton(FontAwesomeIcon.Clipboard, default, $"export##{bodyType}"))
						ImGui.SetClipboardText(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cfg.CustomBoneOffset[bodyType]))));
					ImGui.SameLine();
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Paste, $"Hold Ctrl and Shift to paste and replace all {bodyType} bone offsets.", default, $"pasteReplaceAll##{bodyType}")) {
						var parsedPasteAll = JsonConvert.DeserializeObject<Dictionary<string, Vector3>>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText())));
						if (parsedPasteAll != null)
							cfg.CustomBoneOffset[bodyType] = parsedPasteAll;
					}

					ImGuiComponents.HelpMarker("Tips:\n" +
						"Click on a row to copy it into clipboard" +
						"Ctrl + Shift + Right click to remove a row" +
						"The plus (+) button will insert a copied row");


					if (ImGui.BeginTable("offsetBonesTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX)) {

						ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
						ImGui.TableSetupColumn("Bone");
						ImGui.TableSetupColumn("X");
						ImGui.TableSetupColumn("Y");
						ImGui.TableSetupColumn("Z");
						ImGui.TableHeadersRow();

						foreach (var os in bt.Value) {
							var boneName = os.Key;
							var offsets = os.Value;
							ImGui.TableNextRow();
							ImGui.TableNextColumn();
							var isDeletable = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
							if(isDeletable) ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Workspace.Workspace.ColRed);
							if (ImGui.Selectable($"{Locale.GetBoneName(boneName)}##{bodyType}##customBoneOffset", false, ImGuiSelectableFlags.SpanAllColumns))
								ImGui.SetClipboardText(Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject((boneName,offsets)))));
							if (isDeletable) ImGui.PopStyleColor();
							if (isDeletable && ImGui.IsItemClicked(ImGuiMouseButton.Right))
								cfg.CustomBoneOffset[bodyType].Remove(boneName);
							ImGui.TableNextColumn();
							ImGui.Text($"{offsets.X:F6}");
							ImGui.TableNextColumn();
							ImGui.Text($"{offsets.Y:F6}");
							ImGui.TableNextColumn();
							ImGui.Text($"{offsets.Z:F6}");
						}
						ImGui.EndTable();
						if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus,"Add a line from clipboard.", default, $"{bodyType}##Clipboard##AddLine")) {
							var parsedPasteLine = JsonConvert.DeserializeObject<(string, Vector3)>(Encoding.UTF8.GetString(Convert.FromBase64String(ImGui.GetClipboardText()))) ;
							cfg.CustomBoneOffset[bodyType][parsedPasteLine.Item1] = parsedPasteLine.Item2;
						}
					}
				}
			}
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, $"Hold Ctrl and Shift to drop ALL bone offsets.", default, "dropAllOffset"))
				cfg.CustomBoneOffset = new();

			ImGui.Spacing();
		}

		// References

		public static void DrawReferencesTab(Configuration cfg) {
			ImGui.Text(Locale.GetString("config.references_tab.explanation"));
			var alpha = cfg.ReferenceAlpha;
			if (ImGui.SliderFloat(Locale.GetString("config.references_tab.image_transparency"), ref alpha, 0.0f, 1.0f)) {
				cfg.ReferenceAlpha = alpha;
			}
			var hideDecoration = cfg.ReferenceHideDecoration;
			if (ImGui.Checkbox(Locale.GetString("config.references_tab.hide_window_decorations"), ref hideDecoration)) {
				cfg.ReferenceHideDecoration = hideDecoration;
			}
			ImGui.Text(Locale.GetString("config.references_tab.reference_images"));
			foreach (var (key, reference) in cfg.References) {
				ImGui.PushID(key);
				bool showing = reference.Showing;
				if (ImGui.Checkbox("##Showing", ref showing)) {
					reference.Showing = showing;
				}
				ImGui.SameLine();
				var buf = new string(reference.Path);
				if (ImGui.InputText("##Path", ref buf, 255, ImGuiInputTextFlags.EnterReturnsTrue) || ImGui.IsItemDeactivatedAfterEdit()) {
					TryChangeReference(cfg, key, buf);
				}
				ImGui.SameLine();
				if (GuiHelpers.IconButton(FontAwesomeIcon.File, ButtonSize)) {
					KtisisGui.FileDialogManager.OpenFileDialog(
						Locale.GetString("config.references_tab.add_reference_file"),
						Locale.GetString("config.references_tab.supported_reference_files") + "{.gif,.jpg,.jpeg,.png}",
						(success, filePath) => {
							if (success) {
								TryChangeReference(cfg, key, filePath);
							}
						}
					);
				}
				ImGui.SameLine();
				if (GuiHelpers.IconButton(FontAwesomeIcon.Trash, ButtonSize)) {
					cfg.References.Remove(key);
					References.DisposeUnreferencedTextures(cfg);
				}
				ImGui.PopID();
			}

			if (GuiHelpers.IconButton(FontAwesomeIcon.Plus, ButtonSize)) {
				cfg.References[cfg.NextReferenceKey] = new ReferenceInfo { Showing = true };
			}
			ImGui.SameLine();
			ImGui.Text(Locale.GetString("config.references_tab.add_new"));

			ImGui.EndTabItem();
		}

		public static bool TryChangeReference(Configuration cfg, int key, string newPath) {
			try {
				var texture = Services.Textures.GetFromFile(newPath);
				cfg.References[key] = new ReferenceInfo {
					Path = newPath,
					Showing = true,
				};
				References.Textures[newPath] = texture;
				Logger.Information("Successfully loaded reference image {0}", newPath);
				References.DisposeUnreferencedTextures(cfg);
				return true;
			} catch (Exception e) {
				Logger.Error(e, "Failed to load reference image {0}", newPath);
				return false;
			}
		}
	}
}
