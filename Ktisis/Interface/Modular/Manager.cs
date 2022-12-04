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

		public static void Init() {
			Configurator.MovingObject = null;
		}
		public static void Dispose() {
			ItemIds.Clear();
		}
		public static void Render() => Ktisis.Configuration.ModularConfig.ForEach(d => d.Draw());
		public static int GenerateId() {
			int id = 0;
			if (ItemIds.Any())
				id = ItemIds.Max() + 1;
			ItemIds.Add(id);
			return id;
		}
		internal static IModularItem? CreateItemFromTypeName(string typeName) {

			// Get the type of desired instance
			Type? objectType = Available.FirstOrDefault(i => i.Name == typeName);
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
			return null;
		}
	}
}
