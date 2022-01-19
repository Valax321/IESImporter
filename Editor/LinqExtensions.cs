using System.Collections.Generic;

namespace Valax321.IESImporter
{
    internal static class LinqExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> list, T item, int defaultValue = -1)
        {
            var i = 0;
            foreach (var e in list)
            {
                if (e.Equals(item))
                {
                    return i;
                }

                i++;
            }

            return defaultValue;
        }
    }
}