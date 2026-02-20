namespace ProperTea.Infrastructure.Common.Address;

/// <summary>
/// ISO 3166-1 numeric codes. Alpha-2 names used as enum members for readability.
/// Curated for European Union and EEA member states plus Ukraine and other key markets.
/// </summary>
public enum Country
{
    // Eastern Europe
    UA = 804, // Ukraine
    PL = 616, // Poland
    CZ = 203, // Czech Republic
    SK = 703, // Slovakia
    HU = 348, // Hungary
    RO = 642, // Romania
    BG = 100, // Bulgaria
    MD = 498, // Moldova

    // Western Europe
    DE = 276, // Germany
    AT = 40,  // Austria
    CH = 756, // Switzerland
    FR = 250, // France
    BE = 56,  // Belgium
    NL = 528, // Netherlands
    LU = 442, // Luxembourg

    // Northern Europe
    SE = 752, // Sweden
    NO = 578, // Norway
    FI = 246, // Finland
    DK = 208, // Denmark
    IS = 352, // Iceland
    EE = 233, // Estonia
    LV = 428, // Latvia
    LT = 440, // Lithuania

    // Southern Europe
    IT = 380, // Italy
    ES = 724, // Spain
    PT = 620, // Portugal
    GR = 300, // Greece
    HR = 191, // Croatia
    SI = 705, // Slovenia
    RS = 688, // Serbia
    ME = 499, // Montenegro
    BA = 70,  // Bosnia and Herzegovina
    MK = 807, // North Macedonia
    AL = 8,   // Albania

    // British Isles
    GB = 826, // United Kingdom
    IE = 372, // Ireland

    // Other
    CY = 196, // Cyprus
    MT = 470  // Malta
}
