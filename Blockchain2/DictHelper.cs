namespace Blockchain2 {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class DictHelper {

        public static IEnumerable<TValue> RandomValues<TKey, TValue>(IDictionary<TKey, TValue> dict) {
            Random rand = new Random();
            List<TValue> values = Enumerable.ToList(dict.Values);
            int size = dict.Count;
            while (true) {
                yield return values[rand.Next(size)];
            }
        }
    }
}