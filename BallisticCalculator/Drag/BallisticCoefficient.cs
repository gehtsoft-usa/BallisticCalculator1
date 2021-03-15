using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The value representing ballistic coefficient
    /// </summary>
    public struct BallisticCoefficient : IEquatable<BallisticCoefficient>
    {
        /// <summary>
        /// The value of the coefficient
        /// </summary>
        [JsonIgnore]
        public double Value { get; }

        /// <summary>
        /// The ballistic table
        /// </summary>
        [JsonIgnore]
        public DragTableId Table { get; }

        /// <summary>
        /// The text presentation of the value in the invariant culture
        /// </summary>
        [JsonPropertyName("Value")]
        public string Text => ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="table"></param>
        public BallisticCoefficient(double coefficient, DragTableId table)
        {
            Value = coefficient;
            Table = table;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text"></param>
        [JsonConstructor]
        public BallisticCoefficient(string text)
        {
            Value = 1;
            Table = DragTableId.G1;

            if (TryParse(text, CultureInfo.InvariantCulture, out double value, out DragTableId table))
            {
                Value = value;
                Table = table;
            }
        }

        /// <summary>
        /// Parses the ballistic coefficient for the current culture
        /// </summary>
        /// <param name="text"></param>
        /// <param name="bc"></param>
        /// <returns></returns>
        public static bool TryParse(string text, out BallisticCoefficient bc) => TryParse(text, CultureInfo.CurrentCulture, out bc);

        /// <summary>
        /// Parses the ballistic coefficient for the specified culture
        /// </summary>
        /// <param name="text"></param>
        /// <param name="culture"></param>
        /// <param name="bc"></param>
        /// <returns></returns>
        public static bool TryParse(string text, CultureInfo culture, out BallisticCoefficient bc)
        {
            if (!TryParse(text, culture, out double value, out DragTableId table))
            {
                bc = new BallisticCoefficient(1, DragTableId.G1);
                return false;
            }
            bc = new BallisticCoefficient(value, table);
            return true;
        }

        private static bool TryParse(string text, CultureInfo cultureInfo, out double value, out DragTableId table)
        {
            value = 0;
            table = DragTableId.G1;
            if (text.Length < 3)
                return false;
            string tableName = text.Substring(text.Length - 2);
            if (!Enum.TryParse<DragTableId>(tableName, out table))
                return false;
            string v = text.Substring(0, text.Length - 2);
            return double.TryParse(v, NumberStyles.Float, cultureInfo, out value);
        }

        /// <summary>
        /// Converts value to string in the current culture
        /// </summary>
        /// <returns></returns>
        override public string ToString() => ToString(CultureInfo.CurrentCulture);

        /// <summary>
        /// Converts value to string in the specified culture
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public string ToString(CultureInfo ci) => ToString(null, ci);

        /// <summary>
        /// Converts value to string using the specified format and the specified culture
        /// </summary>
        /// <param name="format"></param>
        /// <param name="ci"></param>
        /// <returns></returns>
        public string ToString(string format, CultureInfo ci) => $"{(format == null ? Value.ToString(ci) : Value.ToString(format, ci))}{Table}";

        /// <summary>
        /// Checks whether the object equals to other object
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(BallisticCoefficient other)
        {
            return Value == other.Value && Table == other.Table;
        }

        /// <summary>
        /// Checks whether object equals to another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is BallisticCoefficient bc)
                return Equals(bc);
            return false;
        }

        /// <summary>
        /// Returns the object hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 21;
                hash = hash * 17 + Value.GetHashCode();
                hash = hash * 17 + Table.GetHashCode();
                return hash;
            }
        }
    }
}
