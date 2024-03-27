using Newtonsoft.Json;
using System.Text;

namespace TESTING_WeddingtreeV1;

internal partial class Program
{
    private static readonly Dictionary<string, string?> address = new()
    {
        {"Name", null},
        {"Name2", null},
        {"Name3", null},
        {"Street", null},
        {"Number", null},
        {"Postal Code", null},
        {"City", null},
        {"Country", null},
        {"RefNum", null},
        {"Weight", null}
    };

    public static string Address
    {
        get
        {
            var builder = new StringBuilder();
            foreach (var item in address)
            {
                builder.AppendLine(item.Key + ": " + item.Value);
            }
            return builder.ToString();
        }
    }

    private static readonly Dictionary<string, string> europeanCountryDictionary = new()
    {
        {"Albanien", "ALB"},
        {"Andorra", "AND"},
        {"Belgien", "BEL"},
        {"Bosnien und Herzegowina", "BIH"},
        {"Bulgarien", "BGR"},
        {"Dänemark", "DNK"},
        {"Deutschland", "DEU"},
        {"Estland", "EST"},
        {"Finnland", "FIN"},
        {"Frankreich", "FRA"},
        {"Griechenland", "GRC"},
        {"Irland", "IRL"},
        {"Island", "ISL"},
        {"Italien", "ITA"},
        {"Kosovo", "XKX"},
        {"Kroatien", "HRV"},
        {"Lettland", "LVA"},
        {"Liechtenstein", "LIE"},
        {"Litauen", "LTU"},
        {"Luxemburg", "LUX"},
        {"Malta", "MLT"},
        {"Mazedonien", "MKD"},
        {"Moldawien", "MDA"},
        {"Monaco", "MCO"},
        {"Montenegro", "MNE"},
        {"Niederlande", "NLD"},
        {"Norwegen", "NOR"},
        {"Österreich", "AUT"},
        {"Polen", "POL"},
        {"Portugal", "PRT"},
        {"Rumänien", "ROU"},
        {"Russland", "RUS"},
        {"San Marino", "SMR"},
        {"Schweden", "SWE"},
        {"Schweiz", "CHE"},
        {"Serbien", "SRB"},
        {"Slowakei", "SVK"},
        {"Slowenien", "SVN"},
        {"Spanien", "ESP"},
        {"Tschechien", "CZE"},
        {"Türkei", "TUR"},
        {"Ukraine", "UKR"},
        {"Ungarn", "HUN"},
        {"Vatikanstadt", "VAT"},
        {"Vereinigtes Königreich", "GBR"},
    };

    private static readonly char[] numbers = new[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    private static IConsignee? consignee = null;

    private static (double postalCharge, CustomsItem[] items) customs;

    private static string credentialsPath = "";

    private static bool refNumMandatory = false;
    public static bool DoParcel { get; private set; } = false;
    private static bool doCheckInput = true;

    private static async Task Main(string[] args)
    {
        refNumMandatory = args.Contains("--amazon");
        DoParcel = args.Contains("--parcel");
        doCheckInput = !args.Contains("--noValidate");
        credentialsPath = Path.Combine(Directory.GetCurrentDirectory(), "DHLWarenpostgenerator_Credentials.json");
        SetCredentials();

        while (true)
        {
            bool redo = false;
            Console.Clear();

            var addressList = InputAddress().Result;

            int i = 0;
            while (doCheckInput)
            {
                Console.Clear();

                Console.WriteLine("Ist die Adresse korrekt?");
                Console.WriteLine("Drücke Enter, um zu bestätigen. Mit den Pfeiltasten in den Zeilen navigieren. Mit Esc abbrechen.\n");

                for (int j = 0; j < addressList.Count; j++)
                {
                    Console.WriteLine(addressList[j]);
                }

                Console.SetCursorPosition(0, i + 3);
                (string line, ConsoleKey? key)? output = ReadLine(addressList[i], addressList.Count);

                if (output is null)
                {
                    ResetDictValues();
                    redo = true;
                    break;
                }
                addressList[i] = output.Value.line;
                if (string.IsNullOrWhiteSpace(addressList[i])) addressList.RemoveAt(i);

                if (output.Value.key is not null)
                {
                    if (output.Value.key == ConsoleKey.UpArrow && i > 0) i--;
                    else if (output.Value.key == ConsoleKey.DownArrow && i < addressList.Count - 1) i++;
                    continue;
                }
                else break;
            }

            if (redo) continue;

            if (!ProcessAddress(addressList))
            {
                Console.ReadKey(true);
                ResetDictValues();
                continue;
            }

            Console.Clear();
            Console.WriteLine("Erstelle Etikett...");

            await ApiService.CreateShipmentLabel(
                consignee!,
                address["Weight"] is null ? -1 : int.Parse(address["Weight"]!),
                address["RefNum"],
                customs.postalCharge,
                customs.items
                );

            ResetDictValues();
        }
    }

    private static void SetCredentials()
    {
        if (!File.Exists(credentialsPath))
        {
            GenerateCredentialsFile();
            Logger.Log("FEHLER: Anmeldedaten fehlen. Öffne Credentials.json und fülle die Felder aus.", $"Datei befindet sich im Pfad {credentialsPath}");
            Console.ReadKey();
            Environment.Exit(-1);
        }

        using var reader = new StreamReader(credentialsPath);

        string json = reader.ReadToEnd();
        var credentials = JsonConvert.DeserializeObject<Credentials>(json);

        ApiService.SetCredentials(credentials.Username, credentials.Password, credentials.ApiSchluessel);
        LabelContent.SetCredentials(credentials);
        Logger.SetLoggerPath(credentials.LogPath);
    }

    private static void GenerateCredentialsFile()
    {
        string json = JsonConvert.SerializeObject(new Credentials(), Formatting.Indented);
        File.Create(credentialsPath).Close();
        using var stream = new StreamWriter(credentialsPath);
        stream.Write(json);
        stream.Close();
    }

    private static void ResetDictValues()
    {
        for (int i = 0; i < address.Count; i++)
        {
            address[address.ElementAt(i).Key] = null;
        }
    }

    private static async Task<List<string>> InputAddress()
    {
        string? input;
        var addressList = new List<string>();

        while (true)
        {
            Console.Clear();
            Console.WriteLine("Gebe die Adresse an:\n");
            Console.Write(">> ");
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            {
                Console.Write(">> ");
                addressList.Add(input);
            }

            if (CancelShipmentPattern().IsMatch(addressList[0]))
            {
                var match = CancelShipmentPattern().Match(addressList[0]);
                await ApiService.CancelShipment(match.Groups[1].Value);
                addressList.Clear();
                continue;
            }

            if (addressList.Count <= 2)
            {
                Console.Clear();
                addressList.Clear();
                Console.WriteLine("Adresse kürzer als erwartet.");
                Console.ReadKey(true);
                Console.Clear();
                continue;
            }

            // If first char in last line is a number, it must be the reference number
            if (RefNumPattern().IsMatch(addressList[^1]))
            {
                address["RefNum"] = RefNumPattern().Match(addressList[^1]).Value;
                addressList.RemoveAt(addressList.Count - 1);
            }

            if (address["RefNum"] is null)
            {
                Console.WriteLine($"\n{(refNumMandatory ? "" : "(Optional) ")}Gebe die Referenznummer an:");
                while (true)
                {
                    Console.Write(">> ");
                    string? refNum = Console.ReadLine()?.Trim();
                    if (refNumMandatory)
                    {
                        if (refNum is not null && RefNumPattern().IsMatch(refNum))
                        {
                            address["RefNum"] = RefNumPattern().Match(refNum).Value;
                            break;
                        }
                        else Console.WriteLine("Muss zwischen 8 und 50 Zeichen lang sein und ausschließlich aus Ziffern und Bindestrichen bestehen.\nBeispiel: 123-4567890-1234567");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(refNum) || refNum.Length >= 8)
                        {
                            address["RefNum"] = string.IsNullOrWhiteSpace(refNum) ? null : refNum;
                            break;
                        }
                        else Console.WriteLine("Muss zwischen 8 und 50 Zeichen lang sein.");
                    }
                }
            }

            Console.WriteLine("\n(Optional) Gebe das Gewicht in Gramm an:");
            while (true)
            {
                Console.Write(">> ");
                string? weightRaw = Console.ReadLine();
                int weight = -1;

                if (!string.IsNullOrWhiteSpace(weightRaw))
                {
                    if (!int.TryParse(weightRaw, out weight))
                    {
                        Console.WriteLine("Das Gewicht muss eine ganze Zahl in Gramm sein.");
                        continue;
                    }
                    else if (weight > 1000)
                    {
                        Console.WriteLine("Das Gewicht darf nicht 1000g (1kg) überschreiten.");
                        continue;
                    }
                }

                if (weight != -1) address["Weight"] = weight.ToString();
                break;
            }

            if (addressList.Contains("Schweiz"))
            {
                Console.Clear();

                Console.WriteLine("\n(Optional) Gebe die Versandkosten an (Standard: 7,50€):");
                while (true)
                {
                    Console.Write(">> ");
                    string? chargeRaw = Console.ReadLine();

                    if (!string.IsNullOrWhiteSpace(chargeRaw))
                    {
                        if (!double.TryParse(chargeRaw, out customs.postalCharge))
                        {
                            Console.WriteLine("Der Wert muss eine Kommazahl sein, wo die Centbeträge mit einem Komma getrennt werden (7,5).");
                            continue;
                        }
                        else if (customs.postalCharge <= 0)
                        {
                            Console.WriteLine("Der Wert muss mehr als 0 sein.");
                            continue;
                        }
                    }

                    break;
                }

                var items = new List<CustomsItem>();
                Console.Clear();

                Console.WriteLine("Gebe alle Waren an:\n");
                while (true)
                {
                    string itemDescription;
                    int packagedQuantity;
                    double itemValue;
                    int itemWeight;

                    Console.WriteLine("\nGebe die Warenbeschreibung an (Schreibe nichts, wenn alle Waren registriert wurden):");
                    while (true)
                    {
                        Console.Write(">> ");
                        itemDescription = Console.ReadLine()!;

                        if (string.IsNullOrWhiteSpace(itemDescription))
                        {
                            if (items.Count >= 1) goto End;

                            Console.WriteLine("Es muss mindestens ein Artikel angegeben werden.");
                            continue;
                        }
                        else break;
                    }

                    Console.WriteLine("\nGebe die Menge an:");
                    while (true)
                    {
                        Console.Write(">> ");
                        string? quantityRaw = Console.ReadLine();

                        if (!string.IsNullOrWhiteSpace(quantityRaw))
                        {
                            if (!int.TryParse(quantityRaw, out packagedQuantity))
                            {
                                Console.WriteLine("Die Menge muss eine ganze Zahl sein.");
                                continue;
                            }
                            else if (packagedQuantity <= 0)
                            {
                                Console.WriteLine("Die Menge muss mehr als 0 sein.");
                                continue;
                            }
                            else break;
                        }
                    }

                    Console.WriteLine("\nGebe den Warenwert an:");
                    while (true)
                    {
                        Console.Write(">> ");
                        string? valueRaw = Console.ReadLine();

                        if (!string.IsNullOrWhiteSpace(valueRaw))
                        {
                            if (!double.TryParse(valueRaw, out itemValue))
                            {
                                Console.WriteLine("Der Wert muss eine Kommazahl sein, wo die Centbeträge mit einem Komma getrennt werden (7,5).");
                                continue;
                            }
                            else if (itemValue <= 0)
                            {
                                Console.WriteLine("Der Wert muss mehr als 0 sein.");
                                continue;
                            }
                            else break;
                        }
                    }

                    Console.WriteLine("\nGebe das Warengewicht in Gramm an:");
                    while (true)
                    {
                        Console.Write(">> ");
                        string? weightRaw = Console.ReadLine();

                        if (!string.IsNullOrWhiteSpace(weightRaw))
                        {
                            if (!int.TryParse(weightRaw, out itemWeight))
                            {
                                Console.WriteLine("Das Gewicht muss eine ganze Zahl in Gramm sein.");
                                continue;
                            }
                            else if (itemWeight > 1000)
                            {
                                Console.WriteLine("Das Gewicht darf nicht 1000g (1kg) überschreiten.");
                                continue;
                            }
                            else break;
                        }
                    }

                    items.Add(new()
                    {
                        itemDescription = itemDescription,
                        packagedQuantity = packagedQuantity,
                        itemValue = new(itemValue),
                        itemWeight = new(itemWeight)
                    });
                }
            End:;
                customs.items = items.ToArray();
            }

            break;
        }

        return addressList;
    }

    /// <returns>Success</returns>
    /// <exception cref="InvalidDataException"></exception>
    private static bool ProcessAddress(List<string> addressList)
    {
        // For regex. Temporary solution
        var addressString = new StringBuilder();
        foreach (string item in addressList)
        {
            addressString.Append(item);
            addressString.Append('\n');
        }

        for (int i = 0; i < addressList.Count; i++)
        {
            if (PostNumberPattern().IsMatch(addressString.ToString()) && LockerIDPattern().IsMatch(addressString.ToString())) return ProcessAddressPackstation(addressList);
        }

        int index = 0;

        // Cut post number out of first string if existant, else use whole line
        if (PostNumberBehindName().IsMatch(addressList[index]))
        {
            var names = PostNumberBehindName().Split(addressList[index]);
            address["Name"] = names[0];
            address["Name2"] = names[1];
            index++;
        }
        else
        {
            address["Name"] = addressList[index];
            index++;
        }

        if (addressList[index].IndexOfAny(numbers) == -1 && !char.IsDigit(addressList[index + 1].First()))
        {
            // Name3 exists if the 2nd line has no numbers and the line below does not start with one
            address["Name3"] = addressList[index];
            index++;
        }

        // Number comes before street
        if (char.IsDigit(addressList[index].First()))
        {
            var streetAndNumber = addressList[index];

            // Street and number are actually invalid
            if (streetAndNumber.Length <= 1) throw new InvalidDataException();

            if (streetAndNumber.Contains(','))
            {
                var streetNumber = streetAndNumber.Split(',');
                address["Number"] = streetNumber[0];
                for (int i = 1; i < streetNumber.Length; i++)
                {
                    address["Street"] += streetNumber[i] + " ";
                }
            }
            else
            {
                var streetNumber = streetAndNumber.Split(' ');
                address["Number"] = streetNumber[0];
                for (int i = 1; i < streetNumber.Length; i++)
                {
                    address["Street"] += streetNumber[i] + " ";
                }
            }
        }
        else
        {
            var match = AddressPattern().Match(addressString.ToString());
            address["Street"] = match.Groups[1].Value;
            address["Number"] = match.Groups[2].Value;
        }

        {
            var match = PostalCodeCityPattern().Match(addressString.ToString());
            address["Postal Code"] = match.Groups[1].Value;
            address["City"] = match.Groups[2].Value;
        }

        if (europeanCountryDictionary.ContainsKey(addressList[^1])) address["Country"] = addressList[^1];

        // Trim excess white-spaces
        for (int i = 0; i < address.Count; i++)
        {
            address[address.ElementAt(i).Key] = address[address.ElementAt(i).Key]?.Trim();
        }

        try
        {
            consignee = new ContactAddress()
            {
                name1 = address["Name"]!,
                name2 = address["Name2"],
                name3 = address["Name3"],
                addressStreet = address["Street"]!,
                addressHouse = address["Number"]!,
                postalCode = address["Postal Code"]!,
                city = address["City"]!,
                country = address["Country"] is null ? europeanCountryDictionary["Deutschland"] : europeanCountryDictionary[address["Country"]!]
            };
        }
        catch (Exception ex)
        {
            Console.Clear();
            Console.WriteLine("Error: Consignee konnte nicht erstellt werden. Grund:\n");
            Console.WriteLine(ex.ToString() + '\n');
            Console.WriteLine("Input content:");
            var builder = new StringBuilder();
            foreach (var item in addressList)
            {
                builder.AppendLine(item);
            }
            Console.WriteLine(builder.ToString());
            Console.WriteLine();
            Console.WriteLine("Address Dict content:");
            foreach (var item in address)
            {
                Console.WriteLine(item.Key + ": " + item.Value);
            }
            return false;
        }

        return true;
    }

    private static bool ProcessAddressPackstation(List<string> addressList)
    {
        var addressString = new StringBuilder();
        foreach (string line in addressList)
        {
            addressString.Append(line);
            addressString.Append('\n');
        }

        string name = addressList[0];

        string postNumber = PostNumberPattern().Match(addressString.ToString()).Groups[1].Value;

        int lockerID = int.Parse(LockerIDPattern().Match(addressString.ToString()).Groups[1].Value);

        var match = PostalCodeCityPattern().Match(addressString.ToString());
        string postalCode = match.Groups[1].Value;
        string city = match.Groups[2].Value;

        // For display purpose only
        address["Name"] = name;
        address["Name2"] = postNumber;
        address["Street"] = "Packstation";
        address["Number"] = lockerID.ToString();
        address["Postal Code"] = postalCode;
        address["City"] = city;
        address["Country"] = "Deutschland";

        consignee = new Locker(name, postNumber, lockerID, postalCode, city);

        return true;
    }

    private static void WriteAddressToConsole()
    {
        Console.Clear();

        foreach (var line in address)
        {
            Console.WriteLine($"{line.Key}: {line.Value}");
        }
    }

    private static (string line, ConsoleKey? key)? ReadLine(string previousInput, int lineCount)
    {
        Console.Write(">> " + previousInput);

        int pos = Console.CursorLeft;
        int currentPos = pos;
        var chars = new List<char>();

        if (string.IsNullOrEmpty(previousInput) == false)
        {
            chars.AddRange(previousInput.ToCharArray());
        }

        while (true)
        {
            var info = Console.ReadKey(true);

            if (info.Key == ConsoleKey.Enter) return (new string(chars.ToArray()), null);
            else if (info.Key == ConsoleKey.Backspace && Console.CursorLeft > 3)
            {
                Console.CursorLeft = currentPos;
                chars.RemoveAt(Console.CursorLeft - 4);
                Console.Write('\r' + new string(' ', Console.BufferWidth) + '\r');
                Console.Write(">> ");
                Console.Write(chars.ToArray());
                Console.CursorLeft = currentPos;
                currentPos = Console.CursorLeft -= 1;
            }
            else if (info.Key == ConsoleKey.LeftArrow && Console.CursorLeft > 3)
            {
                Console.CursorLeft = currentPos;
                currentPos = Console.CursorLeft -= 1;
            }
            else if (info.Key == ConsoleKey.RightArrow && Console.CursorLeft < chars.Count + 3)
            {
                Console.CursorLeft = currentPos;
                currentPos = Console.CursorLeft += 1;
            }
            else if (info.Key == ConsoleKey.UpArrow && Console.CursorTop > 3) return (new string(chars.ToArray()), info.Key);
            else if (info.Key == ConsoleKey.DownArrow && Console.CursorTop < lineCount + 3) return (new string(chars.ToArray()), info.Key);
            else if (char.IsLetterOrDigit(info.KeyChar) || char.IsWhiteSpace(info.KeyChar))
            {
                Console.CursorLeft = currentPos;
                chars.Insert(Console.CursorLeft - 3, info.KeyChar);
                var builder = new StringBuilder();
                foreach (var c in chars)
                {
                    builder.Append(c);
                }
                Console.Write("\r>> ");
                Console.Write(chars.ToArray());
                Console.Write(new string(' ', Math.Max(0, Console.BufferWidth - chars.ToArray().Length - 3)));
                Console.CursorLeft = currentPos;
                currentPos = Console.CursorLeft += 1;
                if (Environment.OSVersion.Version.Build < 22000) Console.CursorTop--; // On Win10, the cursor moves down when writing. On Win11, it doesn't.
            }
            else if (info.Key == ConsoleKey.Escape)
            {
                return null;
            }
        }
    }
}
