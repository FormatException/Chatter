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

            foreach (var dllFile in dllFiles)
            {
                var assembly = System.Reflection.Assembly.LoadFile(dllFile);

                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.BaseType == typeof(T))
                    {
                        var dirPath = Path.GetDirectoryName(dllFile);

                        //note that exe is also a valid assembly but let's not mess with that right now
                        var localDlls = Directory.GetFiles(dirPath, "*.dll", SearchOption.TopDirectoryOnly);

                        foreach (var localDll in localDlls)
                        {
                            Assembly.LoadFile(localDll);
                        }

                        //okay, so we found our class, now we also need to load all the other dlls
                        //in here before we try to instantiate it
                        var obj = Activator.CreateInstance(type, objectsOnCtor) as T;
                        if (obj != null)
                            values.Add(obj);
                    }
                }
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
