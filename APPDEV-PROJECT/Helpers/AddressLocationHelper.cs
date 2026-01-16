using System;
using System.Collections.Generic;

namespace APPDEV_PROJECT.Helpers
{
    public class AddressLocationHelper
    {
        public const string AddressFormat = "Building/Establishment Name, Street Name, Barangay/Area/District, City/Municipality, Province/Metropolitan Area, Country";

        /// <summary>
        /// San Juan barangays with their coordinates
        /// </summary>
        public static Dictionary<string, (double lat, double lng)> SanJuanBarangays = new()
        {
            { "San Juan Greenhills", (14.602981736177803, 121.04488737237482) },
            { "San Juan Addition Hills", (14.593957651465185, 121.03816375432932) },
            { "Corazon de Jesus", (14.606833931096597, 121.03067168045362) },
            { "West Crame", (14.607717930795932, 121.04981118308083) },
            { "West Greenhills", (14.598770424566561, 121.04580245503976) },
            { "San Juan Pinaglabanan", (14.605104730989897, 121.0288131464939) },
            { "Balong-Bato", (14.609072024777793, 121.02585726853202) },
            { "F. Manalo", (14.598387248539703, 121.02525322515011) },
            { "San Juan del Monte", (14.59865706629644, 121.03040301434575) }
        };

        /// <summary>
        /// Validates if an address is in the correct format
        /// </summary>
        public static bool IsValidAddressFormat(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            var parts = address.Split(',');
            return parts.Length >= 5; // At least 5 parts separated by commas
        }

        /// <summary>
        /// Extracts the barangay from the address (3rd part)
        /// Address format: Building, Street, Barangay, City, Province, Country
        /// </summary>
        public static string ExtractBarangay(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return string.Empty;

            var parts = address.Split(',');
            if (parts.Length >= 3)
            {
                return parts[2].Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if the barangay is in San Juan
        /// </summary>
        public static bool IsInSanJuan(string barangay)
        {
            if (string.IsNullOrWhiteSpace(barangay))
                return false;

            return SanJuanBarangays.ContainsKey(barangay);
        }

        /// <summary>
        /// Gets coordinates for a San Juan barangay
        /// </summary>
        public static (double? lat, double? lng) GetCoordinatesForBarangay(string barangay)
        {
            if (string.IsNullOrWhiteSpace(barangay))
                return (null, null);

            if (SanJuanBarangays.TryGetValue(barangay, out var coords))
            {
                return (coords.lat, coords.lng);
            }

            return (null, null);
        }

        /// <summary>
        /// Processes address and returns coordinates
        /// Returns null if address is not in San Juan
        /// </summary>
        public static (double? lat, double? lng) ProcessAddressAndGetCoordinates(string address)
        {
            if (!IsValidAddressFormat(address))
                return (null, null);

            var barangay = ExtractBarangay(address);
            
            if (!IsInSanJuan(barangay))
                return (null, null);

            return GetCoordinatesForBarangay(barangay);
        }
    }
}
