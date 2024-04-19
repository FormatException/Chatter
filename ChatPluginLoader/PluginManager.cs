using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChatPluginLoader
{
    public class PluginManager
    {
        public static List<T> LoadPlugins<T>(string pluginPath, params object[] objectsOnCtor) where T : class
        {
            //We'll look for our T here and if found load all of the other dlls in the directory into the current domain.
            //The problem is that even though we've loaded them, .NET doesn't know how to resolve any child libraries.  The
            //child libaries are called on when T is instantiated so hopefully we've already loaded them all.  All we're
            //going to do in the AssemblyResolve is look in the current domains loaded assemblies and point back to them.
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            //Note that this takes a very loosey-goosey approach to loading the assemblies.  I'm assuming the caller has vetted
            //the assemblies here as it's just loading everything in to the current domain and that has security implications.

            List<T> values = new List<T>();
            var dllFiles = Directory.GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories);

            List<Type> classesToLoad = new List<Type>();
            foreach (var dllFile in dllFiles)
            {
                var assembly = Assembly.LoadFile(dllFile);

                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.BaseType == typeof(T))
                        classesToLoad.Add(type);
                }
            }
            foreach (var type in classesToLoad)
            {
                var obj = Activator.CreateInstance(type, objectsOnCtor) as T;
                if (obj != null)
                    values.Add(obj);
            }

            return values;
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs e)
        {
            try
            {
                //try and find the assembly in the existing domain
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var existingAssembly = assemblies.FirstOrDefault(x => x.FullName == e.Name);
                if (existingAssembly != null)
                    return existingAssembly;

                if (File.Exists(e.Name))
                {
                    return Assembly.LoadFrom(e.Name);
                }
                
                // During probing for satellite assemblies it can happen that an assembly does not exists.
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AssemblyResolve: " + ex);
                return null;
            }
        }
    }
}
