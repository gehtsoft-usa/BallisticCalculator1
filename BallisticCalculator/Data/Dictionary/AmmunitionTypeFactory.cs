namespace BallisticCalculator.Data.Dictionary
{
    /// <summary>
    /// A factory class to create a pre-filled ammunition type dictionary
    /// </summary>
    public static class AmmunitionTypeFactory
    {
        /// <summary>
        /// Creates a dictionary with the predefined types
        /// </summary>
        /// <returns></returns>
        public static AmmunitionTypeDictionary Create()
        {
            var dictionary = new AmmunitionTypeDictionary
            {
                new AmmunitionType("FMJ", "Full Metal Jacket"),
                new AmmunitionType("FMJBT", "Full Metal Jacketed Boat Tail"),
                new AmmunitionType("MC", "Metal Cased"),
                new AmmunitionType("HP", "Hollow Point"),
                new AmmunitionType("HPBT", "Hollow Point Boat Tail"),
                new AmmunitionType("JHP", "Jacketed Hollow Point"),
                new AmmunitionType("JFP", "Jacketed Flat Point"),
                new AmmunitionType("JSP", "Jacketed Soft Point"),
                new AmmunitionType("JRN", "Jacketed Round Nose"),
                new AmmunitionType("LRN", "Led Round Nose"),
                new AmmunitionType("EFMJ", "Expanding Full Metal Jacket"),
                new AmmunitionType("WC", "Wad Cutter"),
                new AmmunitionType("SWC", "Semi Wad Cutter"),
                new AmmunitionType("RFP", "Rounded Flat Point"),
                new AmmunitionType("AP", "Armor Piercing"),
                new AmmunitionType("API", "Armor Piercing Incendiary"),
                new AmmunitionType("FR", "Frangible"),
                new AmmunitionType("SL", "Slug")
            };
            dictionary.Sort((a, b) => a.Abbreviation.CompareTo(b.Abbreviation));
            return dictionary;
        }
    }


}
