using CsvHelper.Configuration.Attributes;

namespace vtb2beancount
{
    /// <summary>
    /// Transaction entry from CSV file
    /// </summary>
    internal class TransactionEntry
    {
        [Name("operation_date")]
        public string? Date { get; set; }
        [Name("amount")]
        public string? TotalValue { get; set; }
        [Name("description")]
        public string? Description { get; set; }
    }
}
