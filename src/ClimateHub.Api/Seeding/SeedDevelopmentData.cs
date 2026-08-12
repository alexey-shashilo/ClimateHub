using ClimateHub.Modules.Building.Domain.Aggregates;
using ClimateHub.Modules.Building.Infrastructure;
using ClimateHub.Modules.Devices.Domain.Aggregates;
using ClimateHub.Modules.Devices.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Api.Seeding;

public static class SeedDevelopmentData
{
    public static async Task SeedAsync(
        BuildingDbContext buildingDb,
        DevicesDbContext devicesDb,
        EnvironmentDbContext environmentDb,
        CancellationToken ct = default)
    {
        if (await buildingDb.Buildings.AnyAsync(ct)) return;

        var building = Building.Create("Главный дом");
        building.AddFloor("Первый этаж", 1);
        building.AddFloor("Второй этаж", 2);
        buildingDb.Buildings.Add(building);
        await buildingDb.SaveChangesAsync(ct);

        var floor1 = building.Floors.First(f => f.Level == 1);
        var floor2 = building.Floors.First(f => f.Level == 2);

        floor1.AddRoom("Гостиная");
        floor1.AddRoom("Кухня");
        floor1.AddRoom("Прихожая");
        floor2.AddRoom("Спальня");
        floor2.AddRoom("Детская");
        floor2.AddRoom("Кабинет");
        floor2.AddRoom("Ванная");

        buildingDb.Floors.Update(floor1);
        buildingDb.Floors.Update(floor2);
        await buildingDb.SaveChangesAsync(ct);

        var rooms = await buildingDb.Rooms.ToListAsync(ct);
        var roomGostinaya = rooms.First(r => r.Name == "Гостиная");
        var roomKuhnya = rooms.First(r => r.Name == "Кухня");
        var roomSpalnya = rooms.First(r => r.Name == "Спальня");
        var roomKabinet = rooms.First(r => r.Name == "Кабинет");
        var roomPrikh = rooms.First(r => r.Name == "Прихожая");
        var roomDetskaya = rooms.First(r => r.Name == "Детская");
        var roomVannaya = rooms.First(r => r.Name == "Ванная");

        var d1 = Device.Register("ESP32-S3-001", "Датчик гостиной", new DeviceModel("Acme", "ClimateNode v2", "2.1.0"), "1.0");
        d1.Activate();
        d1.AddCapability(DeviceCapability.Temperature(d1.Id));
        d1.AddCapability(DeviceCapability.Humidity(d1.Id));
        d1.AddCapability(DeviceCapability.Co2(d1.Id));

        var d2 = Device.Register("STM32-001", "Датчик CO₂ спальни", new DeviceModel("Acme", "CO2Sensor Pro"), "1.0");
        d2.Activate();
        d2.AddCapability(DeviceCapability.Co2(d2.Id));

        var d3 = Device.Register("ESP32-S3-002", "Метеостанция кухни", new DeviceModel("Acme", "ClimateNode v2", "2.1.0"), "1.0");
        d3.Activate();
        d3.AddCapability(DeviceCapability.Temperature(d3.Id));
        d3.AddCapability(DeviceCapability.Humidity(d3.Id));
        d3.AddCapability(DeviceCapability.Co2(d3.Id));

        var d4 = Device.Register("ESP32-S3-003", "Датчик кабинета", new DeviceModel("Acme", "ClimateNode v1"), "1.0");
        d4.AddCapability(DeviceCapability.Temperature(d4.Id));
        d4.AddCapability(DeviceCapability.Humidity(d4.Id));
        d4.AddCapability(DeviceCapability.Co2(d4.Id));

        var d5 = Device.Register("ESP32-S3-004", "Датчик детской", new DeviceModel("Acme", "ClimateNode v2", "2.0.0"), "1.0");
        d5.AddCapability(DeviceCapability.Temperature(d5.Id));
        d5.AddCapability(DeviceCapability.Humidity(d5.Id));

        var d6 = Device.Register("STM32-002", "Датчик прихожей", new DeviceModel("Acme", "ClimateNode Basic"), "1.0");
        d6.AddCapability(DeviceCapability.Temperature(d6.Id));

        var d7 = Device.Register("ESP32-S3-005", "Датчик ванной", new DeviceModel("Acme", "ClimateNode v2", "2.1.0"), "1.0");
        d7.AddCapability(DeviceCapability.Temperature(d7.Id));
        d7.AddCapability(DeviceCapability.Humidity(d7.Id));

        devicesDb.Devices.AddRange(d1, d2, d3, d4, d5, d6, d7);
        await devicesDb.SaveChangesAsync(ct);

        d1.AssignToRoom(roomGostinaya.Id);
        d2.AssignToRoom(roomSpalnya.Id);
        d3.AssignToRoom(roomKuhnya.Id);
        d4.AssignToRoom(roomKabinet.Id);
        d5.AssignToRoom(roomDetskaya.Id);
        d6.AssignToRoom(roomPrikh.Id);
        d7.AssignToRoom(roomVannaya.Id);
        await devicesDb.SaveChangesAsync(ct);

        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        environmentDb.Set<RoomParameterEntity>().AddRange(
            new RoomParameterEntity { RoomId = roomGostinaya.Id, Parameter = "temperature", Value = 22.4, Unit = "celsius", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d1.Id },
            new RoomParameterEntity { RoomId = roomGostinaya.Id, Parameter = "humidity", Value = 42.0, Unit = "percent", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d1.Id },
            new RoomParameterEntity { RoomId = roomGostinaya.Id, Parameter = "co2", Value = 735.0, Unit = "ppm", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d1.Id },
            new RoomParameterEntity { RoomId = roomKuhnya.Id, Parameter = "temperature", Value = 24.1, Unit = "celsius", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d3.Id },
            new RoomParameterEntity { RoomId = roomKuhnya.Id, Parameter = "humidity", Value = 38.0, Unit = "percent", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d3.Id },
            new RoomParameterEntity { RoomId = roomKuhnya.Id, Parameter = "co2", Value = 680.0, Unit = "ppm", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d3.Id },
            new RoomParameterEntity { RoomId = roomSpalnya.Id, Parameter = "temperature", Value = 23.5, Unit = "celsius", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d2.Id },
            new RoomParameterEntity { RoomId = roomSpalnya.Id, Parameter = "co2", Value = 1250.0, Unit = "ppm", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d2.Id },
            new RoomParameterEntity { RoomId = roomKabinet.Id, Parameter = "temperature", Value = 21.7, Unit = "celsius", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d4.Id },
            new RoomParameterEntity { RoomId = roomKabinet.Id, Parameter = "humidity", Value = 18.0, Unit = "percent", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d4.Id },
            new RoomParameterEntity { RoomId = roomKabinet.Id, Parameter = "co2", Value = 1100.0, Unit = "ppm", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d4.Id },
            new RoomParameterEntity { RoomId = roomDetskaya.Id, Parameter = "temperature", Value = 22.8, Unit = "celsius", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d5.Id },
            new RoomParameterEntity { RoomId = roomPrikh.Id, Parameter = "temperature", Value = 20.0, Unit = "celsius", MeasuredAt = baseTime.AddMinutes(-120), ReceivedAt = DateTimeOffset.UtcNow.AddMinutes(-60), Quality = "stale", SourceDeviceId = d6.Id },
            new RoomParameterEntity { RoomId = roomVannaya.Id, Parameter = "temperature", Value = 25.0, Unit = "celsius", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d7.Id },
            new RoomParameterEntity { RoomId = roomVannaya.Id, Parameter = "humidity", Value = 65.0, Unit = "percent", MeasuredAt = baseTime, ReceivedAt = DateTimeOffset.UtcNow, Quality = "valid", SourceDeviceId = d7.Id }
        );
        await environmentDb.SaveChangesAsync(ct);
    }
}
