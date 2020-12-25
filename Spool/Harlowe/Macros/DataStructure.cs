using System;
using System.Collections.Generic;
using System.Linq;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        public DataSet DS(params Data[] values) => new DataSet(values);
        public DataSet DataSet(params Data[] values) => new DataSet(values);
        public DataMap DataMap(params Data[] pairs) {
            var map = new Dictionary<Data, Data>();
            for (int i = 1; i < pairs.Length; i += 2) {
                map[pairs[i - 1]] = pairs[i];
            }
            return new DataMap(map);
        }
        public DataMap DM(params Data[] pairs) => DataMap(pairs);
        public Array Array(params Data[] values) => new Array(values);
        public Array A(params Data[] values) => new Array(values);


        public Array range(double a, double b) => b < a ? range(b, a) :
            new Array(Enumerable.Range((int)a, ((int)b) - ((int)a) + 1).Select(x => new Number(x)));

        public Boolean AllPass(Filter filter, params Data[] values) => Boolean.Get(values.All(new Func<Data, bool>(filter)));
        public Boolean SomePass(Filter filter, params Data[] values) => Boolean.Get(values.Any(new Func<Data, bool>(filter)));
        public Boolean NonePass(Filter filter, params Data[] values) => Boolean.Get(values.All(x => !filter(x)));

        // TODO: altered
        public Number count(Array array, params Data[] testValues) => new Number(array.Count(x => System.Array.IndexOf(testValues, x) >= 0));
        public Number count(string text, params string[] testValues) => new Number(testValues.Sum(value => {
            int count = 0, minIndex = text.IndexOf(value, 0);
            while (minIndex != -1)
            {
                minIndex = text.IndexOf(value, minIndex + value.Length);
                count++;
            }
            return count;
        }));

        public Array DataPairs(DataMap map) => DataEntries(map);
        public Array DataEntries(DataMap map) {
            return new Array(map.OrderBy(p => p.Key).Select(p =>
                new DataMap(new Dictionary<Data, Data>{
                    {new String("name"), p.Key},
                    {new String("value"), p.Value}
                })
            ));
        }

        public Array DataNames(DataMap map) => new Array(map.Keys.OrderBy(x => x));
        public Array DataValues(DataMap map) => new Array(map.OrderBy(p => p.Key).Select(p => p.Value));

        public Array find(Filter filter, params Data[] values) => new Array(values.Where(new Func<Data, bool>(filter)));
        // TODO: Folded
        public Array interlaced(params Array[] lists) => new Array(
            Enumerable.Range(0, lists.Min(x => x.Count))
            .SelectMany(i => lists.Select(l => l[i]))
        );
        public Array repeated(double count, params Data[] values) =>
            new Array(Enumerable.Range(0, (int)count).SelectMany(_ => values));
        public Array reversed(params Data[] values) => new Array(values.Reverse());

        static int mod(int a, int b)
        {
            var c = a % b;
            return c*b < 0 ? c+b : c;
        }

        public Array rotated(double rotation, params Data[] values) =>
            new Array(Enumerable.Range(-(int)rotation, values.Length).Select(i => values[mod(i, values.Length)]));
        public Array shuffled(params Data[] list)
        {
            int n = list.Length;  
            while (n > 1) {  
                n--;  
                int k = Context.Random.Next(n + 1);  
                var value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }
            return new Array(list);
        }
        public Array sorted(params Data[] list)
        {
            System.Array.Sort(list);
            return new Array(list);
        }
    }
}