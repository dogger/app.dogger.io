using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.IO
{
    [ExcludeFromCodeCoverage]
    public class File : IFile
    {
        public async Task<byte[]> ReadAllBytesAsync(string path)
        {
            return await System.IO.File.ReadAllBytesAsync(path);
        }
    }
}
