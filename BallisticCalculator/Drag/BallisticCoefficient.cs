using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace BallisticCalculator
{
    /// <summary>
    /// The type of the ballisitic coefficient value
    /// </summary>
    public enum BallisticCoefficientValueType
    {
        /// <summary>
        /// Coefficient.
        /// 
        /// The typical BC value. 
        /// 
        /// It is proportion of the bullet sectional density to 
        /// the sectional density of the original table's bullet
        /// </summary>
        Coefficient,
        
        /// <summary>
        /// The form factor
        /// 
        /// The coefficient showing how the bullet's behavior rely to
        /// the the original bullet. 
        /// 
        /// If you use form factor, make sure that the bullet diameter and
        /// bullet weight are specified. 
        /// </summary>
        FormFactor,
    }

    /// <summary>
    /// The value representing ballistic coefficient
    /// </summary>
    public struct BallisticCoefficient : IEquatable<BallisticCoefficient>
    {
        /// <summary>
        /// The typeof of the value
        /// </summary>
        [JsonIgnore]
        public BallisticCoefficientValueType ValueType { get; set; } = BallisticCoefficientValueType.Coefficient;

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
        /// <param name="value"></param>
        /// <param name="table"></param>
        public BallisticCoefficient(double value, DragTableId table) : this(value, table, BallisticCoefficientValueType.Coefficient)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="table"></param>
        /// <param name="valueType"></param>
        public BallisticCoefficient(double coefficient, DragTableId table, BallisticCoefficientValueType valueType)
        {
            ValueType = valueType;
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

            if (TryParse(text, CultureInfo.InvariantCulture, out var value, out var table, out var valueType))
            {
                ValueType = valueType;
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
            if (!TryParse(text, culture, out var value, out var table, out var valueType))
            {
                bc = new BallisticCoefficient(1, DragTableId.G1);
                return false;
            }
            bc = new BallisticCoefficient(value, table, valueType);
            return true;
        }

        private static bool TryParse(string text, CultureInfo cultureInfo, out double value, out DragTableId table, out BallisticCoefficientValueType valueType)
        {
            value = 0;
            table = DragTableId.G1;
            valueType = BallisticCoefficientValueType.Coefficient;

            if (text.Length < 3)
                return false;
            
            if (text[0] == 'F')
            {
                text = text.Substring(1);
                if (text.Length < 3)
                    return false;
                
                valueType = BallisticCoefficientValueType.FormFactor;
            }

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
        public string ToString(string format, CultureInfo ci) => $"{(ValueType == BallisticCoefficientValueType.FormFactor ? "F" : "")}{(format == null ? Value.ToString(ci) : Value.ToString(format, ci))}{Table}";

        /// <summary>
        /// Checks whether the object equals to other object
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(BallisticCoefficient other)
        {
            return ValueType == other.ValueType && 
                Value == other.Value && 
                Table == other.Table;
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
                hash = hash * 17 + ValueType.GetHashCode();
                hash = hash * 17 + Value.GetHashCode();
                hash = hash * 17 + Table.GetHashCode();
                return hash;
            }
        }
    }
}
