using System;
using System.Text.Json;

namespace BDCommon
{
    public static class Cloner
    {
        /// <summary>
        /// Perform a deep copy of the object via serialization.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>A deep copy of the object.</returns>
        public static T Clone<T>(T source)
        {
            if (!typeof(T).IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", nameof(source));
            }

            var serializedData = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(serializedData);
        }
    }
}
