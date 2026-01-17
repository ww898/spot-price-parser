using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AwsPriceParser
{
    public static class SpotDump
    {
        private static readonly Dictionary<string, string> OsNameMap = new()
            {
                { "mswin", "Windows" },
                { "linux", "Linux" },
            };

        public static void DumpMd(SpotJson.Result result, TextWriter writer)
        {
            var (regions, sizes, data) = result;
            var orderedRegions = regions.OrderBy(x => x).ToList();
            var orderedSizes = sizes.OrderBy(x => x, Definitions.AwsEc2InstanceTypeNameComparer).ToList();

            foreach (var (osName, os) in data)
            {
                writer.WriteLine($"### {OsNameMap.GetValueOrDefault(osName, osName)}:");
                writer.WriteLine(orderedRegions.Aggregate(new StringBuilder("|Instance type|"), (builder, region) => builder.Append($"{Definitions.GetRegionName(region) ?? "???"} ({region})|")));
                writer.WriteLine(orderedRegions.Aggregate(new StringBuilder("|---|"), (builder, _) => builder.Append(":---:|")));

                foreach (var size in orderedSizes)
                    if (os.TryGetValue(size, out var row))
                    {
                        var minUsd = double.MaxValue;
                        var maxUsd = double.MinValue;
                        var usds = orderedRegions.Select(region =>
                            {
                                if (!row.TryGetValue(region, out var usd))
                                    return (double?)null;
                                minUsd = Math.Min(minUsd, usd);
                                maxUsd = Math.Max(maxUsd, usd);
                                return usd;
                            }).ToArray();
                        writer.WriteLine(usds.Aggregate(new StringBuilder($"|{size}|"), (builder, mayBeUsd) =>
                            {
                                if (mayBeUsd == null)
                                    return builder.Append("-|");
                                var usd = mayBeUsd.Value;
                                var isMin = Math.Abs(minUsd - usd) < Definitions.Δ;
                                var isMax = Math.Abs(maxUsd - usd) < Definitions.Δ;
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
        }
    }
}