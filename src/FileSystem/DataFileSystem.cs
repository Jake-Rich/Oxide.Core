extern alias References;

using Oxide.Core.Configuration;
using References::Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oxide.Core.FileSystem;
using System.Reflection;
using Oxide.Core.Plugins;

namespace Oxide.Core
{
    /// <summary>
    /// Manages all data files
    /// </summary>
    public class DataFileSystem
    {
        /// <summary>
        /// Gets the directory that this system works in
        /// </summary>
        public string Directory { get; private set; }

        // All currently loaded datafiles
        private readonly Dictionary<string, DynamicConfigFile> _datafiles;

        //Constructor should be internal so people can't easily create a new datafilesystem over the whole filesystem
        /// <summary>
        /// Initializes a new instance of the DataFileSystem class
        /// </summary>
        /// <param name="directory"></param>
        internal DataFileSystem(string directory)
        {
            Directory = directory;
            _datafiles = new Dictionary<string, DynamicConfigFile>();
            KeyValuesConverter converter = new KeyValuesConverter();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
        }

        public DynamicConfigFile GetFile(string name)
        {
            name = DynamicConfigFile.SanitizeName(name);
            DynamicConfigFile datafile;
            if (_datafiles.TryGetValue(name, out datafile))
            {
                return datafile;
            }

            datafile = new DynamicConfigFile(Path.Combine(Directory, $"{name}.json"));
            _datafiles.Add(name, datafile);
            return datafile;
        }

        /// <summary>
        /// Check if datafile exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ExistsDatafile(string name)
        {
            var path = Path.Combine(Directory, name.Replace(".json", "") + ".json");
            return File.Exists(path);
        }

        /// <summary>
        /// Gets a datafile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DynamicConfigFile GetDatafile(string name)
        {
            DynamicConfigFile datafile = GetFile(name);

            // Does it exist?
            if (datafile.Exists())
            {
                // Load it
                datafile.Load();
            }
            else
            {
                // Just make a new one
                datafile.Save();
            }

            return datafile;
        }

        /// <summary>
        /// Saves the specified datafile
        /// </summary>
        /// <param name="name"></param>
        public void SaveDatafile(string name) => GetFile(name).Save();

        /// <summary>
        /// Read data files in a batch and send callback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        public void ForEachObject<T>(string name, Action<T> callback)
        {
            string folder = DynamicConfigFile.SanitizeName(name);
            IEnumerable<DynamicConfigFile> files = _datafiles.Where(d => d.Key.StartsWith(folder)).Select(a => a.Value);
            foreach (DynamicConfigFile file in files)
            {
                callback?.Invoke(file.ReadObject<T>());
            }
        }

        private Dictionary<Type, List<DataObjectField>> _cachedDataObjectFields = new Dictionary<Type, List<DataObjectField>>();
        private Dictionary<Plugin, List<Type>> _pluginToDataObjectTypes = new Dictionary<Plugin, List<Type>>();

        private struct DataObjectField
        {
            public FieldInfo FieldInfo;
            public Type type;
            public bool File;
            public bool DataCollection;
            public string Name;
        }

        #region Read / Write JSON

        public T ReadObject<T>(string name)
        {
            if (!ExistsDatafile(name))
            {
                T instance = Activator.CreateInstance<T>();
                WriteObject(name, instance);
                return instance;
            }

            var filename = Path.Combine(Directory, name + ".json");

            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(filename));
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogInfo($"Failed to deserialize object {name} from JSON:\n{ex}");
                return default(T);
            }
        }

        public object ReadObject(string name, Type type)
        {
            if (!ExistsDatafile(name))
            {
                object instance = Activator.CreateInstance(type);
                WriteObject(name, instance);
                return instance;
            }

            var filename = Path.Combine(Directory, name + ".json");

            try
            {
                return JsonConvert.DeserializeObject(File.ReadAllText(filename), type);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogInfo($"Failed to deserialize object {name} from JSON:\n{ex}");
                return null;
            }
        }

        //People probally used the generic even though it isnt needed here. Best to leave it to not randomly break plugins
        public void WriteObject<T>(string name, T Object, JsonSerializerSettings settings = null)
        {
            WriteObject(name, (object)Object, settings);
        }

        //Another wrapper to prevent old plugins from breaking
        //Sync does nothing and never did
        public void WriteObject<T>(string name, T Object, bool sync)
        {
            WriteObject(name, Object);
        }

        /// <summary>
        /// Saves a class as a .json file in the data directory
        /// </summary>
        /// <param name="name"></param>
        /// <param name="Object"></param>
        /// <param name="settings"></param>
        public void WriteObject(string name, object Object, JsonSerializerSettings settings = null)
        {
            var filename = Path.Combine(Directory, name + ".json");
            string directoryName = Utility.GetDirectoryName(filename);
            if (directoryName != null && !System.IO.Directory.Exists(directoryName))
            {
                System.IO.Directory.CreateDirectory(directoryName);
            }
            string text = null;
            try
            {
                text = JsonConvert.SerializeObject(Object, Formatting.Indented, settings);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogInfo($"Failed to serialize object {name} to JSON:\n{ex}");
                return;
            }
            File.WriteAllText(filename, text);
        }

        #endregion

        #region Allowing normal file access inside data directory

        /// <summary>
        /// Prevents people using .. to go outside of the data directory
        /// </summary>
        /// <param name="subPath"></param>
        /// <returns></returns>
        private string SafeCombine(string subPath, string extension = null)
        {
            subPath = subPath.Replace("\\..", "");
            if (extension != null)
            {
                subPath = subPath.Replace(extension, "") + extension;
            }
            return Path.Combine(Directory, subPath);
        }

        /// <summary>
        /// Returns all text from a file. Returns an empty string if one isnt found
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string ReadAllText(string name)
        {
            string path = SafeCombine(name, ".txt");
            if (!File.Exists(path))
            {
                return "";
            }
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Reads all lines of a text file
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string[] ReadAllLines(string name)
        {
            string path = SafeCombine(name, ".txt");
            if (!File.Exists(path))
            {
                return new string[0];
            }
            return File.ReadAllLines(path);
        }

        /// <summary>
        /// Writes all bytes into the data directory. Only certain extensions are allowed for files.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="fileName"></param>
        public void WriteBytes(byte [] bytes, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (fileName != "" && !WhitelistedFileExtensions.Contains(extension))
            {
                Interface.Oxide.LogInfo($"Extension \"{extension}\"for file {fileName} is an illegal file extension!");
                return;
            }
            var path = SafeCombine(fileName);
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Writes all text to a .txt file
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="fileName"></param>
        public void WriteTextFile(string contents, string fileName)
        {
            var path = SafeCombine(Path.GetFileNameWithoutExtension(fileName), ".txt");
            File.WriteAllText(path, contents);
        }

        /// <summary>
        /// Gets data files from path, with optional search pattern
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public string[] GetFiles(string path = "", string searchPattern = "*")
        {
            return System.IO.Directory.GetFiles(SafeCombine(path), searchPattern);
        }

        /// <summary>
        /// Get names of files without extension and path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public string[] GetFilesWithoutExtension(string path = "", string searchPattern = "*")
        {
            var files = GetFiles(path, searchPattern);
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }

        /// <summary>
        /// Gets folders in the data directory plus path, with optional search pattern
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string[] GetDirectories(string path = "", string searchPattern = "*")
        {
            return System.IO.Directory.GetDirectories(SafeCombine(path), searchPattern);
        }

        /// <summary>
        /// Creates a folder in the data directory
        /// </summary>
        /// <param name="path"></param>
        public void CreateDirectory(string path)
        {
            System.IO.Directory.CreateDirectory(SafeCombine(path));
        }

        /// <summary>
        /// Checks if a folder exists in the data directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool DirectoryExists(string path)
        {
            return System.IO.Directory.Exists(SafeCombine(path));
        }

        #endregion

        #region DataObject

        private List<DataObjectField> GetDataObjectFields(Type type)
        {
            if (_cachedDataObjectFields.TryGetValue(type, out var list))
            {
                return list;
            }

            _cachedDataObjectFields.Add(type, list);

            #region Store Plugin of each type so we can cleanup easily later on
            var plugin = GetPluginFromType(type);
            List<Type> pluginTypes;
            if (!_pluginToDataObjectTypes.TryGetValue(plugin, out pluginTypes))
            {
                pluginTypes = new List<Type>();
                _pluginToDataObjectTypes.Add(plugin, pluginTypes);
            }
            pluginTypes.Add(type);
            #endregion

            CheckDataObjectFolder(plugin);

            list = new List<DataObjectField>();
            foreach(var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object[] attributes = field.GetCustomAttributes(typeof(FileFieldAttribute), true);
                if (attributes.Length < 1)
                {
                    continue;
                }
                list.Add(new DataObjectField()
                {
                    FieldInfo = field,
                    File = attributes[0] is FileFieldAttribute,
                    DataCollection = attributes[0] is DataObjectCollectionBase,
                    type = field.FieldType,
                    Name = ((FileFieldAttribute)attributes[0]).Name,
                });
            }
            return list;
        }

        private Plugin GetPluginFromType(Type type)
        {
            Interface.Oxide.LogInfo($"Get Plugin From Type");
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                Interface.Oxide.LogInfo($"CurrentType: {currentType.ToString()}");
                if (currentType.IsSubclassOf(typeof(Plugin)))
                {
                    Interface.Oxide.LogInfo($"Looking for plugin of that type");
                    return Oxide.Core.Interface.Oxide.RootPluginManager.GetPlugins().FirstOrDefault(x => x.GetType() == currentType);
                }
                //Gets the outer class, or the subclass if its derived
                currentType = type.IsNested ? type.DeclaringType : type.BaseType;
            }
            return null;
        }

        public T ReadDataObject<T>(string path, string name) where T : INamedDataObject
        {
            Interface.Oxide.LogInfo("GetDataObject");
            var fields = GetDataObjectFields(typeof(T));
            Interface.Oxide.LogInfo("GetDataObject2");
            if (fields.Count == 0)
            {
                return ReadObject<T>(Path.Combine(path, name));
            }
            var data = Activator.CreateInstance<T>();
            foreach (var field in fields)
            {
                field.FieldInfo.SetValue(data, ReadDataObject(field.type, Path.Combine(path, field.Name ?? ""), field.Name ?? ""));
            }
            return data;
        }

        //We were infinite looping fyi
        private object ReadDataObject(Type type, string path, string name)
        {
            var fields = GetDataObjectFields(type);
            if (fields.Count == 0)
            {
                return ReadObject(Path.Combine(path, name), type);
            }
            var data = Activator.CreateInstance(type);
            foreach (var field in fields)
            {
                field.FieldInfo.SetValue(data, ReadDataObject(field.type, Path.Combine(path, field.Name ?? ""), field.Name ?? ""));
            }
            return data;
        }

        public void WriteDataObject<T>(T dataObject, string path) where T : INamedDataObject
        {
            var fields = GetDataObjectFields(dataObject.GetType());
            if (fields.Count == 0)
            {
                WriteObject(SafeCombine(path, ".json"), dataObject);
                return;
            }
            foreach(var field in fields)
            {
                var subDataObject = field.FieldInfo.GetValue(dataObject);
                if (subDataObject is DataObjectCollectionBase)
                {
                    //Save each element of the collection as it's own file
                    ((DataObjectCollectionBase)subDataObject).SaveAll();
                }
                else if (dataObject != null)
                {
                    //Save the field as it's own file
                    WriteDataObject(subDataObject, Path.Combine(path, dataObject.Name));
                }
            }
        }

        private void WriteDataObject(object dataObject, string path)
        {
            var fields = GetDataObjectFields(dataObject.GetType());
            if (fields.Count == 0)
            {
                WriteObject(SafeCombine(path, ".json"), dataObject);
                return;
            }
            foreach (var field in fields)
            {
                var subDataObject = field.FieldInfo.GetValue(dataObject);
                if (subDataObject is DataObjectCollectionBase)
                {
                    //Save each element of the collection as it's own file
                    ((DataObjectCollectionBase)subDataObject).SaveAll();
                }
                else
                {
                    //Save the field as it's own file
                    if (dataObject is INamedDataObject)
                    {
                        WriteDataObject(subDataObject, Path.Combine(path, ((INamedDataObject)dataObject).Name));
                    }
                }
            }
        }

        private void LoadDataObjects(Plugin plugin)
        {
            //Get all fields with attribute FileField
            //Try load them all from the folder
            var fields = GetDataObjectFields(plugin.GetType());
            foreach(var field in fields)
            {
                field.FieldInfo.SetValue(plugin, ReadDataObject(field.type, "", field.Name ?? ""));
            }
        }

        private void CheckDataObjectFolder(Plugin plugin)
        {
            var dataObjectDirectory = Path.Combine(Directory, plugin.Title.ToLower());
            if (!System.IO.Directory.Exists(dataObjectDirectory))
            {
                System.IO.Directory.CreateDirectory(dataObjectDirectory);
            }
        }

        #endregion

        internal void OnPluginLoaded(Plugin plugin)
        {
            //We dont do anything yet
        }

        internal void OnPluginUnloaded(Plugin plugin)
        {
            return;
            List<Type> types;
            if (_pluginToDataObjectTypes.TryGetValue(plugin, out types))
            {
                foreach(var type in types)
                {
                    _cachedDataObjectFields.Remove(type);
                }
            }
        }

        private readonly HashSet<string> WhitelistedFileExtensions = new HashSet<string>()
        {
            //Text data storage
            "json",
            "xml",
            "cfg",
            "config",
            //Text
            "txt",
            "csv",
            //Image
            "png",
            "jpeg",
            "jpg",
            "gif",
            "bmp",
            //Compressed
            "zip",
            //General data
            "dat",
            "sav",
            //Unity assets
            "asset",
        };
    }
}
