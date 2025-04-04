using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ktisis.Helpers
{
    public static class WinformsUtils
    {
        /// <summary>
        /// Retrieves file list from clipboard using winforms via reflection
        /// </summary>
        /// <returns></returns>
        public static List<string> ReadClipboardFiles()
        {
            var ret = new List<string>();
            try
            {
                var fType = Assembly.Load("System.Windows.Forms");
                var clipboard = fType.GetType("System.Windows.Forms.Clipboard");
                if((bool?)clipboard?.GetMethod("ContainsFileDropList")?.Invoke(null, []) == true)
                {
                    var cb = (StringCollection?)clipboard?.GetMethod("GetFileDropList")?.Invoke(null, []);
                    if(cb == null) throw new InvalidOperationException("There are no files in clipboard");
                    foreach(var f in cb)
                    {
                        if(f != null) ret.Add(f);
                    }
                }
            }
            catch(Exception e)
            {
                Ktisis.Log.Error($"{e}");
            }
            return ret;
        }
    }
}
