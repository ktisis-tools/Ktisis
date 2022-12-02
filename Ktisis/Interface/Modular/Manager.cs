using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;

namespace Ktisis.Interface.Modular {
	internal class Manager {

		private const string NamespacePrefix = "Ktisis.Interface.Modular.ItemTypes.";

		internal static readonly List<Type> AvailableContainers = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Container").ToList();
		internal static readonly List<Type> AvailableSpliters = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Splitter").ToList();
		internal static readonly List<Type> AvailablePanel = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Panel").ToList();
		internal static readonly List<Type> Available = AvailableContainers.Concat(AvailableSpliters).Concat(AvailablePanel).ToList();

		public static List<int> ItemIds = new();
		public static List<IModularItem> Config = new();

		public static void Init() {
			Configurator.MovingObject = null;
			Config = ParseConfigList(Ktisis.Configuration.ModularConfig)!;
		}
		public static void Dispose() {
			Config = new();
			ItemIds.Clear();
		}
		public static void Render() => Config?.ForEach(d => d.Draw());
		public static int GenerateId() {
			int id = 0;
			if (ItemIds.Any())
				id = ItemIds.Max() + 1;
			ItemIds.Add(id);
			return id;
		}
		public static ParamsExtra GenerateExtra() =>
			new(new() { { "Id", GenerateId() } });

		private static List<IModularItem> ParseConfigList(List<ConfigObject>? configObjects) {
			if (configObjects == null) return new();

			List<IModularItem> configList = new();
			foreach (var o in configObjects) {
				var item = ParseConfigItem(o);
				if (item == null) continue;
				configList.Add(item);
			}
			if (configList.Any())
				return configList;
			return new();
		}
		private static IModularItem? ParseConfigItem(ConfigObject configObject) {

			// Get the type of desired instance
			Type? objectType = Available.FirstOrDefault(i => i.Name == configObject.Type);
			if (objectType == null) return null;

			// Get available constructors
			var constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			if (constructors == null || constructors.Length == 0) return null;

			object? instance = null;

			// create possible parameters
			var items = ParseConfigList(configObject.Items);
			ParamsExtra extra = configObject.Extra;

			Dictionary<int, object[]?> paramSolutions = new() {
				{0, new object[] { items, extra } }, // most Container and Splitter
				{1, new object[] { extra } }, // most Panel
			};

			// check if any constructor is compatible with out parameters
			var compatibleParamIndex = AnyCompatibleConstructors(paramSolutions, constructors);
			if (compatibleParamIndex != null)
				if (paramSolutions.TryGetValue((int)compatibleParamIndex, out object[]? parameters)) {
					instance = Activator.CreateInstance(objectType, parameters);
					if (instance != null) return (IModularItem)instance;
				}

			// if parameterless constructor exists, use it
			if (constructors.Any(c => c.GetParameters().Length == 0)) {
				instance = Activator.CreateInstance(objectType);
				if (instance != null) return (IModularItem)instance;
			}

			return null;
		}
		private static int? AnyCompatibleConstructors(Dictionary<int, object[]?> parametersSolutions, ConstructorInfo[]? constructors) {
			if (constructors == null) return null;
			foreach (var solu in parametersSolutions) {
				if (solu.Value == null) continue;
				int paramMatches = 0;
				foreach (var ctor in constructors) {
					paramMatches = 0;
					var ctorParameters = ctor.GetParameters();
					for (int i = 0; i < ctorParameters.Length; i++) {

						if (i < 0 || i >= solu.Value.Length) continue;
						var paramSolution = solu.Value.GetValue(i);
						if (paramSolution == null) continue;

						if (paramSolution.GetType() == ctorParameters[i].ParameterType)
							paramMatches++;
					}
					if (paramMatches == ctor.GetParameters().Length)
						return solu.Key;
				}
			}
			return null;
		}
	}

	[Serializable]
	public class ConfigObject {
		public string Type;
		public ParamsExtra Extra;
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public List<ConfigObject>? Items;
		public ConfigObject(string type, ParamsExtra extra, List<ConfigObject>? items = null) {
			this.Type = type;
			this.Extra = extra;
			this.Items = items;
		}
	}
	[Serializable]
	public class ParamsExtra {
		public Dictionary<string, int> Ints;
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, string>? Strings;
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, bool>? Bools;

		public ParamsExtra(Dictionary<string, int> ints, Dictionary<string, string>? strings = null, Dictionary<string, bool>? bools = null) {
			this.Strings = strings;
			this.Ints = ints;
			this.Bools = bools;
		}

		internal void SetInt(string key, int value) {
			if (this.Ints.ContainsKey(key))
				this.Ints[key] = value;
			else
				this.Ints.Add(key, value);
			Manager.Init();
		}
		internal void SetString(string key, string value) {
			if (this.Strings == null) this.Strings = new();
			if (this.Strings.ContainsKey(key))
				this.Strings[key] = value;
			else
				this.Strings.Add(key, value);
			Manager.Init();
		}
		internal void SetBool(string key, bool value) {
			if (this.Bools == null) this.Bools = new();
			if (this.Bools.ContainsKey(key))
				this.Bools[key] = value;
			else
				this.Bools.Add(key, value);
			Manager.Init();
		}
	}
}
