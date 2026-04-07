namespace OSKHelpers.Logging
{
    /// <summary>
    /// Interface for objects that must produce a CSV-formatted log.
    /// </summary>
    public interface ICSVLogItem
    {
        /// <summary>
        /// Header row of the CSV file.
        /// </summary>
        string GetCSVHeader();

        /// <summary>
        /// Data row for the CSV file.
        /// </summary>
        string GetCSVData();
    }
}
