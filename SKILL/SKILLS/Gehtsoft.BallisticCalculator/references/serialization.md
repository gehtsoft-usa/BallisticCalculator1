# Serialization & persistence

Save/load the library's data objects (`Ammunition`, `Rifle`, `Atmosphere`, `AmmunitionLibraryEntry`,
`ReticleDefinition`, …) and — importantly — **build your own file format around them**: embed library
objects inside your own XML document, or decorate your own classes so they serialize with the same
machinery. Two independent mechanisms are available:

- **BXml** — the library's own compact XML serializer (namespace `BallisticCalculator.Serialization`).
- **System.Text.Json** — the same data classes are also JSON-annotated and round-trip through
  `System.Text.Json.JsonSerializer` directly.

```csharp
using BallisticCalculator;
using BallisticCalculator.Serialization;
```

## 1. BXml — quick save / load

The easiest entry points are extension methods (`BallisticXmlExtensions`) plus static helpers.

```csharp
var ammo = new Ammunition(
    weight: new Measurement<WeightUnit>(168, WeightUnit.Grain),
    ballisticCoefficient: new BallisticCoefficient(0.223, DragTableId.G7),
    muzzleVelocity: new Measurement<VelocityUnit>(2750, VelocityUnit.FeetPerSecond));

// --- write ---
ammo.BallisticXmlSerialize(@"ammo.xml");        // to a file
using (var ms = new MemoryStream())
{
    ammo.BallisticXmlSerialize(ms);             // to a stream
    string xml = Encoding.UTF8.GetString(ms.ToArray());   // -> string (no direct string helper)
}

// --- read ---
Ammunition a1 = BallisticXmlDeserializer.ReadFromFile<Ammunition>(@"ammo.xml");
using (var input = File.OpenRead(@"ammo.xml"))
    Ammunition a2 = input.BallisticXmlDeserialize<Ammunition>();   // extension on Stream
```

Signatures:
- `void value.BallisticXmlSerialize(string fileName)` and `void value.BallisticXmlSerialize(Stream stream)` (extensions, `T : class`).
- `T stream.BallisticXmlDeserialize<T>()` (extension, `T : class`).
- Statics: `BallisticXmlSerializer.SerializeToFile<T>(T, string)`, `SerializeToStream<T>(T, Stream)`;
  `BallisticXmlDeserializer.ReadFromFile<T>(string)`, `ReadFromStream<T>(Stream)` (all `T : class`).
- There is **no** built-in serialize-to-string; use a `MemoryStream` as above.

## 2. BXml — embedding in your own document (build your own format)

`BallisticXmlSerializer.Serialize` returns a **detached** `XmlElement` owned by a document you control,
and never appends it itself — so it drops cleanly into any parent element you choose. This is the
supported way to wrap library data in your own schema.

```csharp
var doc = new XmlDocument();
var root = doc.CreateElement("my-loadout");
doc.AppendChild(root);
root.SetAttribute("profile", "match-308");

var serializer = new BallisticXmlSerializer(doc);   // reuse YOUR document
root.AppendChild(serializer.Serialize(ammo));       // place the element wherever you like
root.AppendChild(serializer.Serialize(rifle));
doc.Save(@"loadout.xml");

// read a child back out of your document:
var child = (XmlElement)root.SelectSingleNode("ammunition");
Ammunition back = new BallisticXmlDeserializer().Deserialize<Ammunition>(child);
```

Key members:
- `new BallisticXmlSerializer()` / `new BallisticXmlSerializer(XmlDocument document)`; `Document` property.
- `XmlElement Serialize(object value, string forceName = null)` — element is created in `Document`, not appended.
- `new BallisticXmlDeserializer()`; `T Deserialize<T>(XmlElement) where T : class`,
  `object Deserialize(XmlElement)` (auto-detects type by element name),
  `object Deserialize(XmlElement, Type)`, `object Deserialize(XmlElement, Type[] possibleTypes)`.
- `Deserialize<T>` returns `null` if the element's resolved type isn't `T`. The parameterless
  `Deserialize(XmlElement)` auto-detects by scanning `[BXmlElement]` names in **loaded** assemblies —
  so for your own custom types prefer the explicit `Deserialize<T>` / `Deserialize(element, type)`.

## 3. BXml — making your own class serializable

Decorate with four attributes. Values of supported primitive types (`Measurement<>`, enums, `double`,
`float`, `int`, `bool`, `string`, `DateTime`, `TimeSpan`, `BallisticCoefficient`, and their `Nullable<>`)
become **XML attributes**; richer members become child elements or collections.

| Attribute | Target | Purpose |
|---|---|---|
| `[BXmlElement("name")]` | class | **Required** on any serializable type; sets its element name. |
| `[BXmlProperty("name")]` | property | A value → XML attribute `name`. |
| `[BXmlProperty("n", Optional = true)]` | property | Null is allowed; otherwise null throws on read/write. |
| `[BXmlProperty(ChildElement = true)]` | property | Serialize as a nested element (target needs its own `[BXmlElement]`/`[BXmlSelect]`). |
| `[BXmlProperty(Name="items", Collection = true)]` | property | An `IEnumerable<T>` (with an `Add` method) of `[BXmlElement]` items, wrapped in `<items>`. `Name` required. |
| `[BXmlProperty("p", FlattenChild = true)]` | property | Inline the child's props as `p`-prefixed attributes (no nested element; not for children with collections). |
| `[BXmlConstructor]` | constructor | Constructor the deserializer calls; param names must match property names. Needed for immutable types. |
| `[BXmlSelect(typeof(A), typeof(B))]` | class/interface | Enumerate concrete subtypes for polymorphic child/collection members. |

If there is no `[BXmlConstructor]`, the deserializer uses the parameterless constructor + property
setters. Immutable library types (e.g. `Atmosphere`, whose properties are read-only) mark their
constructor `[BXmlConstructor]` instead.

```csharp
using BallisticCalculator.Serialization;
using Gehtsoft.Measurements;

[BXmlElement("my-load")]
public class MyLoad
{
    [BXmlProperty("name")]                       public string Name { get; set; }
    [BXmlProperty("charge", Optional = true)]    public Measurement<WeightUnit>? Charge { get; set; }
    [BXmlProperty(ChildElement = true)]          public Ammunition Ammunition { get; set; }
    [BXmlProperty(Name = "alternatives", Collection = true, Optional = true)]
    public List<Ammunition> Alternatives { get; } = new();

    public MyLoad() { }                          // used on read (no [BXmlConstructor] here)
}
```

The library's own classes are the reference examples: `AmmunitionLibraryEntry`
(`[BXmlElement("ammunition-library-entry")]`, attribute + optional + `ChildElement` props), `Rifle`
(`ChildElement` for `Sight`/`Rifling`/`Zero`), `Atmosphere` (`[BXmlConstructor]`), `ReticleDefinition`
(`Collection` + `FlattenChild`), `ReticleElement` (`[BXmlSelect]`).

## 4. System.Text.Json

The data classes carry `System.Text.Json.Serialization` attributes and round-trip through the standard
serializer with no special options:

```csharp
using System.Text.Json;
string json = JsonSerializer.Serialize(ammo);
Ammunition back = JsonSerializer.Deserialize<Ammunition>(json);
```

- Each type marks its deserialization constructor `[JsonConstructor]` (`Ammunition`, `Rifle`,
  `Atmosphere`, `AmmunitionLibraryEntry`, `BallisticCoefficient`).
- Optional properties use `[JsonIgnore(Condition = WhenWritingNull)]`; computed members (e.g.
  `Atmosphere.SoundVelocity`/`Density`) are `[JsonIgnore]`.
- `BallisticCoefficient` serializes as its single string form (e.g. `"0.223G7"`) and rebuilds via its
  `(string)` constructor.
- `Measurement<T>` JSON behavior comes from the `Gehtsoft.Measurements` package (it serializes as its
  text form); it round-trips with the standard serializer. If you need a guarantee for every field,
  round-trip-test your specific object once.

## Runtime note
Even the **BXml** path reflects over the data classes' `System.Text.Json` attributes at runtime, so
`System.Text.Json` (v9.x) must be loadable. It normally is — the package pulls it in transitively via
`Gehtsoft.Measurements` — so a standard `dotnet add package BallisticCalculator` needs nothing extra.
Only if your build trims/excludes it (or you reference the DLLs directly without their NuGet
dependencies) will BXml throw `FileNotFoundException` for `System.Text.Json`; add the package to fix it.
