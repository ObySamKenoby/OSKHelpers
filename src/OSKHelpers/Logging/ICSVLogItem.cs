namespace OSKHelpers.Logging
{
    /// <summary>
    /// Interfaccia per gli oggetti che devono produrre un log in formato CSV.
    /// </summary>
    public interface ICSVLogItem
    {
        /// <summary>
        /// Header del file CSV.
        /// </summary>
        string GetCSVHeader();

        /// <summary>
        /// Contenuto della riga per il file CSV.
        /// </summary>
        string GetCSVData();
    }
}
