using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Core.Configuration
{
    public class InstanceConfigFile<T> : ConfigFile where T : class
    {
        public T Instance { get; private set; }

        public InstanceConfigFile(string filename) : base(filename)
        {

        }

        public new void Save(string fileName)
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject(fileName ?? Filename, Instance);
        }

        public new void Load(string fileName)
        {
            try
            {
                Instance = Interface.Oxide.DataFileSystem.ReadObject<T>(fileName ?? Filename);
            }
            catch (Exception ex)
            {
                Interface.Oxide.ServerConsole.AddMessage($"Failed to parse JSON: {ex}");
            }
        }
    }
}
