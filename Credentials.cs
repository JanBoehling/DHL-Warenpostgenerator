namespace TESTING_WeddingtreeV1;

[Serializable]
public struct Credentials
{
    public string Username;
    public string Password;
    public string ApiSchluessel;
     
    public DHLProduct WarenpostDeutschland;
    public DHLProduct WarenpostInternational;
    public DHLProduct PaketDeutschland;
    public DHLProduct PaketInternational;

    public string LogPath;

    public string ZollReferenzNummer;

    public Shipper Shipper;

    public Credentials()
    {
        Username = "";
        Password = "";
        ApiSchluessel = "";

        WarenpostDeutschland = new()
        {
            ProduktCode = "",
            Abrechnungsnummer = ""
        };
        WarenpostInternational = new()
        {
            ProduktCode = "",
            Abrechnungsnummer = ""
        };
        PaketDeutschland = new()
        {
            ProduktCode = "",
            Abrechnungsnummer = ""
        };
        PaketInternational = new()
        {
            ProduktCode = "",
            Abrechnungsnummer = ""
        };

        LogPath = "";
        ZollReferenzNummer = "";

        Shipper = new()
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
    }
}
