using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    public interface IDirectoryInfo : IFileSystemInfo
    {
        //
        // Summary:
        //     Gets a string representing the directory's full path.
        //
        // Returns:
        //     A string representing the directory's full path.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     null was passed in for the directory name.
        //
        //   T:System.IO.PathTooLongException:
        //     The fully qualified path is 260 or more characters.
        //
        //   T:System.Security.SecurityException:
        //     The caller does not have the required permission.
        string DirectoryName { get; }
        
        //
        // Summary:
        //     Gets or sets a value that determines if the current file is read only.
        //
        // Returns:
        //     true if the current file is read only; otherwise, false.
        //
        // Exceptions:
        //   T:System.IO.FileNotFoundException:
        //     The file described by the current System.IO.FileInfo object could not be found.
        //
        //   T:System.IO.IOException:
        //     An I/O error occurred while opening the file.
        //
        //   T:System.UnauthorizedAccessException:
        //     This operation is not supported on the current platform.-or- The caller does
        //     not have the required permission.
        //
        //   T:System.ArgumentException:
        //     The user does not have write permission, but attempted to set this property to
        //     false.
        bool IsReadOnly { get; set; }
        //
        // Summary:
        //     Gets the size, in bytes, of the current file.
        //
        // Returns:
        //     The size of the current file in bytes.
        //
        // Exceptions:
        //   T:System.IO.IOException:
        //     System.IO.FileSystemInfo.Refresh cannot update the state of the file or directory.
        //
        //   T:System.IO.FileNotFoundException:
        //     The file does not exist.-or- The Length property is called for a directory.
        long Length { get; }
        
    }
}
