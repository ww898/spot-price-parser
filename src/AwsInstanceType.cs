using System;
using System.Text;
using System.Text.RegularExpressions;

namespace AwsPriceParser
{
    public readonly struct AwsInstanceType
    {
        private static readonly Regex AwsInstanceTypeRegex = new(
            @"^(?'series'[a-z]+)(?'generation'\d*)(?'options'[a-z\d]*)(-(?'parameter'[a-z\d]+))?\.(?'size'[a-z\d-]+)$",
            RegexOptions.Compiled);

        public static AwsInstanceType Parse(string instanceType)
        {
            var match = AwsInstanceTypeRegex.Match(instanceType.ToLowerInvariant());
            if (!match.Success)
                throw new InvalidOperationException($"Unsupported AWS EC2 instance name ${instanceType}");
            var groups = match.Groups;
            uint.TryParse(groups["generation"].Value, out var generation);
            return new(
                groups["series"].Value,
                generation,
                groups["options"].Value,
                groups["parameter"].Value,
                groups["size"].Value);
        }

        public readonly string Series;
        public readonly uint Generation;
        public readonly string Options;
        public readonly string Parameter;
        public readonly string Size;

        private AwsInstanceType(string series, uint generation, string options, string parameter, string size)
        {
            if (series.Length == 0)
                throw new InvalidOperationException($"Unsupported AWS EC2 instance name series ${series}");
            if (size.Length == 0)
                throw new InvalidOperationException($"Unsupported AWS EC2 instance name size ${size}");
            Series = series;
            Generation = generation;
            Options = options;
            Parameter = parameter;
            Size = size;
        }

        public override string ToString()
        {
            var builder = new StringBuilder(Series);
            if (Generation != 0)
                builder.Append(Generation);
            builder.Append(Options);
            if (Parameter.Length != 0)
                builder.Append('-').Append(Parameter);
            return builder.Append('.').Append(Size).ToString();
        }
    }
}