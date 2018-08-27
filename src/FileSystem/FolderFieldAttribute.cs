using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Core.FileSystem
{
    public class FolderFieldAttribute : FileFieldBase
    {
        public FolderFieldAttribute(string fileName)
        {
            Name = fileName;
        }
    }
}
