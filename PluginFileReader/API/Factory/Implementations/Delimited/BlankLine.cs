namespace PluginFileReader.API.Factory.Implementations.Delimited
{
    /// <summary>
    /// Determines how an entire blank or empty line should be handled.
    /// </summary>
    public enum BlankLine
    {
        /// <summary>
        /// Return a line with a single empty column.
        /// </summary>
        EmptySingleColumn,

        /// <summary>
        /// Blank and empty lines are skipped.
        /// </summary>
        SkipEntireLine,

        /// <summary>
        /// Consider end of file
        /// </summary>
        EndOfFile
    }
}