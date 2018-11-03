using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Core.FileSystem
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FileFieldAttribute : Attribute
    {
        public string Name = "";
        public bool WriteAfterLoad;

        /// <summary>
        /// Will reflect this field in the data directory
        /// </summary>
        /// <param name="fileName">Name of the file on disk</param>
        /// <param name="writeAfterLoaded">Set to true for config / setting files</param>
        public FileFieldAttribute(string fileName, bool writeAfterLoaded = false)
        {
            Name = fileName;
            WriteAfterLoad = writeAfterLoaded;
        }
    }
}
