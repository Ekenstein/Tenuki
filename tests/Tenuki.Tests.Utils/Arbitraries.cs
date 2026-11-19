using System.Globalization;
using FsCheck.Fluent;

namespace Tenuki.Tests.Utils;

public static class Arbitraries
{
    public static readonly Arbitrary<string> EmptyString = Gen.Constant("")
        .Let(gen => Arb.From(gen, _ => []));
    public static readonly Arbitrary<string> NumberString = ArbMap
        .Default.ArbFor<int>()
        .Let(arb =>
        {
            var gen = arb.Generator.Select(n => n.ToString(CultureInfo.InvariantCulture));
            return Arb.From(
                gen,
                value =>
                {
                    var n = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    var values = arb.Shrinker(n);
                    return values.Select(x => x.ToString(CultureInfo.InvariantCulture));
                }
            );
        });

    public static readonly Arbitrary<string> RealNumberString = ArbMap
        .Default.ArbFor<decimal>()
        .Let(arb =>
        {
            var gen = arb.Generator.Select(n => n.ToString(CultureInfo.InvariantCulture));
            return Arb.From(
                gen,
                value =>
                {
                    var n = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    var values = arb.Shrinker(n);
                    return values.Select(x => x.ToString(CultureInfo.InvariantCulture));
                }
            );
        });

    public static Arbitrary<DifferentStrings> DifferentStrings()
    {
        var stringGen = ArbMap.Default.ArbFor<string>();

        var differentStringsGen =
            from value1 in ArbMap.Default.GeneratorFor<string>()
            from value2 in ArbMap.Default.GeneratorFor<string>()
            where value1 != value2
            select new DifferentStrings(value1, value2);

        IEnumerable<DifferentStrings> Shrinker(DifferentStrings current)
        {
            // Shrink value1 while keeping value2 static
            var shrinkV1 =
                from s1 in stringGen.Shrinker(current.Value1)
                where s1 != current.Value2
                select new DifferentStrings(s1, current.Value2);

            // Shrink value2 while keeping value1 static
            var shrinkV2 =
                from s2 in stringGen.Shrinker(current.Value2)
                where current.Value1 != s2
                select new DifferentStrings(current.Value1, s2);

            // Combine both shrinking paths
            return shrinkV1.Concat(shrinkV2);
        }

        return Arb.From(differentStringsGen, Shrinker);
    }
}
