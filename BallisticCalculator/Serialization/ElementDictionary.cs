using System;
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

        private readonly Lazy<Dictionary<string, Type>> mReferences = new Lazy<Dictionary<string, Type>>(SearchAllTypes);

        private static Dictionary<string, Type> SearchAllTypes()
        {
            Dictionary<string, Type> dict = new Dictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
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

