namespace OSKHelpers.Common
{
	/// <summary>
	/// Strongly typed key/value pair utility class.<br/>
	/// Its intended use is not as a dictionary replacement but for lists that populate
	/// combo-boxes or similar UI elements.
	/// </summary>
	/// <typeparam name="KeyType">Type of the key.</typeparam>
	/// <typeparam name="ValueType">Type of the value.</typeparam>
	public class KeyValue<KeyType, ValueType>
	{
		#region Properties

		/// <summary>
		/// Gets or sets the key.
		/// </summary>
		public KeyType Key { get; set; }
		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		public ValueType Value { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new empty instance of <see cref="KeyValue{KeyType, ValueType}"/>.
		/// </summary>
		public KeyValue() { }

		/// <summary>
		/// Initializes a new instance of <see cref="KeyValue{KeyType, ValueType}"/> with the specified key and value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public KeyValue(KeyType key, ValueType value)
		{
			Key = key;
			Value = value;
		}

		#endregion
	}
}
