using Dalamud.Logging;
using Ktisis.Structs.Actor.State;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Ktisis.Interface.Modular {
	internal class Manager {


		public delegate void Spliters(string handle, List<Action>? contents);
		public static Spliters? OnSpliters = null;


		private static MethodInfo[] AvailableContainers = typeof(Container).GetMethods(BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo[] AvailableSpliters = typeof(Spliter).GetMethods(BindingFlags.Public | BindingFlags.Static);
		private static MethodInfo[] AvailablePanels = typeof(Panel).GetMethods(BindingFlags.Public | BindingFlags.Static);



		public delegate void CI(ContentsInfo ci);


		// Acts as deserialized json for testing
		private static List<ConfigObject> Test = new() {
			new("Window", new() {
				new ConfigObject("ActorsList")
			}),
			new("Window", new() {
			}),
		};

		public static List<(Delegate, ContentsInfo)>? Config = new();

		public static List<string> Handles = new();


		public static void Init() {

			PluginLog.Debug($"av c:{string.Join(", ", AvailableContainers.Select(i => i.Name.ToString()))}");
			PluginLog.Debug($"av s:{string.Join(", ", AvailableSpliters.Select(i => i.Name.ToString()))}");
			PluginLog.Debug($"av p:{string.Join(", ", AvailablePanels.Select(i => i.Name.ToString()))}");



			Config = ListConfigObjectToDelegate(Test);
			//Config = ListConfigObjectToDelegate(Ktisis.Configuration.ModularConfig);
		}



		public static List<(Delegate, ContentsInfo)>? ListConfigObjectToDelegate(List<ConfigObject>? configObjects) {
			if (configObjects == null) return null;
			List<(Delegate, ContentsInfo)> listDelegateAndInfo = new();
			foreach (var o in configObjects) {
				var delegateAndInfo = ConfigObjectToDelegate(o);
				if (delegateAndInfo == null) continue;
				listDelegateAndInfo.Add(((Delegate, ContentsInfo))delegateAndInfo);
			}
			if (listDelegateAndInfo.Any())
				return listDelegateAndInfo;
			return null;
		}
		public static (Delegate, ContentsInfo)? ConfigObjectToDelegate(ConfigObject configObject) {

			Type? type = AvailableContainers.FirstOrDefault(i => i.Name == configObject.Name)?.DeclaringType;
			if (type == null)
				type = AvailableSpliters.FirstOrDefault(i => i.Name == configObject.Name)?.DeclaringType;
			if (type == null)
				type = AvailablePanels.FirstOrDefault(i => i.Name == configObject.Name)?.DeclaringType;
			if (type == null) return null;
			PluginLog.Debug($" type {configObject.Name}: {type?.Name}");




			MethodInfo? mi = type?.GetMethod(configObject.Name, BindingFlags.Public | BindingFlags.Static);
			if (mi != null) {


				var reflectionDelgate = Delegate.CreateDelegate(typeof(CI), mi);



				string handle = $"Window {Handles.Count}##Modular##{Handles.Count}";
				PluginLog.Debug($" mi not null {reflectionDelgate.Method.Name} {handle}");
				Handles.Add(handle);


				List<(Delegate, ContentsInfo)>? actions = null;
				if(configObject.Contents != null)
					actions = ListConfigObjectToDelegate(configObject.Contents);


				var ci = new ContentsInfo {
					Handle = handle,
					Actions = actions
				};

				return (reflectionDelgate, ci);
			}
			return null;
		}
		public static void Dispose() => OnSpliters = null;

		public static void Render() => Config?.ForEach(d => d.Item1.DynamicInvoke(d.Item2));


		public static void DrawConfigTab(Configuration cfg) {

		}
	}

	internal class ModItem {
		public StringHandle handle { get; set; }
	}
	public class ContentsInfo {
		public string Handle = "##modular##handle";
		public List<(Delegate, ContentsInfo)>? Actions = null;
	}

	[Serializable]
	public class ConfigObject {
		public string Name;
		public List<ConfigObject>? Contents;

		public ConfigObject(string name, List<ConfigObject>? contents = null) {
			this.Name = name;
			this.Contents = contents;
		}
	}
}
