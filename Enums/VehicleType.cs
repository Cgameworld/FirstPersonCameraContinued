using System;

namespace FirstPersonCameraContinued.Enums
{
    [Flags] // Indicates that the enum can be treated as a set of flags (bitwise operations)
    public enum VehicleType : long // Explicitly set the underlying type to long
    {
        Unknown = 0,
        PersonalCar = 1L << 0, // 1
        PostVan = 1L << 1, // 2
        PoliceVan = 1L << 2, // 4
        PoliceCar = 1L << 3, // 8
        MaintenanceVehicle = 1L << 4, // 16
        Ambulance = 1L << 5, // 32
        GarbageTruck = 1L << 6, // 64
        FireEngine = 1L << 7, // 128
        DeliveryTruck = 1L << 8, // 256
        Hearse = 1L << 9, // 512
        CargoTransport = 1L << 10, // 1024
        Taxi = 1L << 11, // 2048
        CarTrailer = 1L << 12, // 4096
        Helicopter = 1L << 13, // 8192

        Bus = 1L << 14,
        Tram = 1L << 15,
        Train = 1L << 16,
        Subway = 1L << 17,
        Ship = 1L << 18,
        Aircraft = 1L << 19,

        CargoTrain = 1L << 20,
        Ferry = 1L << 21,
        WorkVehicle = 1L << 22,
        Bicycle = 1L << 23,
        ElectricScooter = 1L << 24,

        Cars = PersonalCar | PoliceCar | Hearse | Taxi,
        Vans = PostVan | PoliceVan | Ambulance | MaintenanceVehicle,
        Trucks = GarbageTruck | FireEngine | DeliveryTruck | CargoTransport,
        Transit = Bus | Tram | Train | Subway
    }
}
