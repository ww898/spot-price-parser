using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AwsPriceParser
{
  public static class Dump
  {
    public static void WriteMarkdown(TextWriter writer, string title, Dictionary<string, Dictionary<string, Dictionary<string, double>>> data)
    {
      var operationSystems = new HashSet<string>();
      var regions = new HashSet<string>();
      var instanceTypes = new HashSet<string>();
      foreach (var (osName, x) in data)
      {
        operationSystems.Add(osName);
        foreach (var (instanceType, y) in x)
        {
          instanceTypes.Add(instanceType);
          foreach (var (region, _) in y)
            regions.Add(region);
        }
      }

      var orderedOperationSystems = operationSystems.OrderBy(x => x).ToList();
      var orderedRegions = regions.OrderBy(x => x).ToList();
      var orderedInstanceTypes = instanceTypes.OrderBy(x => x, Definitions.AwsEc2InstanceTypeNameComparer).ToList();

      foreach (var operationSystem in orderedOperationSystems)
        if (data.TryGetValue(operationSystem, out var operationSystemValue))
        {
          writer.WriteLine($"### {title} for {operationSystem}:");
          writer.WriteLine(orderedRegions.Aggregate(new StringBuilder("|Instance type|"), (builder, region) => builder.Append($"{Definitions.GetRegionName(region) ?? "???"}</br>{region}|")));
          writer.WriteLine(orderedRegions.Aggregate(new StringBuilder("|---|"), (builder, _) => builder.Append(":---:|")));

          foreach (var instanceType in orderedInstanceTypes)
            if (operationSystemValue.TryGetValue(instanceType, out var instanceTypeValue))
            {
              var minUsd = double.MaxValue;
              var maxUsd = double.MinValue;
              var usds = orderedRegions.Select(region =>
                {
                  if (!instanceTypeValue.TryGetValue(region, out var usd))
                    return (double?)null;
                  minUsd = Math.Min(minUsd, usd);
                  maxUsd = Math.Max(maxUsd, usd);
                  return usd;
                }).ToArray();
              writer.WriteLine(usds.Aggregate(new StringBuilder($"|{instanceType}|"), (builder, mayBeUsd) =>
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