using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

/*
    All credit goes to JohnnyCrazy
    (https://github.com/JohnnyCrazy/scripthookvdotnet/blob/native-generator/helpers/NativeGenerator/NativeFile.cs)
 */
namespace NativeGenerator.Json
{
    public class NativeFile : Dictionary<string, NativeNamespace> { }
    public class NativeNamespace : Dictionary<string, NativeFunction> { }

    public class NativeFunction
    {
        public class Parameter
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("params")]
        public List<Parameter> Params { get; set; }

        [JsonProperty("results")]
        public string Results { get; set; }

        [JsonProperty("jhash")]
        public string JHash { get; set; }
    }
}
