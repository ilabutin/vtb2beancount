using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using vtb2beancount;
using Tomlyn;
using Tomlyn.Model;

string configFile = args[0];
string outputFile = args[1];

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var config = Toml.ToModel(File.ReadAllText(configFile, Encoding.UTF8));
TomlTable cardsTable = (TomlTable)config["cards"];
TomlTable categoriesTable = (TomlTable)config["categories"];
TomlTable dropStrings = (TomlTable)config["vtb_drop"];
TomlTable options = (TomlTable)config["options"];

Dictionary<string, string> categoriesSubstrings = new Dictionary<string, string>();
foreach (var categoryPair in categoriesTable)
{
    if (categoryPair.Key.StartsWith("$$"))
    {
        categoriesSubstrings.Add(categoryPair.Key.Substring(2), (string)categoryPair.Value);
    }
}

NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
numberFormatInfo.NumberDecimalSeparator = ".";
var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ";",
};
List<Transaction> transactions = new List<Transaction>();
for (int argN = 2; argN < args.Length; argN++)
{
    using (StreamReader reader = new StreamReader(args[argN], System.Text.Encoding.GetEncoding("utf-8")))
    {
        using (CsvReader csvReader = new CsvReader(reader, csvConfig))
        {
            var entries = csvReader.GetRecords<TransactionEntry>().ToList();
            Console.WriteLine($"{entries.Count} entries read");
            transactions.AddRange(
                entries.Select(ParseCsvEntry).Where(t => t.StatusOk).OrderBy(t => t.Date));
        }
    }
}

using StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8);
for (int i = 0; i < transactions.Count; i++)
{
    Transaction t = transactions[i];
    Transaction? nextT = i == transactions.Count - 1 ? null : transactions[i + 1];
    
    if (dropStrings.Values.Any(v => t.Description?.Contains((string)v) ?? false))
    {
        Console.WriteLine($"dropped: {t}");
        continue;
    }

    // Если две подряд транзакции с одинаковой суммой, но разными знаками, то убираем одну, т.к. это перевод
    if (t.Description == "Перевод между счетами" && nextT != null)
    {
        if (t.TotalValue == -nextT.TotalValue
            && t.Description == nextT.Description
            && t.Category == "Переводы")
        {
            Console.WriteLine($"dropped transfer: {t}");
            continue;
        }
    }

    // Write header line
    string headerDescription = t.Description ?? "Прочее";
    // Write MCC if exists, otherwise 0
    string mcc = t.Mcc ?? "0";
    // Write main expense account
    string? cardNumber = "3206";
    
    if (!cardsTable.TryGetValue(cardNumber, out object account))
    {
        account = "XX";
    }

    // Write category
    object category = "YY";
    if (t is { TotalValue: < 0, Description: not null } 
             && categoriesTable.TryGetValue(t.Description, out var foundCategory))
    {
        category = foundCategory;
    }
    else if (t is { TotalValue: < 0, Description: not null })
    {
        foreach (string categorySubstring in categoriesSubstrings.Keys)
        {
            if (t.Description.Contains(categorySubstring))
            {
                category = categoriesSubstrings[categorySubstring];
            }
        }         
    }
    else if (t.TotalValue > 0)
    {
        category = "Income:";
    }
    else
    {
        category = "Expenses:";
    }
    
    writer.WriteLine($"{t.Date.ToString("yyyy-MM-dd")} * \"{headerDescription}\"");
    writer.WriteLine($"  mcc: {mcc}");
    writer.WriteLine($"  {account}     {t.TotalValue.ToString("F2", CultureInfo.InvariantCulture)} RUB");
    writer.WriteLine($"  {category}");
    writer.WriteLine();
}

Transaction ParseCsvEntry(TransactionEntry transactionEntry)
{
    DateOnly date = DateOnly.FromDateTime(DateTime.Now);
    if (transactionEntry.Date is { } d)
    {
        date = DateOnly.ParseExact(d, "dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    decimal totalValue = 0.0M;
    if (transactionEntry.TotalValue is { } v)
    {
        totalValue = Convert.ToDecimal(v.Split(" ")[0], numberFormatInfo);
    }

    string category = "";

    return new Transaction(date, "", totalValue, category,
        null, transactionEntry.Description, true);
}