using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Sections;
using Ktisis.Localization;
using Ktisis.Scene.Types;

namespace Ktisis.Interface.Components.Config;

[Transient]
public class BoneCategoryEditor {
	private readonly ConfigManager _cfg;
	private readonly LocaleManager _locale;

	private CategoryConfig Config => this._cfg.File.Categories;
	
	public BoneCategoryEditor(
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._cfg = cfg;
		this._locale = locale;
	}
	
	// Setup

	private readonly Dictionary<string, List<BoneCategory>> CategoryMap = new();

	public void Setup() {
		this.Selected = null;
		this.BuildCategoryMap();
	}

	private void BuildCategoryMap() {
		this.CategoryMap.Clear();
		for (var i = -1; i < this.Config.CategoryList.Count; i++) {
			var parent = i >= 0 ? this.Config.CategoryList[i].Name : null;
			var categories = this.Config.CategoryList
				.Where(cat => cat.ParentCategory == parent)
				.ToList();
			if (categories.Count > 0)
				this.CategoryMap.Add(parent ?? string.Empty, categories);
		}
	}
	
	// Draw
	
	public void Draw() {
		using var pad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
		using var frame = ImRaii.Child("##BoneCategoriesFrame", ImGui.GetContentRegionAvail(), true);
		if (!frame.Success) return;
		
		using var tablePad = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(10, 10));
		using var table = ImRaii.Table("##BoneCategoriesTable", 2, ImGuiTableFlags.Resizable);
		if (!table.Success) return;
		
		ImGui.TableSetupColumn("CategoryList");
		ImGui.TableSetupColumn("CategoryInfo");

		ImGui.TableNextRow();
		this.DrawCategoryList();
		this.DrawCategoryInfo();

		var dummy = ImGui.GetContentRegionAvail() with { X = 0.0f };
		dummy.Y -= ImGui.GetStyle().ItemSpacing.Y;
		ImGui.Dummy(dummy);
	}
	
	// Category list

	private void DrawCategoryList() {
		ImGui.TableNextColumn();

		using var _ = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, UiBuilder.DefaultFont.FontSize);
		this.DrawCategoryList(string.Empty);
	}
	
	private void DrawCategoryList(string key) {
		if (this.CategoryMap.TryGetValue(key, out var categories))
			categories.ForEach(this.DrawListCategory);
	}

	private void DrawListCategory(BoneCategory category) {
		if (category.IsNsfw && !this.Config.ShowNsfwBones) return;
		
		using var node = this.DrawCategoryNode(category);
		if (ImGui.IsItemClicked()) {
			if (ImGui.GetItemRectMin().X + ImGui.GetTreeNodeToLabelSpacing() < ImGui.GetMousePos().X)
				this.Selected = this.Selected != category ? category : null;
		}
		if (node.Success) this.DrawCategoryList(category.Name);
	}

	private ImRaii.IEndObject DrawCategoryNode(BoneCategory category) {
		var display = this._cfg.File.Editor.GetDisplayForType(EntityType.BoneGroup);
		using var _ = ImRaii.PushColor(ImGuiCol.Text, display.Color);

		var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
		if (this.Selected == category)
			flags |= ImGuiTreeNodeFlags.Selected;
		if (!this.CategoryMap.ContainsKey(category.Name))
			flags |= ImGuiTreeNodeFlags.Leaf;
		
		return ImRaii.TreeNode(
			this._locale.GetCategoryName(category),
			flags
		);
	}
	
	// Category info

	private BoneCategory? Selected;

	private void DrawCategoryInfo() {
		ImGui.TableNextColumn();
		if (this.Selected == null) return;
		
		ImGui.Text(this._locale.GetBoneName(this.Selected.Name));
	}
}
