using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IFileSystemInfo
    {
        //
        // Summary:
        //     Gets the name of the file.
        //
        // Returns:
        //     The name of the file.
        string Name { get; }

        string FullName { get; }

        //
        // Summary:
        //     Gets a value indicating whether a file exists.
        //
        // Returns:
        //     true if the file exists; false if the file does not exist or if the file is a
        //     directory.
        bool Exists { get; }

    }
}
