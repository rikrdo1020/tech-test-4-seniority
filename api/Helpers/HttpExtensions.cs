using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace api.Helpers
{
    public static class HttpExtensions
    {
        public static T Get<T>(
        this IQueryCollection collection,
        string key)
        {
            var value = default(T);

            if (collection.TryGetValue(key, out var result))
            {
                try
                {
                    string valueToConvert = result.ToString();
                    value = (T)Convert.ChangeType(valueToConvert, typeof(T), CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    // conversion failed
                    // skip value
                }
            }
            return value;
        }
    }
}
