using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.DB;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class ObjSpawner : MonoBehaviour, ILayerWithPropertyData
    {
        [Header("Required input")] 
        [SerializeField] private Material baseMaterial;
        [SerializeField] private ObjImporter.ObjImporter importerPrefab;

        [Header("Settings")] 
        [SerializeField] private bool createSubMeshes = false;
        
        [SerializeField] private ObjPropertyData propertyData = new();
        public LayerPropertyData PropertyData => propertyData;

        private ObjImporter.ObjImporter importer;
        private string tempfilepath;

        private void Awake()
        {
            gameObject.transform.position = ObjectPlacementUtility.GetSpawnPoint();
        }

        private void Start()
        {
            StartImport();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var propertyData = properties.OfType<ObjPropertyData>().FirstOrDefault();
            if (propertyData == null) return;

            // Property data is set here, and the parsing and loading of the actual data is done
            // in the start method, there a coroutine is started to load the data in a streaming fashion.
            // If we do that here, then this may conflict with the loading of the project file and it would
            // cause duplication when adding a layer manually instead of through the loading mechanism
            this.propertyData = propertyData;
        }

        private void StartImport()
        {
            DisposeImporter();

            
            importer = Instantiate(importerPrefab);

            var localPath = propertyData.ObjFile.LocalPath.TrimStart('/', '\\');
            var path = Path.Combine(Application.persistentDataPath, localPath);
            
            ImportObj(path);
        }

        private void ImportObj(string path)
        {
            string copiedFilename = path + ".temp";
            File.Copy(path, copiedFilename);

            tempfilepath = copiedFilename;
            // Call the method to store the .temp OBJ file in the in-memory database
            StoreTempObjFileInMemory();  // New method to store in ObjectDB
            importer.objFilePath = copiedFilename;
            Debug.Log($"Copied OBJ to temp file: {copiedFilename}");  // Log the copied temp file path

            importer.mtlFilePath = "";
            importer.imgFilePath = "";

            importer.BaseMaterial = baseMaterial;
            importer.createSubMeshes = createSubMeshes;
            importer.StartImporting(OnObjImported);
        }


        private void OnObjImported(GameObject returnedGameObject)
        {
            // By explicitly stating the worldPositionStays to false, we ensure Obj is spawned and it will retain the
            // position and scale in this parent object
            returnedGameObject.transform.SetParent(this.transform, false);
            returnedGameObject.AddComponent<MeshCollider>();

            

            DisposeImporter();
        }

        // New method to store the .temp OBJ file in ObjectDB
        private void StoreTempObjFileInMemory()
        {
            // Ensure the source path (importer's objFilePath) is not empty
            if (string.IsNullOrEmpty(tempfilepath))
            {
                Debug.LogError("The .temp file path is empty or null. Cannot store the file.");
                return;
            }

            // Read the contents of the .temp file
            byte[] fileBytes = File.ReadAllBytes(tempfilepath);
            Debug.Log($"Read .temp file at path: {tempfilepath}");  // Log the file path

            // Store the file bytes in ObjectDB
            string fileName = Path.GetFileName(tempfilepath).Replace(".temp", "");
            ObjectDB.insert(fileName, fileBytes);

            Debug.Log($"Stored .obj file in memory with key: {fileName}");  // Log the storage operation
        }


        // New method to move the .temp OBJ file
        private void MoveTempObjFile()
        {
            // Ensure the source path (importer's objFilePath) is not empty
            if (string.IsNullOrEmpty(importer.objFilePath))
            {
                Debug.LogError("The .temp file path is empty or null. Cannot move the file.");
                return;
            }

            // Define the paths
            string sourcePath = importer.objFilePath;  // This is the .temp file
            string destinationDirectory = "Assets/ImportedOBJ_permanent";  // The permanent directory where you want to store the OBJ
            string fileName = Path.GetFileNameWithoutExtension(sourcePath).Replace(".temp", ".obj");  // Original .obj file name
            string destinationPath = Path.Combine(destinationDirectory, fileName);

            // Ensure the destination directory exists
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Check if the source file actually exists before moving
            if (File.Exists(sourcePath))
            {
                // Move the .temp file to the permanent location and rename it as .obj
                File.Move(sourcePath, destinationPath);
                Debug.Log($"Moved .obj file to: {destinationPath}");
            }
            else
            {
                Debug.LogError($"The .temp file does not exist at path: {sourcePath}. Cannot move the file.");
            }
        }


        private void DisposeImporter()
        {
            if (importer != null) Destroy(importer.gameObject);
        }
    }
}