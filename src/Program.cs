using System;
using System.Collections.Generic;

namespace AwsPriceParser
{
    internal static class Program
    {
        /*
        private static readonly HashSet<string> AllowedSizes = new()
            {
                "r5d.large", "r5dn.large",
                "i3en.large",

                "m6id.large", "r6id.large",
                "m6idn.large", "r6idn.large",
                "i4i.large",

                "m7gd.large", "r7gd.large",
                "m8gd.large", "r8gd.large",
                "i8g.large",

                "i4i.xlarge",
                "m5dn.xlarge", // Intel Xeon Platinum 8259CL 2.5GHz Turbo:3.5GHz SingleThread:1948, 4 vCPUs, 16.0 GiB, 150 GB NVMe SSD
                "m5d.xlarge",
                "m5ad.xlarge", // AMD EPYC 7571 2.1GHz Turbo:2.9GHz SingleThread:1934, 4 vCPUs, 16.0 GiB, 150 GB NVMe SSD
                "c5ad.xlarge", // AMD EPYC 7R32 2.8GHz Turbo:3.3Ghz SingleThread:1925, 4 vCPUs, 8.0 GiB, 150 GB NVMe SSD
                "c6id.xlarge",
                "m6id.xlarge",
                "c5d.xlarge", // 100 GB NVMe SSD - it's smaller than others

                "r6id.2xlarge", "r6idn.2xlarge",
                "i4i.2xlarge",

                "c7gd.2xlarge", "m7gd.2xlarge", "r7gd.2xlarge",
                "c8gd.2xlarge", "m8gd.2xlarge", "r8gd.2xlarge",
                "i8g.2xlarge",
            };
            */

        private static bool IsAllowedSize(string size)
        {
            var p = AwsInstanceType.Parse(size);
            return p.Size is "large" or "xlarge" or "2xlarge" && (
                p.Series is "m" or "c" or "r" && p.Generation is 7 or 8 && p.Options.Contains('g') && p.Options.Contains('d') ||
                p.Series is "i"               && p.Generation is 7 or 8 && p.Options.Contains('g') ||
                p.Series is "m" or "c" or "r" && p.Generation is 6      && p.Options.Contains('i') && p.Options.Contains('d') ||
                p.Series is "m" or "c" or "r" && p.Generation is 5      && p.Options.Contains('d') ||
                p.Series is "i"               && p.Generation is 4      && p.Options.Contains('i'));
        }

        private static bool IsAllowedRegion(string s) => s.StartsWith("eu-");

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    throw new ArgumentException("Invalid argument count");

                var spotPrices = SpotJson.Read(args[0], IsAllowedRegion, IsAllowedSize);
                SpotDump.DumpMd(spotPrices, Console.Out);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
        }
    }
}