namespace OSKHelpers.CollectionUtils
{
    /// <summary>
    /// Enum whose negative values represent special navigation actions for building pager UI controls.
    /// </summary>
    public enum PagingControlChars
    {
        /// <summary>Suspension points (ellipsis) indicating omitted page numbers.</summary>
        SuspensionPoints = -1,
        /// <summary>Navigate to the first page.</summary>
        First            = -2,
        /// <summary>Skip back by one group of pages.</summary>
        FastRewind       = -3,
        /// <summary>Navigate to the previous page.</summary>
        Previous         = -4,
        /// <summary>Navigate to the next page.</summary>
        Next             = -5,
        /// <summary>Skip forward by one group of pages.</summary>
        FastForward      = -6,
        /// <summary>Navigate to the last page.</summary>
        Last             = -7
    };
}