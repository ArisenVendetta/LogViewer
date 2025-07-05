namespace LogViewer
{
    /// <summary>
    /// Represents the result of an attempt to export a log file.
    /// </summary>
    /// <remarks>This class provides details about the outcome of a log export operation, including whether
    /// the operation  was successful, any error messages or exceptions encountered, and information about the exported
    /// file.</remarks>
    public sealed class ExportLogResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// Gets or sets the error message associated with the current operation or state.
        /// </summary>
        public string? ErrorMessage { get; set; }
        /// <summary>
        /// Gets or sets the file path associated with the current operation.
        /// </summary>
        public string? FilePath { get; set; }
        /// <summary>
        /// Gets or sets the exception associated with the current operation.
        /// </summary>
        public Exception? Exception { get; set; }
        /// <summary>
        /// Gets or sets the type of the file being processed.
        /// </summary>
        public FileType? FileType { get; set; }
    }
}