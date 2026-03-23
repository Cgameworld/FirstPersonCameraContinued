using System.Collections.Generic;

namespace FirstPersonCameraContinued.Enums
{
    public enum VehicleTypeCategory
    {
        Unknown,
        CityServices,
        PublicTransit,
        Cargo,
        PrivateTransport,
        Aviation
    }

    public static class VehicleTypeCategoryExtensions
    {
        private static readonly Dictionary<VehicleType, VehicleTypeCategory> CategoryMap = new()
        {
            { VehicleType.Ambulance, VehicleTypeCategory.CityServices },
            { VehicleType.GarbageTruck, VehicleTypeCategory.CityServices },
            { VehicleType.FireEngine, VehicleTypeCategory.CityServices },
            { VehicleType.PoliceCar, VehicleTypeCategory.CityServices },
            { VehicleType.Hearse, VehicleTypeCategory.CityServices },
            { VehicleType.MaintenanceVehicle, VehicleTypeCategory.CityServices },
            { VehicleType.WorkVehicle, VehicleTypeCategory.CityServices },
            { VehicleType.PostVan, VehicleTypeCategory.CityServices },

            { VehicleType.Bus, VehicleTypeCategory.PublicTransit },
            { VehicleType.Tram, VehicleTypeCategory.PublicTransit },
            { VehicleType.Train, VehicleTypeCategory.PublicTransit },
            { VehicleType.Subway, VehicleTypeCategory.PublicTransit },
            { VehicleType.Ferry, VehicleTypeCategory.PublicTransit },
            { VehicleType.Ship, VehicleTypeCategory.PublicTransit },

            { VehicleType.DeliveryTruck, VehicleTypeCategory.Cargo },
            { VehicleType.CargoTransport, VehicleTypeCategory.Cargo },
            { VehicleType.CargoTrain, VehicleTypeCategory.Cargo },

            { VehicleType.PersonalCar, VehicleTypeCategory.PrivateTransport },
            { VehicleType.Taxi, VehicleTypeCategory.PrivateTransport },
            { VehicleType.Bicycle, VehicleTypeCategory.PrivateTransport },
            { VehicleType.ElectricScooter, VehicleTypeCategory.PrivateTransport },

            { VehicleType.Helicopter, VehicleTypeCategory.Aviation },
            { VehicleType.Aircraft, VehicleTypeCategory.Aviation },
        };

        public static VehicleTypeCategory GetCategory(this VehicleType vehicleType)
        {
            return CategoryMap.TryGetValue(vehicleType, out var category)
                ? category
                : VehicleTypeCategory.Unknown;
        }
    }
}
