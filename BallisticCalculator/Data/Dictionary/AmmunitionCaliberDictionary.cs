using Gehtsoft.Measurements;
using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BallisticCalculator.Data.Dictionary
{
    /// <summary>
    /// The dictionary of ammunition calibers
    /// </summary>
    public class AmmunitionCaliberDictionary : IReadOnlyList<AmmunitionCaliber>
    {
        private readonly List<AmmunitionCaliber> mList = new List<AmmunitionCaliber>();

        /// <summary>
        /// Returns an item by its index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public AmmunitionCaliber this[int index] => mList[index];

        /// <summary>
        /// Returns the number of the items
        /// </summary>
        public int Count => mList.Count;

        /// <summary>
        /// Returns a enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<AmmunitionCaliber> GetEnumerator() => mList.GetEnumerator();

        /// <summary>
        /// Returns a enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds an element to the list
        /// </summary>
        /// <param name="value"></param>
        public void Add(AmmunitionCaliber value) => mList.Add(value);

        /// <summary>
        /// Removes an element from the list
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => mList.RemoveAt(index);

        /// <summary>
        /// Finds an index of the item that matches the predicate specified
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public int IndexOf(Func<AmmunitionCaliber, bool> predicate)
        {
            for (int i = 0; i < mList.Count; i++)
                if (predicate(mList[i]))
                    return i;
            return -1;
        }

        /// <summary>
        /// Finds the the item that matches the predicate specified
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public AmmunitionCaliber Find(Func<AmmunitionCaliber, bool> predicate)
        {
            var index = IndexOf(predicate);
            if (index < 0)
                return null;
            return mList[index];
        }
    }

    /// <summary>
    /// <para>The factory to read a AmmunitionCaliber dictionary.</para>
    /// <para>The factory can read a stream or file, or create a default set calibers.</para>
    /// </summary>
    public static class AmmunitionCaliberFactory
    {

        /// <summary>
        /// Creates a dictionary and prefills it with the default set
        /// </summary>
        /// <returns></returns>
        public static AmmunitionCaliberDictionary Create()
        {
            var dictionary = new AmmunitionCaliberDictionary();
            using var stream = typeof(AmmunitionCaliberFactory).Assembly.GetManifestResourceStream("BallisticCalculator.Resources.Calibers.csv");
            dictionary.ReadStream(stream);
            return dictionary;
        }

        /// <summary>
        /// <para>Reads the stream as a dictonary</para>
        /// <para>The stream is expected to be a text with the following format</para>
        /// <para>1. One line of the file is one caliber</para>
        /// <para>2. Fields are separated with semicolon (`;`)</para>
        /// <para>3. Fields are:</para>
        /// <para>3a. Type: pistol, rifle, shotgun or cannon</para>
        /// <para>3b. Group: a caliber group in inches or millimeters</para>
        /// <para>3c. Bullet diameter in inches or millimeters</para>
        /// <para>3d. The primary name</para>
        /// <para>3e. The optional names separated by a comma</para>
        /// <para>Example:</para>
        /// <para>`rifle;4mm;0.172';.17 HMR;.17 Hornady Magnum Rimfire, .17 Hornady Magnum`</para>
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <param name="ignoreErrors"></param>
        public static void ReadStream(this AmmunitionCaliberDictionary dictionary, Stream stream, Encoding encoding = null, bool ignoreErrors = false)
        {
            using var ts = new StreamReader(stream, encoding ?? Encoding.ASCII, true, 4096, true);
            int ln = 0;
            while (true)
            {
                ln++;
                var line = ts.ReadLine();
                if (line == null)
                    break;
                if (string.IsNullOrEmpty(line))
                    continue;
                try
                {
                    var item = ParseLine(line, ln);
                    dictionary.Add(item);
                }
                catch (Exception)
                {
                    if (ignoreErrors)
                        continue;
                    throw;
                }
            }
        }

        private static AmmunitionCaliber ParseLine(string line, int ln)
        {
            var parts = line.Split(';');
            if (parts.Length != 5)
                throw new ArgumentException($"The line must be exactly 5 fields separated by semicolon (line {ln})", nameof(line));
            var type = parts[0] switch
            {
                "pistol" => AmmunitionCaliberType.Pistol,
                "rifle" => AmmunitionCaliberType.Rifle,
                "shotgun" => AmmunitionCaliberType.Shotgun,
                "cannon" => AmmunitionCaliberType.Cannon,
                _ => throw new ArgumentException($"The ammunition type {parts[0]} is unknown (line {ln})", nameof(line)),
            };

            Measurement<DistanceUnit>? group = null, diameter;

            if (!string.IsNullOrEmpty(parts[1]))
            {
                if (Measurement<DistanceUnit>.TryParse(CultureInfo.InvariantCulture, parts[1], out var x1))
                    group = x1;
                else
                    throw new ArgumentException($"Can't parse group '{parts[1]}' (line {ln})", nameof(line));
            }

            if (Measurement<DistanceUnit>.TryParse(CultureInfo.InvariantCulture, parts[2], out var x2))
                diameter = x2;
            else
                throw new ArgumentException($"Can't parse bullet diameter '{parts[2]}' (line {ln})", nameof(line));


            if (string.IsNullOrEmpty(parts[3]))
                throw new ArgumentException($"No name is defined (line {ln})", nameof(line));

            return new AmmunitionCaliber(type, group, diameter.Value, parts[3], parts[4]);
        }
    }
}
