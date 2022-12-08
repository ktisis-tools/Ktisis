using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace Ktisis.Interface.Modular {

	[JsonConverter(typeof(IModularItemConverter))]
	public interface IModularItem {
		public string GetTitle();
		public void DrawConfig();
		public void Draw();
	}
	public interface IModularContainer : IModularItem {
		public List<IModularItem> Items { get; }
	}


	public class IModularItemConverter : JsonConverter {

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {

			var jo = JObject.Load(reader);

			var typeString = jo["$type"]?.ToString();
			if (typeString == null)
				throw new JsonReaderException();

			var type = Type.GetType(typeString);
			if (type == null)
				throw new JsonReaderException();

			var item = Manager.CreateItemFromTypeName(type.Name);
			if (item == null)
				throw new JsonReaderException();

			serializer.Populate(jo.CreateReader(), item);

			return item;
		}

		public override bool CanWrite {
			get { return false; }
		}

		public override bool CanConvert(Type objectType) {
			return false;
		}
	}
}
