﻿using System;
using Netherlands3D.Indicators.JsonConverters;
using Newtonsoft.Json;

namespace Netherlands3D.Indicators.Dossier.DataLayers
{
    [Serializable]
    public struct Frame
    {
        public string label;

        [JsonConverter(typeof(UriConverter))]
        public Uri data;
        
        [JsonConverter(typeof(UriConverter))]
        public Uri map;
    }
}