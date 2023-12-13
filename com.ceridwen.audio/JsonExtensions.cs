using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace com.ceridwen.audio
{
    public static class JsonExtensions
    {
        #region Public Methods

        public static void Populate<T>(this JToken value, T target) where T : class
        {
            using (var sr = value.CreateReader())
            {
                JsonSerializer.CreateDefault().Populate(sr, target); // Uses the system default JsonSerializerSettings
            }
        }

        #endregion
    }
}
