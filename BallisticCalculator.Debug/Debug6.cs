using BallisticCalculator.Serialization;
using System;

namespace BallisticCalculator.Debug
{
    internal static class Debug6
    {
        public static void Do(String[] args)
        {
            var le = BallisticXmlDeserializer.ReadLegacyAmmunitionLibraryEntryFromFile(args[0]);
            BallisticXmlSerializer.SerializeToFile<AmmunitionLibraryEntry>(le, args[1]);
        }
    }
}
