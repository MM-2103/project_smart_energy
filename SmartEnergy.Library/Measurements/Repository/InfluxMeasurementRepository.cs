using System.Diagnostics;
using InfluxDB.Client;
using SmartEnergy.Library.Measurements.Models;

namespace SmartEnergy.Library.Measurements.Repository;

/// <summary>
/// Implementation of the <c>IMeasurementRepository<c> interface.
/// </summary>
public class InfluxMeasurementRepository //: IMeasurementRepository
{
    private readonly IInfluxDBClient _client;
    private readonly List<string> _manufacturerMacs = ["16405E", "144068", "164068", "1640EF","1640D8"];

    public InfluxMeasurementRepository(IInfluxDBClient client)
    {
        _client = client;
    }

    public List<Measurement> GetEnergyConsumed(int meterId, int daysToRetrieve, string aggregateWindow)
    {
        // Forward this request to a common private method and add specific data for this request (sensor and unit values)
        return QueryOdoMeterReading(meterId, daysToRetrieve, aggregateWindow, Sensor.energy_consumed, Unit.KilowattHour);
    }

    public List<Measurement> GetEnergyProduced(int meterId, int daysToRetrieve, string aggregateWindow)
    {
        // Forward this request to a common private method and add specific data for this request (sensor and unit values)
        return QueryOdoMeterReading(meterId, daysToRetrieve, aggregateWindow, Sensor.energy_produced, Unit.KilowattHour);
    }

    public List<Measurement> GetGasDelivered(int meterId, int daysToRetrieve, string aggregateWindow)
    {
        // Forward this request to a common private method and add specific data for this request (sensor and unit values)
        return QueryOdoMeterReading(meterId, daysToRetrieve, aggregateWindow, Sensor.gas_delivered, Unit.CubicMeter);
    }

    public List<Measurement> GetPower(int meterId, int daysToRetrieve, string aggregateWindow)
    {
        foreach (string manufacturerMac in _manufacturerMacs)
        {
            List<Measurement> measurements = QueryPowerReadingForManufacturer(meterId, daysToRetrieve, aggregateWindow, manufacturerMac);
            if (measurements.Count > 0)
            {
                return measurements;
            }
            // else we continue with the next manufacturer mac address
        }

        // if no results are found it's most likely an invalid meterId, no results
        return [];
    }

    public List<Measurement> QueryPowerReadingForManufacturer(int meterId, int daysToRetrieve, string aggregateWindow, string manufacturerMac)
    {
        var measurements = new List<Measurement>();
        // A stopwatch is used so we can monitor the time it took to retrieve and process the data from the influx database
        var startTime = Stopwatch.StartNew();

        // This is an influx query. Influx processes this and returns the data based on the parameter you provide in the query
        string query =
            $"from(bucket: \"p1-smartmeters\")" +
            $"  |> range(start: {getStartDate(daysToRetrieve)}, stop: now())" +
            $"  |> filter(fn: (r) => r[\"signature\"] == \"{getFullMeterIdInHex(meterId, manufacturerMac)}\")" +
            $"  |> filter(fn: (r) => r[\"_field\"] == \"power_consumed\" or r[\"_field\"] == \"power_produced\")" +
            $"  |> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")" +
            $"  |> map(fn: (r) => ({{r with _value: r.power_consumed - r.power_produced}}))" +
            $"  |> aggregateWindow(every: {aggregateWindow}, fn: mean, createEmpty: false)" +
            $"  |> yield(name: \"mean\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // retrieve the temperature and price data to enrich measurements with this information
        var energyPrices = EnrichMeasurementsWithEnergyPrice(daysToRetrieve);
        var temperatures = EnrichMeasurementsWithTemperature(daysToRetrieve, aggregateWindow);

        // Make an API Call to the Influx server to retrieve the data requested by the query
        var fluxTables = _client.GetQueryApi().QueryAsync(query).GetAwaiter().GetResult();

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTable. These records
        // are then converted to Measurement object and stored in the measurements list.
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                // Get the right fields we need from the record and store it in a local variable for later use
                DateTime dateTime = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc();
                string locationId = (string)record.GetValueByKey("signature");
                double value = (double)record.GetValueByKey("_value") * 1000;

                // round up to the full hour so we can lookup the energy price for the hourly slot
                DateTime dateTimeRounded = dateTime.AddMinutes(dateTime.Minute * -1).AddSeconds(dateTime.Second * -1);

                // We use the earlier fetched data to set energyprice and temperature on the retrieved date/time of the measurement data
                energyPrices.TryGetValue(dateTimeRounded.Ticks, out var energyPrice);
                temperatures.TryGetValue(dateTime.Ticks, out var temperature);

                // Create a new Measurement object and add it to the list.
                var singleMeasurement = new Measurement
                {
                    Timestamp = dateTime,
                    LocationId = locationId,
                    Sensor = Sensor.power,
                    Value = value,
                    Unit = Unit.Watt,
                    EnergyPrice = energyPrice,
                    Temperature = temperature
                };

                measurements.Add(singleMeasurement);
            }
        }

        // Print the time it has taken to query and process the data before returning the list of measurements
        Console.WriteLine("Time consumed for API Call: " + startTime.ElapsedMilliseconds + "ms");
        return measurements;
    }

    /// <summary>
    /// Shared method with common implementation for querying the influx database.
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregateWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    /// <param name="sensor">Type of data to retrieve from the influx database</param>
    /// <param name="unit">The unit of representation that is linked to the <c>sensor</c> information</param>
    /// <returns></returns>
    private List<Measurement> QueryOdoMeterReading(int meterId, int daysToRetrieve, string aggregateWindow, Sensor sensor, Unit unit)
    {
        foreach (string manufacturerMac in _manufacturerMacs)
        {
            List<Measurement> measurements = QueryOdoMeterReadingForManufacturer(meterId, daysToRetrieve, aggregateWindow, sensor, unit, manufacturerMac);
            if (measurements.Count > 0)
            {
                return measurements;
            }
            // else we continue with the next manufacturer mac address
        }

        // if no results are found it's most likely an invalid meterId, no results
        return [];
    }

    /// <summary>
    /// Shared method with common implementation for querying the influx database.
    /// </summary>
    /// <param name="meterId">Decimal representation of the Hexadecimal ID of the P1 meter to retrieve data from</param>
    /// <param name="daysToRetrieve">Number of days to retrieve from the dataset. Range: 1 (only today) up to 30 (one month)</param>
    /// <param name="aggregateWindow">The time window for the aggregation of the measurements in seconds, minutes, hours or days.</param>
    /// <param name="sensor">Type of data to retrieve from the influx database</param>
    /// <param name="unit">The unit of representation that is linked to the <c>sensor</c> information</param>
    /// <param name="manufacturerMac">The MAC address of the manufacturer (Wifi Unit)</param>
    /// <returns></returns>
    private List<Measurement> QueryOdoMeterReadingForManufacturer(int meterId, int daysToRetrieve, string aggregateWindow, Sensor sensor, Unit unit, string manufacturerMac)
    {
        var measurements = new List<Measurement>();
        // A stopwatch is used so we can monitor the time it took to retrieve and process the data from the influx database
        var startTime = Stopwatch.StartNew();

        // This is an influx query. Influx processes this and returns the data based on the parameter you provide in the query
        string query =
            $"from(bucket: \"p1-smartmeters\")" +
            $"  |> range(start: {getStartDate(daysToRetrieve)}, stop: now())" +
            $"  |> filter(fn: (r) => r[\"_field\"] == \"{sensor}\")" +
            $"  |> filter(fn: (r) => r[\"signature\"] == \"{getFullMeterIdInHex(meterId, manufacturerMac)}\")" +
            $"  |> aggregateWindow(every: {aggregateWindow}, fn: min, createEmpty: false)" +
            $"  |> yield(name: \"min\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // in case of energy consumed or produced we will enrich the measurements with the power prices
        var energyPrices = new Dictionary<long, double>();
        if (Sensor.energy_consumed.Equals(sensor) || Sensor.energy_produced.Equals(sensor))
        {
            energyPrices = EnrichMeasurementsWithEnergyPrice(daysToRetrieve);
        }

        // enrich measurements with the temperature data available, independend of the type of sensor requested
        var temperatures = EnrichMeasurementsWithTemperature(daysToRetrieve, aggregateWindow);

        // Make an API Call to the Influx server to retrieve the data requested by the query
        var fluxTables = _client.GetQueryApi().QueryAsync(query).GetAwaiter().GetResult();

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTable. These records
        // are then converted to Measurement object and stored in the measurements list.
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                // Get the right fields we need from the record and store it in a local variable for later use
                DateTime dateTime = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc();
                string locationId = (string)record.GetValueByKey("signature");
                double value = (double)record.GetValueByKey("_value");

                // round up to the full hour so we can lookup the energy price for the hourly slot
                DateTime dateTimeRounded = dateTime.AddMinutes(dateTime.Minute * -1).AddSeconds(dateTime.Second * -1);

                // We use the earlier fetched data to set energyprice and temperature on the retrieved date/time of the measurement data
                energyPrices.TryGetValue(dateTimeRounded.Ticks, out var energyPrice);
                temperatures.TryGetValue(dateTime.Ticks, out var temperature);

                // Round the value up to the standarized rounding for measurements:
                // * gas will be rounded to three decimals and price will be set to a fixed value
                // * energy will be rounded to full numbers (no decimals)
                if (Sensor.gas_delivered.Equals(sensor))
                {
                    value = Math.Round(value, 3);
                    energyPrice = 1.376d;
                }
                else
                {
                    value = Math.Round(value, 0);
                }

                // Create a new Measurement object and add it to the list.
                var singleMeasurement = new Measurement
                {
                    Timestamp = dateTime,
                    LocationId = locationId,
                    Sensor = sensor,
                    Value = value,
                    Unit = unit,
                    EnergyPrice = energyPrice,
                    Temperature = temperature
                };

                measurements.Add(singleMeasurement);
            }
        }

        // Print the time it has taken to query and process the data before returning the list of measurements
        Console.WriteLine("Time consumed for API Call: " + startTime.ElapsedMilliseconds + "ms");
        return measurements;
    }

    /// <summary>
    /// Method to retrieve the temperature from the database. The temperature is published every hour and we will
    /// interpolate te values based on the value of AGGREGATE_WINDOW_TIME.
    ///
    /// The getStartDate is manipulated a bit to retrieve an extra day. Otherwise the measurement records will have no value for
    /// temperature for the first hour of the day (12:00AM till 1:00AM). Note: 12:00AM is considered to be part of the day before.
    /// </summary>
    private Dictionary<long, double> EnrichMeasurementsWithTemperature(int daysToRetrieve, string aggregateWindow)
    {
        var temperatures = new Dictionary<long, double>();
        string query =
                $"import \"interpolate\"" +
                $"from(bucket: \"ha-playground\")" +
                $"  |> range(start: {getStartDate(daysToRetrieve + 1)}, stop: now())" +
                $"  |> filter(fn: (r) => r[\"entity_id\"] == \"forecast_sendlab_playground\")" +
                $"  |> filter(fn: (r) => r[\"domain\"] == \"weather\")" +
                $"  |> filter(fn: (r) => r[\"_field\"] == \"temperature\")" +
                $"  |> interpolate.linear(every: {aggregateWindow})" +
                $"  |> aggregateWindow(every: {aggregateWindow}, fn: mean, createEmpty: false)" +
                $"  |> yield(name: \"mean\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTable. These records
        // are then converted to key (time) and value (temperature) pairs and stored in the energyPrices list.
        var fluxTables = _client.GetQueryApi().QueryAsync(query).GetAwaiter().GetResult();
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                long ticks = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc().Ticks;
                double temperature = (double)record.GetValueByKey("_value");
                temperatures.Add(ticks, Math.Round(temperature, 1));
            }
        }

        // return the list of key value pairs
        return temperatures;
    }

    /// <summary>
    /// Method to retrieve the energy prices from the database in hourly windows
    ///
    /// The getStartDate is manipulated a bit to retrieve an extra day. Otherwise the measurement records will have no value for
    /// temperature for the first hour of the day (12:00AM till 1:00AM). Note: 12:00AM is considered to be part of the day before.
    /// </summary>
    private Dictionary<long, double> EnrichMeasurementsWithEnergyPrice(int daysToRetrieve)
    {
        var energyPrices = new Dictionary<long, double>();
        string query =
                $"from(bucket: \"ha-playground\")" +
                $"  |> range(start: {getStartDate(daysToRetrieve + 1)}, stop: now())" +
                $"  |> filter(fn: (r) => r[\"entity_id\"] == \"nordpool\")" +
                $"  |> filter(fn: (r) => r[\"_field\"] == \"value\")" +
                $"  |> aggregateWindow(every: 60m, fn: mean, createEmpty: false)" + /// hardcoded aggregateWindow, since that is the price window (per hour)
                $"  |> yield(name: \"mean\")";

        // Enable or disable next line if you want to see or hide the query that is executed
        // Console.WriteLine(query);

        // Make an API Call to the Influx server to retrieve the data requested by the query. Loop over the results by
        // reading all fluxTables (most likely one) and process all records (results) in that fluxTables. These records
        // are then converted to key (time) and value (price) pairs and stored in the energyPrices list.
        var fluxTables = _client.GetQueryApi().QueryAsync(query).GetAwaiter().GetResult();
        foreach (var table in fluxTables)
        {
            foreach (var record in table.Records)
            {
                long ticks = ((NodaTime.Instant)record.GetValueByKey("_time")).ToDateTimeUtc().Ticks;
                double energyPrice = (double)record.GetValueByKey("_value");
                energyPrices.Add(ticks, Math.Round(energyPrice, 4));
            }
        }

        /// return the list of key value pairs
        return energyPrices;
    }

    /// <summary>
    /// A method to determine the earliest date based on the parameter lastNumberOfDaysToRetrieve
    /// </summary>
    private static string getStartDate(int lastNumberOfDaysToRetrieve)
    {
        // to prevent the database from overloading the maximum number of days will be 30
        lastNumberOfDaysToRetrieve = Math.Min(30, lastNumberOfDaysToRetrieve);

        // subtract 1 day to get the correct start date. From the user perspective it is more logical to
        // assume that one day is today, but the days to subtract should then be 0, thus the correction.
        // Also correct for the local timezone and subtract some seconds to get the record at 00:00:00.
        return DateTime.Now.ToUniversalTime().Date.AddDays((lastNumberOfDaysToRetrieve - 1) * -1).AddSeconds(-19).ToString("o");
    }

    /// <summary>
    /// Returns the full the Meter identifier (id) based on the provided meterId
    /// </summary>
    private string getFullMeterIdInHex(int meterId, string manufacturerMac)
    {
        return "2019-ETI-EMON-V01-" + meterId.ToString("X6") + "-" + manufacturerMac;
    }
}