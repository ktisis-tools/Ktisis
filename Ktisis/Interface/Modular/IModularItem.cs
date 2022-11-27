using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.Interface.Modular {

	public interface IModularItem {
		public void Draw();
	}
	public interface IModularContainer : IModularItem {
		public List<IModularItem> Items { get; }
	}
}
