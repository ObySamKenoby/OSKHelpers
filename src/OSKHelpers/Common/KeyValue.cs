namespace OSKHelpers.Common
{
	/// <summary>
	/// Classe di utilità per unità chiave / valore fortemente tipizzate.<br/>
	/// L'utilizzo ideale non è quello di sfruttarle come dizionari (c'è la classe apposita) ma per liste che vadano a popolare
	/// combobox o quant'altro
	/// </summary>
	/// <typeparam name="KeyType">Tipo della chiave</typeparam>
	/// <typeparam name="ValueType">Tipo del valore</typeparam>
	public class KeyValue<KeyType, ValueType>
	{
		#region Proprietà

		public KeyType Key { get; set; }
		public ValueType Value { get; set; }

		#endregion

		#region Costruttori

		public KeyValue() { }

		public KeyValue(KeyType key, ValueType value)
		{
			Key = key;
			Value = value;
		}

		#endregion
	}
}
