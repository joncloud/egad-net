# Egad.NET
[![NuGet](https://img.shields.io/nuget/v/Egad.svg)](https://www.nuget.org/packages/Egad/)

## Description
Egad.NET provides additional `DataSet` conversion functionality beyond what is included in Json.NET. It includes functionality necessary to keep the DataSet's XML intact after serializing across JSON. In addition its faster than serializing to an `XmlWriter`!

## Licensing
Released under the MIT License.  See the [LICENSE][] file for further details.

## Benchmarks
### Complex
``` ini

BenchmarkDotNet=v0.10.14, OS=ubuntu 16.04
Intel Core i5-2400 CPU 3.10GHz (Sandy Bridge), 1 CPU, 4 logical and 4 physical cores
.NET Core SDK=2.1.101
  [Host]     : .NET Core 2.0.6 (CoreCLR 4.6.0.0, CoreFX 4.6.26212.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.6 (CoreCLR 4.6.0.0, CoreFX 4.6.26212.01), 64bit RyuJIT


```
|     Method |     Mean |     Error |    StdDev |   Gen 0 | Allocated |
|----------- |---------:|----------:|----------:|--------:|----------:|
|  Xml_Write | 281.6 us | 0.7950 us | 0.7436 us | 21.4844 |  67.39 KB |
|   Xml_Read | 694.2 us | 3.0124 us | 2.8178 us | 64.4531 | 200.52 KB |
| Json_Write | 129.0 us | 0.1562 us | 0.1461 us |  8.5449 |  26.61 KB |
|  Json_Read | 536.4 us | 0.7912 us | 0.7401 us | 24.4141 |  77.15 KB |


## Installation
In the Package Manager Console execute

```powershell
Install-Package Egad
```

Or update `*.csproj` to include a dependency on

```xml
<ItemGroup>
  <PackageReference Include="Egad" Version="0.1.0-*" />
</ItemGroup>
```

## Usage
Egad.NET builds on top of Json.NET. It works directly with `JsonSerializer` or `JsonSerializerSettings`. Use the `UseEgad()` extension method to start serializing DataSets.

```csharp
var dataSet = new DataSet("myDataSet");
var settings = new JsonSerializerSettings().UseEgad();
var json = JsonConvert.SerializeObject(dataSet, settings);
```