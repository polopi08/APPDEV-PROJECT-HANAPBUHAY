using System;
using System.Collections.Generic;
using System.Linq;

namespace APPDEV_PROJECT.Helpers
{
    public class WorkerFilterHelper
    {
        public const double EarthRadiusKm = 6371;
        public const double MaxDistanceKm = 3.0;

        public class WorkerWithDistance
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Skill { get; set; }
            public double Lat { get; set; }
            public double Lng { get; set; }
            public double DistanceKm { get; set; }
        }

        /// <summary>
        /// Calculate distance between two geographic coordinates using Haversine formula
        /// </summary>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) *
                       Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        /// <summary>
        /// Get workers near a location, filtered by distance and optionally by skill
        /// </summary>
        public static List<WorkerWithDistance> GetNearbyWorkers(
            List<WorkerWithDistance> allWorkers,
            double userLat,
            double userLng,
            string skillFilter = "",
            string searchQuery = "")
        {
            return allWorkers
                .Select(worker => new WorkerWithDistance
                {
                    Id = worker.Id,
                    Name = worker.Name,
                    Skill = worker.Skill,
                    Lat = worker.Lat,
                    Lng = worker.Lng,
                    DistanceKm = CalculateDistance(userLat, userLng, worker.Lat, worker.Lng)
                })
                .Where(w => w.DistanceKm <= MaxDistanceKm)
                .Where(w => string.IsNullOrEmpty(skillFilter) || 
                           w.Skill.Equals(skillFilter, StringComparison.OrdinalIgnoreCase))
                .Where(w => string.IsNullOrEmpty(searchQuery) || 
                           w.Skill.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => w.DistanceKm)
                .ToList();
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}
