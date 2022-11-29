using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Util;

namespace Ktisis.Interface.Modular {
	internal class Manager {

		private const string NamespacePrefix = "Ktisis.Interface.Modular.ItemTypes.";

		internal static readonly List<Type> AvailableContainers = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Container").ToList();
		internal static readonly List<Type> AvailableSpliters = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Splitter").ToList();
		internal static readonly List<Type> AvailablePanel = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == NamespacePrefix + "Panel").ToList();
		internal static readonly List<Type> Available = AvailableContainers.Concat(AvailableSpliters).Concat(AvailablePanel).ToList();

		public static List<string> Handles = new();
		public static List<IModularItem> Config = new();

		public static void Init() {
			Handles.Clear();
			Configurator.MovingObject = null;
			Config = ParseConfigList(Ktisis.Configuration.ModularConfig)!;
		}
		public static void Dispose() => Config = new();
		public static void Render() => Config?.ForEach(d => d.Draw());

		private static List<IModularItem>? ParseConfigList(List<ConfigObject>? configObjects) {
			if (configObjects == null) return null;

			List<IModularItem> configList = new();
			foreach (var o in configObjects) {
				var item = ParseConfigItem(o);
				if (item == null) continue;
				configList.Add(item);
			}
			if (configList.Any())
				return configList;
			return null;
		}
		private static IModularItem? ParseConfigItem(ConfigObject configObject) {

			// Get the type of desired instance
			Type? objectType = Available.FirstOrDefault(i => i.Name == configObject.Type);
			if (objectType == null) return null;

			// Get available constructors
			var constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
			if (constructors == null || constructors.Length == 0) return null;

			object? instance = null;

			// if parameterless constructor exists, use it
			if (constructors.Any(c => c.GetParameters().Length == 0)) {
				instance = Activator.CreateInstance(objectType);
				if (instance != null) return (IModularItem)instance;
			}

			// create possible parameters
			string handle = $"Window {Handles.Count}##Modular##{Handles.Count}";
			var items = ParseConfigList(configObject.Items)!;
			Dictionary<int, object[]?> paramSolutions = new() {
				{0, new object[] { Handles.Count, handle, items }},
				{1, new object[] { Handles.Count, handle,  }},
				{2, new object[] { items }},
			};
			Handles.Add(handle);

			// check if any constructor is compatible with out parameters
			var compatibleParamIndex = AnyCompatibleConstructors(paramSolutions, constructors);
			if (compatibleParamIndex != null)
				if (paramSolutions.TryGetValue((int)compatibleParamIndex, out object[]? parameters)) {
					instance = Activator.CreateInstance(objectType, parameters);
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
					for (int i =0; i < ctorParameters.Length; i++) {

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
		public List<ConfigObject>? Items;

		public ConfigObject(string type, List<ConfigObject>? items = null) {
			this.Type = type;
			this.Items = items;
		}
	}
}
