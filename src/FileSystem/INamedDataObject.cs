using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Core.FileSystem
{
    public interface INamedDataObject
    {
        string FileName { get; set; }
    }
}
