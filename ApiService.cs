using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace TESTING_WeddingtreeV1;

internal static class ApiService
{
    private static string _username = "sandy_sandbox";
    private static string _password = "pass";
    private static string _key = "";
    private static string baseRequestUri = "https://api-sandbox.dhl.com/parcel/de/shipping/v2/";

    public static void SetCredentials(string username, string password, string key)
    {
#if DEBUG
#else
        _username = username;
        _password = password;
        baseRequestUri = "https://api-eu.dhl.com/parcel/de/shipping/v2/";
#endif
        _key = key;
    }

    public static async Task CreateShipmentLabel(IConsignee consignee, int weight, string? referenceNumber = null, double postalCharge = 7.5, params CustomsItem[] items)
    {
        (bool success, string? message) = ValidateShipmentLabel(consignee, weight, referenceNumber: null, postalCharge, items).Result;
        if (!success || message is not null)
        {
            Console.Clear();
            Logger.Log(message);
            if (success) // WARNING
            {
                Console.WriteLine("\n\nUm trotzdem zu drucken, \"Drucken\" schreiben.");
                var doContinueInput = Console.ReadLine();
                if (doContinueInput is not null && !doContinueInput.ToLower().Equals("drucken")) return;
            }
            else
            {
                Console.ReadKey(true);
                return;
            }
        }

        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, baseRequestUri + "orders?includeDocs=URL&printFormat=910-300-700");
        request.Headers.Add("dhl-api-key", _key);
        request.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"))}");

        var content = SerializeObject(new LabelContent(consignee, referenceNumber, weight, postalCharge, items));

        request.Content = content;
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            Console.Clear();
            Logger.Log(body);
            Console.ReadKey(true);
            return;
        }

        dynamic? data = null;
        try
        {
            data = JObject.Parse(body);
            var url = data.items[0].label.url;
            

            string shipmentNumber = data.items[0].shipmentNo;
            TextCopy.ClipboardService.SetText(shipmentNumber);

            new Process()
            {
                StartInfo =
                {
                    UseShellExecute = true,
                    FileName = url
                }
            }.Start();

            if (Program.DoParcel && consignee.country.Equals("CHE"))
            {
                var customsDoc = data.items[0].customsDoc.url;

                new Process()
                {
                    StartInfo =
                        {
                            UseShellExecute = true,
                            FileName = customsDoc
                        }
                }.Start();
            }

            Logger.Log(body);
        }
        catch (Exception ex)
        {
            Console.Clear();
            Logger.Log(body, ex.ToString(), data);
            Console.ReadKey(true);
            return;
        }
    }

    private static async Task<(bool success, string? message)> ValidateShipmentLabel(IConsignee consignee, int weight, string? referenceNumber, double postalCharge, CustomsItem[] items)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, baseRequestUri + "orders?includeDocs=URL&printFormat=910-300-700&validate=true");
        request.Headers.Add("dhl-api-key", _key);
        request.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"))}");

        var label = new LabelContent(consignee, referenceNumber, weight, postalCharge, items);
        var content = SerializeObject(label);

        request.Content = content;
        dynamic data;
        try
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                Console.Clear();
                Logger.Log(body);
                Console.ReadKey(true);
                return (false, body);
            }

            data = JObject.Parse(body);
        }
        catch (Exception ex)
        {
            return (false, ex.ToString());
        }

        var statusCode = data.status.statusCode;

        if (statusCode == 200) return (true, null);

        Console.WriteLine(data.items[0].validationMessage);
        Console.ReadLine();

        var builder = new StringBuilder();
        foreach ( var item in data.items[0].validationMessages)
        {
            if (item.validationMessage == "The street entered could not be found.")
            {
                builder.AppendLine("Die Straße konnte nicht gefunden werden.");
            }
            else if (item.validationMessage == "The postcode is invalid. Please use the format 99999. You may, however, still print a shipping label.")
            {
                builder.AppendLine("Die Postleitzahl konnte nicht erkannt werden.");
            }
            else if (item.validationMessage == "The city entered does not match the postcode. The shipment is not codeable.")
            {
                builder.AppendLine("Die Stadt befindet sich nicht in der Postleitzahl");
            }
            else if (item.validationMessage == "The house number entered could not be found.")
            {
                builder.AppendLine("Die Hausnummer konnte nicht gefunden werden.");
            }
            else builder.AppendLine(item.ToString());
        }
        //builder.AppendLine();
        //foreach ( var item in content.ReadAsStream().)
        //{
        //
        //}
        //builder.AppendLine();

        foreach (var item in data.items[0].validationMessages)
        {
            if (item.validationState != "Warning") return (false, builder.ToString());
        }

        return (true, builder.ToString() + "\n" + consignee + "\n");
    }

    public static async Task CancelShipment(string trackingNumber)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, baseRequestUri + $"orders?profile=STANDARD_GRUPPENPROFIL&shipment={trackingNumber}");
        request.Headers.Add("dhl-api-key", _key);
        request.Headers.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"))}");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            Console.Clear();
            Logger.Log(body);
            Console.ReadKey(true);
            return;
        }
        Console.WriteLine($"Bestellung {trackingNumber} erfolgreich gelöscht.");
        Console.ReadKey();
    }

    private static HttpContent SerializeObject(LabelContent content)
    {
        return new StringContent(
            JsonConvert.SerializeObject(content),
            new MediaTypeHeaderValue("application/json"));
    }
}
