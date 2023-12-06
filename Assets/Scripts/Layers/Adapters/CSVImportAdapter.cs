using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class IDColor
    {
        [Index(0)] public string Id { get; set; }
        [Index(1)] public string HexColor { get; set; }

        public Color Color
        {
            get
            {
                var hex = HexColor;
                if (!hex.StartsWith("#"))
                    hex = "#" + hex;

                var canParse = ColorUtility.TryParseHtmlString(hex, out var color);
                return canParse ? color : Interaction.NO_OVERRIDE_COLOR; 
            }
        }
    }

    
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/CSVImportAdapter", fileName = "CSVImportAdapter", order = 0)]
    public class CSVImportAdapter : ScriptableObject
    {
        private static Transform datasetLayerParent;

        static Transform DatasetLayerParent
        {
            get
            {
                if (!datasetLayerParent)
                {
                    datasetLayerParent = new GameObject("DatasetLayers").transform;
                }

                return datasetLayerParent;
            }
        }

        public int maxParsesPerFrame = 100;

        public void CreateCSVDatasetLayer(string file)
        {
            // this.path = path;
            foreach (Transform existingDatasetLayers in DatasetLayerParent) //todo: temp fix to allow only 1 dataset layer
            {
                Destroy(existingDatasetLayers.gameObject);
            }
            
            var datasetLayer = new GameObject(file).AddComponent<DatasetLayer>();
            datasetLayer.transform.SetParent(DatasetLayerParent);
            // FindObjectOfType<LayerManager>().RefreshLayerList(); //todo remove findObjectOfType
            datasetLayer.UI.Select();

            var fullPath = Path.Combine(Application.persistentDataPath, file);
            datasetLayer.StartCoroutine(StreamReadCSV(fullPath, datasetLayer, maxParsesPerFrame));
        }

        private IEnumerator StreamReadCSV(string path, DatasetLayer layer, int maxParsesPerFrame)
        {
            var dictionaries = ReadCSVColors(path, maxParsesPerFrame).GetEnumerator();

            while (dictionaries.MoveNext())
            {
                // print("frame: " + Time.frameCount);
                var dictionary = dictionaries.Current;
                // print(dictionary.Count);
                // Debug.Log("pindex: " + layer.PriorityIndex);
                // var cl = GeometryColorizer.AddAndMergeCustomColorSet(layer.PriorityIndex, dictionary);
                var cl = GeometryColorizer.AddAndMergeCustomColorSet(GeometryColorizer.GetLowestPriorityIndex(), dictionary);
                // Debug.Log(cl.PriorityIndex);
                layer.ColorSetLayer = cl;

                yield return null;
            }

            // GeometryColorizer.ReorderColorSet(0, 0, IndexCollisionAction.Swap);
        }

        private IEnumerable<Dictionary<string, Color>> ReadCSVColors(string path, int maxParsesPerFrame)
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = false,
                Delimiter = ";"
            };

            using var reader = new StreamReader(path);
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<IDColor>().GetEnumerator();
                var dictionary = new Dictionary<string, Color>();

                while (records.MoveNext())
                {
                    var record = records.Current;
                    dictionary[record.Id] = record.Color;

                    if (dictionary.Count >= maxParsesPerFrame)
                    {
                        yield return dictionary;
                        dictionary.Clear();
                    }
                }

                //return the remaining elements of the part not divisible by maxParsesPerFrame 
                if (dictionary.Count > 0)
                {
                    yield return dictionary;
                }
                // return records.ToDictionary(record => record.Id, record => record.Color); //don't return like this, because it will stop the parsing from being spread over multiple frames
            }
        }
    }
}