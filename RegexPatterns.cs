using System.Text.RegularExpressions;

namespace TESTING_WeddingtreeV1
{
    internal partial class Program
    {
        [GeneratedRegex("^((?:\\p{L}| |\\d|\\.|-)+?)\\s(\\d+(?: ?(?:-|/) ?\\d+){0,3} *\\S?)$", RegexOptions.Multiline)]
        private static partial Regex AddressPattern();
        // ^((?:\p{L}| |\d|\.|-)+?)\s(\d+(?: ?(?:-|/) ?\d+){0,3} *\S?)$

        [GeneratedRegex("(?<!\\d)(\\d{4,5})\\ ([[\\p{L}\\p{Mn}\\p{Pc}ÄÖÜäöüß\\-\\/\\ \\.]+(?:\\([\\w\\/\\ \\.]+\\))?)(?<!\\ )", RegexOptions.Multiline)]
        private static partial Regex PostalCodeCityPattern();
        // (?<!\d)(\d{4,5})\ ([[\p{L}\p{Mn}\p{Pc}ÄÖÜäöüß\-\/\ \.]+(?:\([\w\/\ \.]+\))?)(?<!\ )

        [GeneratedRegex("(\\d{6,10})", RegexOptions.Multiline)]
        private static partial Regex PostNumberPattern();
        // (\d{6,10})

        [GeneratedRegex(".*Packstation(?:\\W|\\s)*(\\d{3})")]
        private static partial Regex LockerIDPattern();
        // .*Packstation(?:\W|\s)*(\d{3})

        [GeneratedRegex("[Ss]torno (\\w+)$")]
        private static partial Regex CancelShipmentPattern();
        // [Ss]torno (\w+)

        [GeneratedRegex("[0-9\\-]{8,35}")]
        private static partial Regex RefNumPattern();
        // [0-9\-]{8,35}

        [GeneratedRegex(".(PO\\d{1,}$)")]
        private static partial Regex PostNumberBehindName();
        // .(PO\d{1,}$)
    }
}
