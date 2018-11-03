extern alias References;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using References::Newtonsoft.Json;

namespace Oxide.Core.FileSystem
{
    /// <summary>
    /// Stores each class as it's own JSON file in a specific folder
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataObjectCollection<T> : DataObjectCollectionBase where T : INamedDataObject
    {
        private Dictionary<string, T> _dataObjects = new Dictionary<string, T>();
        private FileSystemWatcher _fileSystemWatcher;
        public string FolderPath;

        public Action<T> OnDataObjectReloaded;

        public DataObjectCollection(string folderName, bool saveOnUnload = true)
        {
            FolderPath = Path.Combine(Interface.Oxide.DataFileSystem.Directory, folderName);

            foreach (var fileName in Directory.GetFiles(FolderPath))
            {
                _dataObjects.Add(fileName, default(T));
            }
        }

        public T Get(string name)
        {
            if (!_dataObjects.TryGetValue(name, out T ret))
            {
                if (!TryLoadObject(name))
                {
                    return default(T);
                }
            }
            _fileSystemWatcher = new FileSystemWatcher();
            _fileSystemWatcher.Changed += OnFileChanged;
            _fileSystemWatcher.Created += OnFileChanged;
            //this.watcher.Deleted += this.watcher_Changed;
            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _fileSystemWatcher.IncludeSubdirectories = false;
            _fileSystemWatcher.EnableRaisingEvents = true;
            return _dataObjects[name];
        }

        public T GetOrCreate(string name)
        {
            if (_dataObjects.TryGetValue(name, out T ret))
            {
                return ret;
            }
            if (!TryLoadObject(name))
            {
                ret = Interface.Oxide.DataFileSystem.ReadObject<T>(Path.Combine(FolderPath, name));
                _dataObjects.Add(name, ret);
            }
            return _dataObjects[name];
        }

        private bool TryLoadObject(string name)
        {
            return false;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            _dataObjects[e.Name] = default(T);
        }
    }

    public class DataObjectCollectionBase
    {
        public virtual void SaveAll()
        {

        }
    }
}
