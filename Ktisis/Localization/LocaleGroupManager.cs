using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Numerics;

using Ktisis.Core.Attributes;

namespace Ktisis.Localization;

[Singleton]
public class LocaleGroupManager : IDisposable {
	private LocaleManager Manager;

	private readonly Dictionary<string, List<string>> RegisteredGroups = new ();
	private readonly Dictionary<string, (Vector2 Minimum, Vector2 Maximum)> StringBoundaries = new ();
	
	public LocaleGroupManager(LocaleManager manager) {
		this.Manager = manager;
	}

	public void Initialize() {
		this.Manager.LocaleChanged += this.OnLocaleChanged;
	}
	
	private void OnLocaleChanged() {
		this.StringBoundaries.Clear();
		
		var types = Assembly.GetExecutingAssembly().GetTypes();
		var attributes = types
			.SelectMany(t => t.GetMembers())
			.SelectMany(t => t.GetCustomAttributes<LocaleGroupAttribute>(true));

		foreach (var attr in attributes) {
			var (group, set) = attr switch {
				PatternLocaleGroupAttribute patternAttribute => patternAttribute.Get(this.Manager),
				_ => attr.Get(this.Manager),
			};
			Ktisis.Log.Debug(string.Join(", ", set));
			this.Register(group, set);
			Ktisis.Log.Debug($"Added {group}");
		}
	}

	public void Register(string groupKey, string[] localeKeys, bool amend = true) {
		var hasKey = this.RegisteredGroups.TryGetValue(groupKey, out var existingGroup);
		this.RegisteredGroups[groupKey] = existingGroup != null && amend 
			? [
				.. localeKeys,
				.. existingGroup
			]
			: [.. localeKeys];
	}

	public float GetMaxX(string groupKey) {
		if (!this.StringBoundaries.ContainsKey(groupKey))
			this.CalculateStringWidth(groupKey);

		return this.StringBoundaries[groupKey].Maximum.X;
	}

	public IDisposable ApplyMaxX(string groupKey) => this.ApplyMaxX(groupKey, ImGui.GetContentRegionAvail().X - ImGui.GetCursorPosX());
	public IDisposable ApplyMaxX(string groupKey, float maxWidth) {
		var groupWidth = this.GetMaxX(groupKey);
		return ImRaii.ItemWidth(maxWidth - groupWidth);
	}

	private (Vector2, Vector2) CalculateStringWidth(string groupKey) {
		this.RegisteredGroups.TryGetValue(groupKey, out var localeKeys);
		if (localeKeys == null)
			throw new NullReferenceException($"'{groupKey}' is not registered as a LocaleGroup");

		var stringBoundaryPair = localeKeys
			.Select(key => ImGui.CalcTextSize(this.Manager.Translate(key)))
			.Aggregate((Min: Vector2.PositiveInfinity, Max: Vector2.Zero), (pair, vec) => {
				if (pair.Min.X > vec.X) pair.Min = pair.Min.WithX(vec.X);
				if (pair.Max.X < vec.X) pair.Max = pair.Max.WithX(vec.X);
				if (pair.Min.Y > vec.Y) pair.Min = pair.Min.WithY(vec.Y);
				if (pair.Max.Y < vec.Y) pair.Max = pair.Max.WithY(vec.Y);
				return pair;
			});

		this.StringBoundaries[groupKey] = stringBoundaryPair;
		
		Ktisis.Log.Debug($"{groupKey} bounds: {stringBoundaryPair.Min}, {stringBoundaryPair.Max}");
		Ktisis.Log.Debug(string.Join(", ", localeKeys));

		return stringBoundaryPair;
	}

	public void Dispose() {
		this.Manager.LocaleChanged -= this.OnLocaleChanged;
		this.StringBoundaries.Clear();
		this.RegisteredGroups.Clear();
	}
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class LocaleGroupAttribute : Attribute {
	public readonly string GroupKey;
	public readonly string[] LocaleKeys;
	public LocaleGroupAttribute(string groupKey, string[] localeKeys) {
		this.GroupKey = groupKey;
		this.LocaleKeys = localeKeys;
	}

	public (string, string[]) Get(LocaleManager _) => (this.GroupKey, this.LocaleKeys);
}

public class PatternLocaleGroupAttribute : LocaleGroupAttribute {
	private readonly string Pattern;
	private readonly string[]? Filter;

	public PatternLocaleGroupAttribute(string groupKey, string[]? filter = null): base(groupKey, []) {
		this.Pattern = groupKey;
		this.Filter = filter;
	}
	
	public PatternLocaleGroupAttribute(string groupKey, string pattern, string[]? filter = null): base(groupKey, []) {
		this.Pattern = pattern;
		this.Filter = filter;
	}

	public new (string, string[]) Get(LocaleManager manager) {
		var matchingKeys = manager.Data?.KeysMatchingPattern(this.Pattern).ToArray() ?? [];
		return (this.GroupKey, this.Filter == null ? matchingKeys : matchingKeys.Where(key => this.Filter.Any(key.Contains)).ToArray());
	}
}