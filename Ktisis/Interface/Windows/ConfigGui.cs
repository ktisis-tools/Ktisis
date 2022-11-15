using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using ImGuiNET;
using Newtonsoft.Json;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Components;

using Ktisis.Util;
using Ktisis.Localization;
using Ktisis.Overlay;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Structs.Actor.Equip.SetSources;

namespace Ktisis.Interface.Windows {
	internal static class ConfigGui {
		public static bool Visible = false;

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}

		// Draw

		public static void Draw() {
			if (!Visible)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin(Locale.GetString("config_tab.title"), ref Visible, ImGuiWindowFlags.NoResize)) {
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
					if (ImGui.BeginTabItem(Locale.GetString("Language")))
						DrawLanguageTab(cfg);
					if (ImGui.BeginTabItem(Locale.GetString("Data")))
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

			var openCtor = cfg.AutoOpenCtor;
			if (ImGui.Checkbox(Locale.GetString("config_tab.autoOpen"), ref openCtor))
				cfg.AutoOpenCtor = openCtor;

			var displayCharName = !cfg.DisplayCharName;
			if (ImGui.Checkbox(Locale.GetString("config_tab.charName"), ref displayCharName))
				cfg.DisplayCharName = !displayCharName;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text(Locale.GetString("config_tab.transformTable"));

			ImGui.PushItemWidth(ImGui.GetFontSize() * 4);
			var transformTablePointDigits = cfg.TransformTableDigitPrecision;
			if (ImGui.DragInt(Locale.GetString("config_tab.precision"), ref transformTablePointDigits, 1f, 1, 8))
				cfg.TransformTableDigitPrecision = transformTablePointDigits;

			var transformTableBaseSpeedPos = cfg.TransformTableBaseSpeedPos;
			if (ImGui.DragFloat(Locale.GetString("config_tab.posSpeed"), ref transformTableBaseSpeedPos, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedPos = transformTableBaseSpeedPos;

			var transformTableBaseSpeedRot = cfg.TransformTableBaseSpeedRot;
			if (ImGui.DragFloat(Locale.GetString("config_tab.rotSpeed"), ref transformTableBaseSpeedRot, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedRot = transformTableBaseSpeedRot;

			var transformTableBaseSpeedSca = cfg.TransformTableBaseSpeedSca;
			if (ImGui.DragFloat(Locale.GetString("config_tab.scaSpeed"), ref transformTableBaseSpeedSca, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableBaseSpeedSca = transformTableBaseSpeedSca;

			var transformTableModifierMultCtrl = cfg.TransformTableModifierMultCtrl;
			if (ImGui.DragFloat(Locale.GetString("config_tab.ctrlSpeedMod"), ref transformTableModifierMultCtrl, 1f, 0.00001f, 10000f, "%.4f",ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultCtrl = transformTableModifierMultCtrl;

			var transformTableModifierMultShift = cfg.TransformTableModifierMultShift;
			if (ImGui.DragFloat(Locale.GetString("config_tab.shiftSpeedMod"), ref transformTableModifierMultShift, 1f, 0.00001f, 10000f, "%.4f", ImGuiSliderFlags.Logarithmic))
				cfg.TransformTableModifierMultShift = transformTableModifierMultShift;

			var displayMultiplierInputs = cfg.TransformTableDisplayMultiplierInputs;
			if (ImGui.Checkbox(Locale.GetString("config_tab.speedModInput"), ref displayMultiplierInputs))
				cfg.TransformTableDisplayMultiplierInputs = displayMultiplierInputs;
			ImGui.PopItemWidth();

			ImGui.EndTabItem();
		}

		// Overlay

		public static void DrawOverlayTab(Configuration cfg) {
			if (ImGui.CollapsingHeader(Locale.GetString("config_tab.skeletonLineDot"), ImGuiTreeNodeFlags.DefaultOpen)) {
				ImGui.Separator();
				var drawLines = cfg.DrawLinesOnSkeleton;
				if (ImGui.Checkbox(Locale.GetString("config_tab.drawSkeleton"), ref drawLines))
					cfg.DrawLinesOnSkeleton = drawLines;

				var lineThickness = cfg.SkeletonLineThickness;
				if (ImGui.SliderFloat(Locale.GetString("config_tab.skeletonThickness"), ref lineThickness, 0.01F, 15F, "%.1f"))
					cfg.SkeletonLineThickness = lineThickness;
			}
			if (ImGui.CollapsingHeader(Locale.GetString("config_tab.boneColor"), ImGuiTreeNodeFlags.DefaultOpen)) {

				ImGui.Separator();

				bool linkBoneCategoriesColors = cfg.LinkBoneCategoryColors;
				if (GuiHelpers.IconButtonTooltip(cfg.LinkBoneCategoryColors ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink, linkBoneCategoriesColors ? Locale.GetString("config_tab.unlinkBones") : Locale.GetString("config_tab.linkBones")))
					cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;

				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Eraser, Locale.GetString("config_tab.eraseColors"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)) {
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
					if (ImGui.ColorEdit4(Locale.GetString("config_tab.boneColor"), ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
						cfg.LinkedBoneCategoryColor = linkedBoneColor;
				} else {

					ImGui.SameLine();
					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Rainbow, Locale.GetString("config_tab.resetColors"), ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift)) {
						foreach ((string categoryName, Category category) in Category.Categories) {
							if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
								continue;
							cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
						}
					}

					ImGui.Text(Locale.GetString("config_tab.categorieColor"));
					ImGui.Columns(2);
					int i = 0;
					bool hasShownAnyCategory = false;
					foreach (Category category in Category.Categories.Values) {
						if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
							continue;

						if (!cfg.BoneCategoryColors.TryGetValue(category.Name, out Vector4 categoryColor))
							categoryColor = cfg.LinkedBoneCategoryColor;

						if (ImGui.ColorEdit4(category.Name, ref categoryColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
							cfg.BoneCategoryColors[category.Name] = categoryColor;

						if (i % 2 != 0) ImGui.NextColumn();
						i++;
						hasShownAnyCategory = true;
					}
					ImGui.Columns();
					if (!hasShownAnyCategory)
						ImGui.TextWrapped(Locale.GetString("config_tab.boneDisplayHint"));
				}
			}
			if (ImGui.CollapsingHeader(Locale.GetString("config_tab.bonePos")))
				DrawBonesOffset(cfg);
			ImGui.EndTabItem();
		}

		// Gizmo

		public static void DrawGizmoTab(Configuration cfg) {
			var allowAxisFlip = cfg.AllowAxisFlip;
			if (ImGui.Checkbox(Locale.GetString("config_tab.axisFaceCamera"), ref allowAxisFlip))
				cfg.AllowAxisFlip = allowAxisFlip;

			ImGui.EndTabItem();
		}

		// Language

		public static void DrawLanguageTab(Configuration cfg) {
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
			if (ImGui.Checkbox(Locale.GetString("config_tab.langBones"), ref translateBones))
				cfg.TranslateBones = translateBones;

			ImGui.EndTabItem();
		}


		// input selector
		public static void DrawInputTab(Configuration cfg) {
			ImGui.Spacing();
			ImGui.Text(Locale.GetString("config_tab.shortcuts"));
			ImGui.Spacing();

			// completely enable/disable keyboard shortcuts
			var enableKeybinds = cfg.EnableKeybinds;
			if(ImGui.Checkbox(Locale.GetString("Enable"), ref enableKeybinds))
				cfg.EnableKeybinds = enableKeybinds;
			if (!cfg.EnableKeybinds) return;

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

			ImGui.Text(Locale.GetString("config_tab.pressedKey"));
			ImGuiComponents.HelpMarker("To assign a key or key combination:\n" +
				"1. Hold the key or key combination\n" +
				"2. Click on the desired action\n\n" +
				"Do not hold any key to unassign.");
			ImGui.SameLine();
			ImGui.Text($":   {PrettyKeys(pressDemo)}");
			ImGui.Spacing();
			ImGui.Spacing();


			// key/Action table
			ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, ImGui.GetStyle().CellPadding * 3);
			if (ImGui.BeginTable("keybinds_table", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.PadOuterX)) {

				// display and configureheaders
				ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
				ImGui.TableSetupColumn("Keys");
				ImGui.TableSetupColumn("Action");
				ImGui.TableHeadersRow();

				foreach (var purpose in Input.Purposes) {

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
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(configuredKeysPretty).X) / 2));
					ImGui.Selectable($"{configuredKeysPretty}##{purpose}", false, ImGuiSelectableFlags.SpanAllColumns);
					clickRow |= ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);

					// display the purpose
					ImGui.TableNextColumn();
					ImGui.Selectable(Locale.GetString($"Keyboard_Action_{purpose}"), false, ImGuiSelectableFlags.SpanAllColumns);
					clickRow |= ImGui.IsItemClicked(ImGuiMouseButton.Left) || ImGui.IsItemClicked(ImGuiMouseButton.Right);

					// execute the change if clicked
					if (clickRow)
						if (pressDemo == defaultKeys) cfg.KeyBinds.Remove(purpose);
						else cfg.KeyBinds[purpose] = pressDemo;

				}
				ImGui.EndTable();
			}
			ImGui.PopStyleVar();
			ImGui.EndTabItem();

		}
		private static string PrettyKeys(List<VirtualKey>  keys) => string.Join(" + ", keys.Select(k => VirtualKeyExtensions.GetFancyName(k)));

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
					cfg.CustomBoneOffset[targetBodyType][bone.HkaBone.Name.String] = targetBoneOffset;
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
	}
}
