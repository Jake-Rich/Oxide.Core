using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Core.FileSystem
{
    public class FileFieldAttribute : FileFieldBase
    {
        public FileFieldAttribute(string fileName)
        {
            Name = fileName;
        }
    }
}
