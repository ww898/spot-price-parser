using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AwsPriceParser
{
  public static class OnDemandJson
  {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private static class Schema
    {
      public record Root(
        string formatVersion,
        string offerCode,
        string version,
        Dictionary<string, Product> products,
        Terms terms);

      public record Product(Dictionary<string, string> attributes);

      public record Terms(Dictionary<string, Dictionary<string, Term>> OnDemand);

      public record Term(Dictionary<string, PriceDimension> priceDimensions);

      public record PriceDimension(string unit, PricePerUnit pricePerUnit);

      public record PricePerUnit(string USD);
    }

    public static Dictionary<string, Dictionary<string, Dictionary<string, double>>> Read(
      FileInfo file,
      Predicate<string> filterRegion,
      Predicate<string> filterInstanceType,
      Predicate<string> filterOperationSystem)
    {
      Schema.Root root;
      using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
        root = JsonSerializer.Deserialize<Schema.Root>(stream, new JsonSerializerOptions())!;

      if (root.formatVersion != "v1.0")
        throw new FormatException("Invalid version, v1.0 expected");
      if (root.offerCode != "AmazonEC2")
        throw new FormatException("Invalid offer code, AmazonEC2 expected");

      var data = new Dictionary<string, Dictionary<string, Dictionary<string, List<Tuple<double, string, Dictionary<string, string>>>>>>();
      foreach (var (productKey, tmp) in root.terms.OnDemand)
      foreach (var (_, term) in tmp)
      foreach (var (_, priceDimensions) in term.priceDimensions)
        if (priceDimensions.unit == "Hrs" && root.products.TryGetValue(productKey, out var product))
        {
          var attributes = product.attributes;
          if (attributes.TryGetValue("tenancy", out var tenancy) && tenancy == "Shared" &&
              attributes.TryGetValue("preInstalledSw", out var preInstalledSw) && preInstalledSw == "NA" &&
              attributes.TryGetValue("capacitystatus", out var capacitystatus) && capacitystatus == "Used" &&
              attributes.TryGetValue("licenseModel", out var licenseModel) && licenseModel == "No License required" &&
              attributes.TryGetValue("operatingSystem", out var operatingSystem) && filterOperationSystem(operatingSystem) &&
              attributes.TryGetValue("regionCode", out var regionCode) && filterRegion(regionCode) &&
              attributes.TryGetValue("instanceType", out var instanceType) && filterInstanceType(instanceType))
          {
            if (!data.TryGetValue(operatingSystem, out var operatingSystemValue))
              data.Add(operatingSystem, operatingSystemValue = new());

            if (!operatingSystemValue.TryGetValue(instanceType, out var instanceTypeValue))
              operatingSystemValue.Add(instanceType, instanceTypeValue = new());

            if (!instanceTypeValue.TryGetValue(regionCode, out var regionCodeValue))
              instanceTypeValue.Add(regionCode, regionCodeValue = new());

            var usd = double.Parse(priceDimensions.pricePerUnit.USD, CultureInfo.InvariantCulture);
            regionCodeValue.Add(Tuple.Create(usd, productKey, product.attributes));
          }
        }
      foreach (var (os, osValue) in data)
      foreach (var (instanceType, instanceTypeValue) in osValue)
      foreach (var (regionCode, regionCodeValue) in instanceTypeValue)
        if (regionCodeValue.Count > 1)
        {
          Console.WriteLine($"{os} {instanceType} {regionCode}");

          var same = new Dictionary<string, Tuple<bool, string?>>();
          foreach (var (_, _, attributes) in regionCodeValue)
          foreach (var (key, value) in attributes)
            if (!same.TryGetValue(key, out var sameValue))
              same.Add(key, Tuple.Create(true, (string?)value));
            else if (sameValue.Item1 && sameValue.Item2 != value)
              same[key] = Tuple.Create(false, (string?)null);

          foreach (var (usd, productKey, attributes) in regionCodeValue)
          {
            Console.WriteLine($"  {productKey} {usd}");
            foreach (var (key, sameValue) in same)
              if (attributes.TryGetValue(key, out var value) && !sameValue.Item1)
                Console.WriteLine($"    {key}={value}");
          }
        }

      return data
        .Select(x => KeyValuePair.Create(x.Key, x.Value
          .Select(y => KeyValuePair.Create(y.Key, y.Value
            .Select(z => KeyValuePair.Create(z.Key, z.Value.Single().Item1))
            .ToDictionary()))
          .ToDictionary()))
        .ToDictionary();
    }
  }
}