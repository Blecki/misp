using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public interface ILibraryInterface
    {
        bool BindLibrary(Engine engine);
    }

    public static class NetModule
    {
        public static bool LoadModule(Engine engine, String assemblyName, String moduleName)
        {
            var assembly = System.Reflection.Assembly.LoadFrom(assemblyName);
            if (assembly == null) return false;
            var moduleType = assembly.GetType(moduleName);
            if (moduleType == null) return false;
            var module = Activator.CreateInstance(moduleType) as ILibraryInterface;
            if (module != null) return module.BindLibrary(engine);
            else return false;
        }
    }
}
