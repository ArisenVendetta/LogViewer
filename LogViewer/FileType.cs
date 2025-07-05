using PropertyChanged;

namespace LogViewer
{
    /// <summary>
    /// Represents a file type with a name and an associated file extension.
    /// </summary>
    /// <remarks>This class is immutable and provides properties to access the name and extension of the file
    /// type.</remarks>
    /// <param name="name"></param>
    /// <param name="extension"></param>
    [AddINotifyPropertyChangedInterface]
    public sealed class FileType(string name, string extension)
    {
        /// <summary>
        /// Gets the name associated with the current instance.
        /// </summary>
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        /// <summary>
        /// Gets the file extension associated with the current object.
        /// </summary>
        public string Extension { get; } = extension ?? throw new ArgumentNullException(nameof(extension));
    }
}
