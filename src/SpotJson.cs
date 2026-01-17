using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AwsPriceParser
{
    public static class SpotJson
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        private static class Schema
        {
            public record Config(string rate, string[] valueColumns, string[] currencies, Regions[] regions);

            public record InstanceTypes(string type, Sizes[] sizes);

            public record Prices(string USD);

            public record Regions(string region, Footnotes footnotes, InstanceTypes[] instanceTypes);

            public record Footnotes
            {
                [JsonPropertyName("*")] public string? asterisk { get; set; }
            }

            public record Root(double vers, Config config);

            public record Sizes(string size, ValueColumns[] valueColumns);

            public record ValueColumns(string name, Prices prices);
        }

        public record Result(
            HashSet<string> Regions,
            HashSet<string> Sizes,
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> Data);

        public static Result Read(
            string file,
            Predicate<string> filterRegion,
            Predicate<string> filterSize)
        {
            Schema.Root root;
            using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                root = JsonSerializer.Deserialize<Schema.Root>(stream)!;

            if (Math.Abs(root.vers - 0.01) >= Definitions.Δ)
                throw new FormatException("Invalid version, 0.01 expected");
            var config = root.config;
            if (!config.currencies.Contains(nameof(Schema.Prices.USD)))
                throw new FormatException("USD is not supported");

            var filteredRegions = new HashSet<string>();
            var filteredSizes = new HashSet<string>();

            var data = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            foreach (var (region, footnotes, instanceTypes) in config.regions)
                if (filterRegion(region))
                {
                    filteredRegions.Add(region);

                    double? GetCurrencyValue(string value)
                    {
                        if (footnotes.asterisk != null && value.Length > 0 && value[^1] == '*')
                            value = value[..^1];
                        if (value == "N/A")
                            return null;
                        return double.Parse(value);
                    }

                    foreach (var (_, sizes) in instanceTypes)
                    foreach (var (size, valueColumns) in sizes)
                        if (filterSize(size))
                        {
                            filteredSizes.Add(size);
                            foreach (var (name, prices) in valueColumns)
                            {
                                var usd = GetCurrencyValue(prices.USD);
                                if (usd != null)
                                {
                                    if (!data.TryGetValue(name, out var nameValue))
                                        data.Add(name, nameValue = new());

                                    if (!nameValue.TryGetValue(size, out var sizeValue))
                                        nameValue.Add(size, sizeValue = new());

                                    sizeValue.Add(region, usd.Value);
                                }
                            }
                        }
                }

            return new(filteredRegions, filteredSizes, data);
        }
    }
}