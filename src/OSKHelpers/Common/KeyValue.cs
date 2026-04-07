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

		public KeyType Key { get; set; }
		public ValueType Value { get; set; }

		#endregion

		#region Constructors

		public KeyValue() { }

		public KeyValue(KeyType key, ValueType value)
		{
			Key = key;
			Value = value;
		}

		#endregion
	}
}
