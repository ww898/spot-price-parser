using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SpotPriceParser
{
  internal static class Program
  {
    private static readonly IReadOnlySet<string> ourAllowedSizes = new HashSet<string>
      {
        "r5d.large", "r5dn.large",
        "i3en.large",
        
        "m6id.large", "r6id.large",
        "m6idn.large", "r6idn.large",
        "i4i.large",
        
        "m7gd.large", "r7gd.large",
        "m8gd.large", "r8gd.large",
        "i8g.large",

        "r6id.2xlarge", "r6idn.2xlarge",
        "i4i.2xlarge",
        
        "c7gd.2xlarge", "m7gd.2xlarge", "r7gd.2xlarge",
        "c8gd.2xlarge", "m8gd.2xlarge", "r8gd.2xlarge",
        "i8g.2xlarge"
      };

    private static readonly IReadOnlyDictionary<string, string> ourAllowedRegions = new Dictionary<string, string>
      {
        { "af-south-1", "Africa (Cape Town)" },
        { "af-south-1-los-1", "Nigeria (Lagos)" },
        { "ap-east-1", "Asia Pacific (Hong Kong)" },
        { "ap-northeast-1", "Asia Pacific (Tokyo)" },
        { "ap-northeast-1-tpe-1", "Taiwan (Taipei)" },
        { "ap-northeast-2", "Asia Pacific (Seoul)" },
        { "ap-northeast-3", "Asia Pacific (Osaka)" },
        { "ap-south-1", "Asia Pacific (Mumbai)" },
        { "ap-south-1-ccu-1", "India (Kolkata)" },
        { "ap-south-1-del-1", "India (Delhi)" },
        { "ap-south-2", "Asia Pacific (Hyderabad)" },
        { "ap-southeast-1", "Asia Pacific (Singapore)" },
        { "ap-southeast-1-bkk-1", "Thailand (Bangkok)" },
        { "ap-southeast-1-mnl-1", "Philippines (Manila)" },
        { "ap-southeast-2", "Asia Pacific (Sydney)" },
        { "ap-southeast-2-akl-1", "New Zealand (Auckland)" },
        { "ap-southeast-2-per-1", "Australia (Perth)" },
        { "ap-southeast-3", "Asia Pacific (Jakarta)" },
        { "ap-southeast-4", "Asia Pacific (Melbourne)" },
        { "ap-southeast-5", "Asia Pacific (Malaysia)" },
        { "ap-southeast-7", "Asia Pacific (Thailand)" },
        { "ca-central-1", "Canada (Central)" },
        { "ca-west-1", "Canada West (Calgary)" },
        { "eu-central-1", "Europe (Frankfurt)" },
        { "eu-central-1-ham-1", "Germany (Hamburg)" },
        { "eu-central-1-waw-1", "Poland (Warsaw)" },
        { "eu-central-2", "Europe (Zurich)" },
        { "eu-north-1", "Europe (Stockholm)" },
        { "eu-north-1-cph-1", "Denmark (Copenhagen)" },
        { "eu-north-1-hel-1", "Finland (Helsinki)" },
        { "eu-south-1", "Europe (Milan)" },
        { "eu-south-2", "Europe (Spain)" },
        { "eu-west-1", "Europe (Ireland)" },
        { "eu-west-2", "Europe (London)" },
        { "eu-west-3", "Europe (Paris)" },
        { "il-central-1", "Israel (Tel Aviv)" },
        { "me-central-1", "Middle East (UAE)" },
        { "me-south-1", "Middle East (Bahrain)" },
        { "me-south-1-mct-1", "Oman (Muscat)" },
        { "mx-central-1", "Mexico (Central)" },
        { "sa-east-1", "South America (São Paulo)" },
        { "us-east-1", "US East (N. Virginia)" },
        { "us-east-1-atl-1", "US East (Atlanta)" },
        { "us-east-1-atl-2", "US East (Atlanta) 2" },
        { "us-east-1-bos-1", "US East (Boston)" },
        { "us-east-1-bue-1", "Argentina (Buenos Aires)" },
        { "us-east-1-chi-1", "US East (Chicago)" },
        { "us-east-1-chi-2", "US East (Chicago) 2" },
        { "us-east-1-dfw-1", "US East (Dallas)" },
        { "us-east-1-dfw-2", "US East (Dallas) 2" },
        { "us-east-1-iah-1", "US East (Houston)" },
        { "us-east-1-iah-2", "US East (Houston) 2" },
        { "us-east-1-lim-1", "Peru (Lima)" },
        { "us-east-1-mci-1", "US East (Kansas City) 2" },
        { "us-east-1-mia-1", "US East (Miami)" },
        { "us-east-1-mia-2", "US East (Miami) 2" },
        { "us-east-1-msp-1", "US East (Minneapolis)" },
        { "us-east-1-nyc-1", "US East (New York City)" },
        { "us-east-1-nyc-2", "US East (New York City) 2" },
        { "us-east-1-phl-1", "US East (Philadelphia)" },
        { "us-east-1-qro-1", "México (Querétaro)" },
        { "us-east-1-scl-1", "Chile (Santiago)" },
        { "us-east-2", "US East (Ohio)" },
        { "us-west-1", "US West (N. California)" },
        { "us-west-2", "US West (Oregon)" },
        { "us-west-2-den-1", "US West (Denver)" },
        { "us-west-2-hnl-1", "US West (Honolulu)" },
        { "us-west-2-las-1", "US West (Las Vegas)" },
        { "us-west-2-lax-1", "US West (Los Angeles)" },
        { "us-west-2-lax-1b", "US West (Los Angeles)" },
        { "us-west-2-pdx-1", "US West (Portland)" },
        { "us-west-2-phx-1", "US West (Phoenix)" },
        { "us-west-2-phx-2", "US West (Phoenix) 2" },
        { "us-west-2-sea-1", "US West (Seattle)" },
      };

    private static readonly Regex ourAwsEc2Regex = new(@"^(?'series'[A-Za-z]+)(?'generation'\d*)(?'options'.*)\.(?'size'.+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static bool FilterRegion(string region) => region.StartsWith("eu-");

    [SuppressMessage("ReSharper", "UnusedVariable")]
    static Program()
    {
      foreach (var instanceName in new[]
                 {
                   "m8g.metal-24xl",
                   "hpc7g.16xlarge",
                   "i8g.16xlarge",
                   "is4gen.medium",
                   "u-12tb1.112xlarge",
                   "x1.32xlarge",
                   "u7i-8tb.112xlarge",
                 })
      {
        var parts = new AwsEc2Parts(instanceName);
      }
    }

    private static readonly IComparer<string> ourAwsEc2InstanceTypeNameComparer = Comparer<string>.Create((x, y) =>
      {
        var xp = new AwsEc2Parts(x);
        var yp = new AwsEc2Parts(y);
        var res = string.Compare(xp.Size, yp.Size, StringComparison.InvariantCulture);
        if (res != 0)
          return res;
        res = xp.Generation.CompareTo(yp.Generation);
        if (res != 0)
          return res;
        res = string.Compare(xp.Series, yp.Series, StringComparison.InvariantCulture);
        if (res != 0)
          return res;
        res = string.Compare(xp.Options, yp.Options, StringComparison.InvariantCulture);
        if (res != 0)
          return res;
        return 0;
      });

    private static readonly IReadOnlyDictionary<string, string> ourColumns = new Dictionary<string, string>
      {
        { "mswin", "Windows" },
        { "linux", "Linux" },
      };

    private static int Main(string[] args)
    {
      try
      {
        if (args.Length != 1)
          throw new ArgumentException("Invalid argument count");
        using var stream = File.Open(args[0], FileMode.Open, FileAccess.Read, FileShare.Read);
        var root = JsonSerializer.Deserialize<JsonRoot>(stream);
        if (Math.Abs(root!.vers - 0.01) > 0.00000001)
          throw new FormatException("Invalid version, 0.01 expected");
        var config = root.config;
        foreach (var column in ourColumns.Keys)
          if (!config.valueColumns.Contains(column))
            throw new FormatException($"{column} is expected");

        var regions = new HashSet<string>();
        var data = new Dictionary<string, SortedList<string, Dictionary<string, double>>>();
        foreach (var region in config.regions)
          if (FilterRegion(region.region))
          {
            var footnotes = region.footnotes;

            double? GetCurrencyValue(string value)
            {
              if (footnotes.asterisk != null && value.Length > 0 && value[^1] == '*')
                value = value[..^1];
              if (value == "N/A")
                return null;
              return double.Parse(value);
            }

            regions.Add(region.region);
            foreach (var instanceType in region.instanceTypes)
            foreach (var size in instanceType.sizes)
              if (ourAllowedSizes.Contains(size.size))
                foreach (var valueColumn in size.valueColumns)
                {
                  if (!data.TryGetValue(valueColumn.name, out var os))
                    data.Add(valueColumn.name, os = new SortedList<string, Dictionary<string, double>>(ourAwsEc2InstanceTypeNameComparer));
                  if (!os.TryGetValue(size.size, out var row))
                    os.Add(size.size, row = new Dictionary<string, double>());
                  var usd = GetCurrencyValue(valueColumn.prices.USD);
                  if (usd != null)
                    row.Add(region.region, usd.Value);
                }
          }

        var orderedRegions = regions.OrderBy(x => x).ToArray();
        foreach (var (valueColumnName, os) in data)
        {
          Console.WriteLine($"### {ourColumns.GetValueOrDefault(valueColumnName, valueColumnName)}:");
          Console.WriteLine(orderedRegions.Aggregate(new StringBuilder("|Instance type|"), (b, r) => b.Append($"{ourAllowedRegions.GetValueOrDefault(r, "???")} ({r})").Append('|')));
          Console.WriteLine(orderedRegions.Aggregate(new StringBuilder("|---|"), (b, _) => b.Append(":---:|")));
          foreach (var (size, row) in os)
          {
            var min = double.MaxValue;
            var max = double.MinValue;
            var usds = orderedRegions.Select(region =>
              {
                if (!row.TryGetValue(region, out var usd))
                  return (double?)null;
                min = Math.Min(min, usd);
                max = Math.Max(max, usd);
                return usd;
              }).ToArray();
            Console.WriteLine(usds.Aggregate(new StringBuilder($"|{size}|"), (builder, mayBeUsd) =>
              {
                if (mayBeUsd == null)
                  return builder.Append("-|");
                var usd = mayBeUsd.Value;
                var isMin = Math.Abs(min - usd) < 0.00000001;
                var isMax = Math.Abs(max - usd) < 0.00000001;
                if (isMin)
                  builder.Append("**<span style=\"color:darkgreen;\">");
                else if (isMax)
                  builder.Append("**<span style=\"color:red;\">");
                builder.Append(usd.ToString("F4"));
                if (isMin || isMax)
                  builder.Append("</span>**");
                return builder.Append('|');
              }));
          }
        }
        return 0;
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e);
        return 1;
      }
    }

    private readonly struct AwsEc2Parts
    {
      public AwsEc2Parts(string instanceName)
      {
        var match = ourAwsEc2Regex.Match(instanceName);
        if (!match.Success)
          throw new InvalidOperationException($"Unsupported AWS EC2 instance name ${instanceName}");
        Series = match.Groups["series"].Value;
        uint.TryParse(match.Groups["generation"].Value, out Generation);
        Options = match.Groups["options"].Value;
        Size = match.Groups["size"].Value;
        if (Series.Length == 0)
          throw new InvalidOperationException($"Unsupported AWS EC2 instance name series ${Series}");
        if (Size.Length == 0)
          throw new InvalidOperationException($"Unsupported AWS EC2 instance name size ${Size}");
      }

      public readonly string Series;
      public readonly uint Generation;
      public readonly string Options;
      public readonly string Size;
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record JsonConfig(string rate, string[] valueColumns, string[] currencies, JsonRegions[] regions);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record JsonInstanceTypes(string type, JsonSizes[] sizes);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private record JsonPrices(string USD);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private record JsonRegions(string region, JsonFootnotes footnotes, JsonInstanceTypes[] instanceTypes);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private record JsonFootnotes
    {
      [JsonPropertyName("*")]
      public string? asterisk { get; set; }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private record JsonRoot(double vers, JsonConfig config);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private record JsonSizes(string size, JsonValueColumns[] valueColumns);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private record JsonValueColumns(string name, JsonPrices prices);
  }
}