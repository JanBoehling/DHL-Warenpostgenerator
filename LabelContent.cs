#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1822 // Mark members as static

namespace TESTING_WeddingtreeV1;

public class LabelContent
{
    public string profile => "STANDARD_GRUPPENPROFIL";
    public List<Shipment> shipments { get; private set; }

    public static string? ShipperCustomsRef { get; private set; } = null;

    public static Shipper Shipper => shipper;

    // Sandbox values
    private static DHLProduct warenpostDEU = new()
    {
        ProduktCode = "V62WP",
        Abrechnungsnummer = "33333333336201"
    };

    private static DHLProduct warenpostINT = new()
    {
        ProduktCode = "V66WPI",
        Abrechnungsnummer = "33333333336604"
    };

    private static DHLProduct parcelDEU = new()
    {
        ProduktCode = "V01PAK",
        Abrechnungsnummer = "33333333330103"
    };

    private static DHLProduct parcelINT = new()
    {
        ProduktCode = "V53WPAK",
        Abrechnungsnummer = "33333333335302"
    };

    private static Shipper shipper = new()
    {
        name1 = "My Online Shop GmbH",
        addressStreet = "Sträßchensweg",
        addressHouse = "10",
        postalCode = "53113",
        city = "Bonn",
        country = "DEU",
        email = "max@mustermann.de",
        contactName = "Max Mustermann"
    };

    private readonly bool isGermany;

    public LabelContent(IConsignee consignee, string? referenceNumber = null, int weight = -1, double postalCharge = 7.5, params CustomsItem[] items)
    {
        isGermany = consignee.country.Equals("DEU");

        string product;
        string billingNumber;

        if (Program.DoParcel)
        {
            product = isGermany ? parcelDEU.ProduktCode : parcelINT.ProduktCode;
            billingNumber = isGermany ? parcelDEU.Abrechnungsnummer : parcelINT.Abrechnungsnummer;
        }
        else
        {
            product = isGermany ? warenpostDEU.ProduktCode : warenpostINT.ProduktCode;
            billingNumber = isGermany ? warenpostDEU.Abrechnungsnummer : warenpostINT.Abrechnungsnummer;
        }

        if (weight == -1) weight = isGermany ? 500 : 100;

        shipments = new(1)
        {
            new()
            {
                product = product,
                billingNumber = billingNumber,
                refNo = referenceNumber,
                shipper = shipper,
                consignee = consignee,
                details = new(weight),
                services = new(),
                customs = consignee.country.Equals("CHE") ? new Customs(postalCharge, items) : null
            }
        };
    }

    public static void SetCredentials(Credentials credentials)
    {
#if DEBUG
#else
        warenpostDEU = credentials.WarenpostDeutschland;
        warenpostINT = credentials.WarenpostInternational;
        parcelDEU = credentials.PaketDeutschland;
        parcelINT = credentials.PaketInternational;
#endif

        ShipperCustomsRef = credentials.ZollReferenzNummer;
        shipper = credentials.Shipper;
    }
}

public readonly struct Shipment
{
    public string product { get; init; }
    public string billingNumber { get; init; }
    public string? refNo { get; init; }
    public Shipper shipper { get; init; }
    public IConsignee consignee { get; init; }
    public Details details { get; init; }
    public Services? services { get; init; }
    public Customs? customs { get; init; }
}

public struct Shipper
{
    public string name1 { get; set; }
    public string addressStreet { get; set; }
    public string addressHouse { get; set; }
    public string postalCode { get; set; }
    public string city { get; set; }
    public string country { get; set; }
    public string contactName { get; set; }
    public string email { get; set; }
}

public interface IConsignee
{
    public string country { get; init; }
}

public readonly struct ContactAddress : IConsignee
{
    public string name1 { get; init; }
    public string? name2 { get; init; }
    public string? name3 { get; init; }
    public string addressStreet { get; init; }
    public string addressHouse { get; init; }
    public string postalCode { get; init; }
    public string city { get; init; }
    public string country { get; init; }

    public override readonly string ToString()
    {
        var builder = new System.Text.StringBuilder();

        builder.AppendLine($"Name1:\t{name1}");
        builder.AppendLine($"Name2:\t{name2}");
        builder.AppendLine($"Straße:\t{addressStreet}");
        builder.AppendLine($"Nummer:\t{addressHouse}");
        builder.AppendLine($"PLZ:\t{postalCode}");
        builder.AppendLine($"Stadt:\t{city}");
        builder.AppendLine($"Land:\t{country}");
        
        return builder.ToString();
    }
}

public readonly struct Locker : IConsignee
{
    public string name { get; init; }
    public int lockerID { get; init; }
    public string postNumber { get; init; }
    public string city { get; init; }
    public string country { get; init; }
    public string postalCode { get; init; }

    public Locker(string name, string postNumber, int lockerID, string postalCode, string city)
    {
        this.name = name;
        this.lockerID = lockerID;
        this.postNumber = postNumber;
        this.city = city;
        this.country = "DEU";
        this.postalCode = postalCode;
    }

    public override readonly string ToString()
    {
        var builder = new System.Text.StringBuilder();

        builder.AppendLine($"Name:\t{name}");
        builder.AppendLine($"Packstation-nummer:\t{lockerID}");
        builder.AppendLine($"Postnummer:\t{postNumber}");
        builder.AppendLine($"PLZ:\t{postalCode}");
        builder.AppendLine($"Stadt:\t{city}");
        builder.AppendLine($"Land:\t{country}");

        return builder.ToString();
    }
}

public struct Details
{
    public Weight weight { get; private set; }

    /// <summary>
    /// Weight is in gramms
    /// </summary>
    public Details(int weight)
    {
        this.weight = new(weight);
    }
    public Details()
    {
        this.weight = new(500);
    }
}

public struct Weight
{
    public readonly string uom => "g";
    public int value { get; private set; }

    /// <summary>
    /// Weight of the package. Value is in gramms
    /// </summary>
    public Weight(int gramms) => value = gramms;
}

public struct Services
{
    public readonly string parcelOutletRouting => LabelContent.Shipper.email;
    public readonly bool premium => true;
}

public struct Customs
{
    public readonly string exportType => "COMMERCIAL_GOODS";
    public Value postalCharges { get; private set; }
    public readonly string? shipperCustomsRef => LabelContent.ShipperCustomsRef;
    public CustomsItem[]? items { get; private set; }

    public Customs(double postalCharge, CustomsItem[] items)
    {
        postalCharges = new(postalCharge);
        this.items = items;
    }
}

public struct Value
{
    public readonly string currency => "EUR";
    public double value { get; private set; }

    public Value(double value) => this.value = value;
}

public readonly struct CustomsItem
{
    public string itemDescription { get; init; }
    public readonly string countryOfOrigin => "DEU";
    public int packagedQuantity { get; init; }
    public Value itemValue { get; init; }
    public Weight itemWeight { get; init; }
}
