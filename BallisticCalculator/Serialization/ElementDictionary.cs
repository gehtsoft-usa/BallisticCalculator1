using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace BallisticCalculator.Serialization
{
    /// <summary>
    /// The dictionary of element names and corresponding types
    /// </summary>
    internal class ElementDictionary
    {
        public object SyncRoot { get; } = new object();

        private readonly Lazy<ConcurrentDictionary<string, Type>> mReferences = new Lazy<ConcurrentDictionary<string, Type>>(SearchAllTypes);

        private static ConcurrentDictionary<string, Type> SearchAllTypes()
        {
            ConcurrentDictionary<string, Type> dict = new ConcurrentDictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException )
                {
                    continue;
                }

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<BXmlElementAttribute>();
                    if (attr != null)
                        dict[attr.Name] = type;
                }
            }
            return dict;
        }

        /// <summary>
        /// Searcher for the type by a key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Type Search(string key)
        {
            if (!mReferences.Value.TryGetValue(key, out Type type))
                return null;
            return type;
        }
    }
}

