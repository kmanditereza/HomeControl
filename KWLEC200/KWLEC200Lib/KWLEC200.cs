﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KWLEC200.cs" company="DTV-Online">
//   Copyright(c) 2017 Dr. Peter Trimmel. All rights reserved.
// </copyright>
// <license>
// Licensed under the MIT license. See the LICENSE file in the project root for more information.
// </license>
// --------------------------------------------------------------------------------------------------------------------
namespace KWLEC200Lib
{
    #region Using Directives

    using System;
    using System.Collections.Generic;

    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    using NModbusLib.Models;

    using BaseClassLib;
    using DataValueLib;
    using KWLEC200Lib.Models;
    using static DataValueLib.DataValue;
    using static KWLEC200Lib.Models.KWLEC200Data;
    using System.Globalization;

    #endregion

    /// <summary>
    /// Class holding data from the Helios KWL EC 200 unit.
    /// The data properties are based on the specification
    /// "Helios Ventilatoren Funktions- und Schnittstellenbeschreibung"
    /// NR. 82 269 - Modbus Gateway TCP/IP mit EasyControls. Druckschrift-Nr. 82269/07.14.
    /// </summary>
    public class KWLEC200 : BaseClass, IKWLEC200
    {
        #region Private Data Members

        /// <summary>
        /// The Modbus TCP/IP client instance.
        /// </summary>
        private readonly IHeliosClient _client;

        #endregion

        #region Public Properties

        /// <summary>
        /// The Data property holds all Modbus data values.
        /// </summary>
        public KWLEC200Data Data { get; set; } = new KWLEC200Data();

        /// <summary>
        /// The OperationData property holds a subset of the Modbus data values.
        /// </summary>
        [JsonIgnore]
        public OverviewData OverviewData { get; } = new OverviewData();

        /// <summary>
        /// Flag indicating that the first update has been sucessful.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets or sets the Modbus TCP/IP master options.
        /// </summary>
        public TcpMasterData Master
        {
            get => _client.TcpMaster;
            set => _client.TcpMaster = value;
        }

        /// <summary>
        /// Gets or sets the Modbus TCP/IP slave options.
        /// </summary>
        public TcpSlaveData Slave
        {
            get => _client.TcpSlave;
            set => _client.TcpSlave = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="KWLEC200"/> class.
        /// </summary>
        /// <param name="logger">The application logger instance.</param>
        /// <param name="client">The Helios TCP/IP client.</param>
        public KWLEC200(ILogger<KWLEC200> logger,
                        IHeliosClient client)
            : base(logger)
        {
            _client = client;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates all properties reading the data from Helios KWL EC 200 hvac.
        /// </summary>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus ReadAll()
        {
            DataStatus status = Good;

            try
            {
                if (_client.Connect())
                {
                    KWLEC200Data data = new KWLEC200Data();

                    foreach (var property in KWLEC200Data.GetProperties())
                    {
                        if (KWLEC200Data.IsReadable(property))
                        {
                            status = _client.ReadProperty(data, property);

                            if (!status.IsGood)
                            {
                                _logger?.LogDebug($"ReadAllAsync('{property}') not OK.");
                                break;
                            }
                        }
                    }

                    data.Status = status;

                    if (status.IsGood)
                    {
                        Data.Refresh(data);
                        OverviewData.Refresh(data);

                        if (IsInitialized == false)
                        {
                            IsInitialized = true;
                        }

                        _logger?.LogDebug("ReadAllAsync OK.");
                    }
                    else
                    {
                        _logger?.LogDebug("ReadAllAsync not OK.");
                    }
                }
                else
                {
                    _logger?.LogError("ReadAllAsync not connected.");
                    Data.Status = BadNotConnected;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"ReadAllAsync exception.");
                status = BadInternalError;
                status.Explanation = $"Exception: {ex.Message}";
            }
            finally
            {
                _client.Disconnect();
            }

            Data.Status = status;
            return status;
        }

        /// <summary>
        /// Updates the Helios KWL EC 200 hvac Overview Data.
        /// </summary>
        /// <returns></returns>
        public DataStatus ReadOverviewData()
        {
            var status = ReadData(OverviewData.GetProperties());

            if (status.IsGood)
            {
                OverviewData.Refresh(Data);
            }

            return status;
        }

        /// <summary>
        /// Updates a single property reading the data from Helios KWL EC 200 hvac.
        /// </summary>
        /// <param name="property">The name of the property.</param>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus ReadData(string property)
        {
            DataStatus status = Good;

            if (KWLEC200Data.IsProperty(property))
            {
                if (KWLEC200Data.IsReadable(property))
                {
                    try
                    {
                        if (_client.Connect())
                        {
                            status = _client.ReadProperty(Data, property);

                            if (status.IsGood)
                            {
                                _logger?.LogDebug($"ReadDataAsync('{property}') OK.");
                            }
                            else
                            {
                                _logger?.LogDebug($"ReadDataAsync('{property}') not OK.");
                            }
                        }
                        else
                        {
                            _logger?.LogError($"ReadDataAsync('{property}') not connected.");
                            status = BadNotConnected;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"ReadDataAsync('{property}') exception: {ex.Message}.");
                        status = BadInternalError;
                        status.Explanation = $"Exception: {ex.Message}";
                    }
                    finally
                    {
                        _client.Disconnect();
                    }
                }
                else
                {
                    _logger?.LogDebug($"ReadDataAsync('{property}') property not readable.");
                    status = BadNotReadable;
                    status.Explanation = $"Property '{property}' not readable.";
                }
            }
            else
            {
                _logger?.LogDebug($"ReadDataAsync('{property}') property not found.");
                status = BadNotFound;
                status.Explanation = $"Property '{property}' not found.";
            }

            return status;
        }

        /// <summary>
        /// Updates a list of properties reading the data from Helios KWL EC 200 hvac.
        /// </summary>
        /// <param name="properties">The list of the property names.</param>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus ReadData(List<string> properties)
        {
            DataStatus status = Good;

            try
            {
                if (_client.Connect())
                {
                    KWLEC200Data data = Data;
                    data.Status = Good;

                    foreach (var property in properties)
                    {
                        if (KWLEC200Data.IsProperty(property))
                        {
                            if (KWLEC200Data.IsReadable(property))
                            {
                                status = _client.ReadProperty(data, property);

                                if (status.IsGood)
                                {
                                    _logger?.LogDebug($"ReadDataAsync(List<property>) property '{property}' OK.");
                                }
                                else
                                {
                                    _logger?.LogDebug($"ReadDataAsync(List<property>) property '{property}' not OK.");
                                }
                            }
                            else
                            {
                                _logger?.LogDebug($"ReadDataAsync(List<property>) property '{property}' not readable.");
                                status = BadNotReadable;
                                status.Explanation = $"Property '{property}' not readable.";
                            }
                        }
                        else
                        {
                            _logger?.LogDebug($"ReadDataAsync(List<property>) property '{property}' not found.");
                            status = BadNotFound;
                            status.Explanation = $"Property '{property}' not found.";
                        }
                    }

                    Data = data;

                    if ((data.IsGood) && (status.IsGood))
                    {
                        _logger?.LogDebug("ReadDataAsync(List<property>) OK.");
                    }
                    else
                    {
                        _logger?.LogDebug("ReadDataAsync(List<property>) not OK.");
                    }
                }
                else
                {
                    _logger?.LogError("ReadDataAsync(List<property>) not connected.");
                    status = BadNotConnected;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"ReadDataAsync(List<property>) exception: {ex.Message}.");
                status = BadInternalError;
                status.Explanation = $"Exception: {ex.Message}";
            }
            finally
            {
                _client.Disconnect();
            }

            return status;
        }

        /// <summary>
        /// Updates a all properties writing the data to Helios KWL EC 200 hvac.
        /// </summary>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus WriteAll()
        {
            try
            {
                if (_client.Connect())
                {
                    foreach (var property in KWLEC200Data.GetProperties())
                    {
                        if (KWLEC200Data.IsWritable(property))
                        {
                            Data.Status = _client.WriteProperty(Data, property);
                        }
                    }

                    _logger?.LogDebug("WriteAllAsync OK.");
                }
                else
                {
                    _logger?.LogError("WriteAllAsync not connected.");
                    Data.Status = BadNotConnected;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"WriteAllAsync exception.");
                Data.Status = BadInternalError;
                Data.Status.Explanation = $"Exception: {ex.Message}";
            }
            finally
            {
                _client.Disconnect();
            }

            return Data.Status;
        }

        /// <summary>
        /// Updates a single data item at the Helios KWL EC 200 hvac
        /// writing the data value. Note that the property is updated.
        /// </summary>
        /// <param name="property">The name of the property.</param>
        /// <param name="data">The data value of the property.</param>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus WriteData(string property, string data)
        {
            DataStatus status = Good;

            if (KWLEC200Data.IsWritable(property))
            {
                try
                {
                    if (_client.Connect())
                    {
                        dynamic value = Data.GetPropertyValue(property);

                        switch (value)
                        {
                            case bool b when bool.TryParse(data, out bool boolData):
                                Data.SetPropertyValue(property, boolData);
                                status = WriteData(property);
                                break;
                            case int i when int.TryParse(data, out int intData):
                                Data.SetPropertyValue(property, intData);
                                status = WriteData(property);
                                break;
                            case double d when double.TryParse(data, out double doubleData):
                                Data.SetPropertyValue(property, doubleData);
                                status = WriteData(property);
                                break;
                            case DateTime d when DateTime.TryParse(data, new CultureInfo("de-DE"), DateTimeStyles.AssumeLocal, out DateTime dateData):
                                Data.SetPropertyValue(property, dateData);
                                status = WriteData(property);
                                break;
                            case TimeSpan t when TimeSpan.TryParse(data, out TimeSpan timeData):
                                Data.SetPropertyValue(property, timeData);
                                status = WriteData(property);
                                break;
                            case AutoSoftwareUpdates autosoftwareupdates when Enum.TryParse<AutoSoftwareUpdates>(data, true, out AutoSoftwareUpdates enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case ConfigOptions configoptions when Enum.TryParse<ConfigOptions>(data, true, out ConfigOptions enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case ContactTypes contacttypes when Enum.TryParse<ContactTypes>(data, true, out ContactTypes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case DateFormats dateformats when Enum.TryParse<DateFormats>(data, true, out DateFormats enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case DaylightSaving daylightsaving when Enum.TryParse<DaylightSaving>(data, true, out DaylightSaving enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case FanLevelConfig fanlevelconfig when Enum.TryParse<FanLevelConfig>(data, true, out FanLevelConfig enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case FanLevels fanlevels when Enum.TryParse<FanLevels>(data, true, out FanLevels enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case FaultTypes faulttypes when Enum.TryParse<FaultTypes>(data, true, out FaultTypes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case FunctionTypes functiontypes when Enum.TryParse<FunctionTypes>(data, true, out FunctionTypes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case GlobalUpdates globalupdates when Enum.TryParse<GlobalUpdates>(data, true, out GlobalUpdates enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case HeatExchangerTypes heatexchangertypes when Enum.TryParse<HeatExchangerTypes>(data, true, out HeatExchangerTypes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case HeliosPortalAccess heliosportalaccess when Enum.TryParse<HeliosPortalAccess>(data, true, out HeliosPortalAccess enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case KwlFTFConfig kwlftfconfig when Enum.TryParse<KwlFTFConfig>(data, true, out KwlFTFConfig enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case KwlSensorConfig kwlsensorconfig when Enum.TryParse<KwlSensorConfig>(data, true, out KwlSensorConfig enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case MinimumFanLevels minimumfanlevels when Enum.TryParse<MinimumFanLevels>(data, true, out MinimumFanLevels enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case OperationModes operationmodes when Enum.TryParse<OperationModes>(data, true, out OperationModes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case PreheaterTypes preheatertypes when Enum.TryParse<PreheaterTypes>(data, true, out PreheaterTypes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case SensorStatus sensorstatus when Enum.TryParse<SensorStatus>(data, true, out SensorStatus enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case StatusTypes statustypes when Enum.TryParse<StatusTypes>(data, true, out StatusTypes enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case VacationOperations vacationoperations when Enum.TryParse<VacationOperations>(data, true, out VacationOperations enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case WeeklyProfiles weeklyprofiles when Enum.TryParse<WeeklyProfiles>(data, true, out WeeklyProfiles enumData):
                                Data.SetPropertyValue(property, enumData);
                                status = WriteData(property);
                                break;
                            case string s:
                                Data.SetPropertyValue(property, data);
                                status = WriteData(property);
                                break;
                            default:
                                _logger?.LogDebug($"WriteAsync {data} to '{property}' not OK.");
                                status = BadEncodingError;
                                break;
                        }

                        if (status.IsGood)
                        {
                            _logger?.LogDebug($"WriteDataAsync {data} to '{property}' OK.");
                        }
                        else
                        {
                            _logger?.LogDebug($"WriteDataAsync {data} to '{property}' not OK.");
                        }
                    }
                    else
                    {
                        _logger?.LogError("WriteDataAsync not connected.");
                        Data.Status = BadNotConnected;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"WriteDataAsync exception: {ex.Message}.");
                    status = BadInternalError;
                    status.Explanation = $"Exception: {ex.Message}";
                }
                finally
                {
                    _client.Disconnect();
                }
            }
            else
            {
                _logger?.LogDebug($"WriteDataAsync invalid property '{property}'.");
                status = BadNotWritable;
                status.Explanation = $"Property '{property}' not writable.";
            }

            return status;
        }

        /// <summary>
        /// Updates a single property writing the data to Helios KWL EC 200 hvac.
        /// </summary>
        /// <param name="property">The name of the property.</param>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus WriteData(string property)
        {
            DataStatus status = Good;

            if (KWLEC200Data.IsWritable(property))
            {
                try
                {
                    if (_client.Connect())
                    {
                        status = _client.WriteProperty(Data, property);

                        if (status.IsGood)
                        {
                            _logger?.LogDebug($"WriteData '{property}' OK.");
                        }
                        else
                        {
                            _logger?.LogDebug($"WriteData '{property}' not OK.");
                        }
                    }
                    else
                    {
                        _logger?.LogError("WriteData not connected.");
                        status = BadNotConnected;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"WriteAsync exception: {ex.Message}.");
                    status = BadInternalError;
                    status.Explanation = $"Exception: {ex.Message}";
                }
                finally
                {
                    _client.Disconnect();
                }
            }
            else
            {
                _logger?.LogDebug($"WriteData invalid property '{property}'.");
                status = BadNotWritable;
                status.Explanation = $"Property '{property}' not writable.";
            }

            return status;
        }

        /// <summary>
        /// Updates a list of properties from Helios KWL EC 200 hvac writing the property values.
        /// </summary>
        /// <param name="properties">The list of the property names.</param>
        /// <returns>The status indicating success or failure.</returns>
        public DataStatus WriteData(List<string> properties)
        {
            DataStatus status = Good;

            try
            {
                if (_client.Connect())
                {
                    KWLEC200Data data = Data;
                    data.Status = Good;

                    foreach (var property in properties)
                    {
                        if (KWLEC200Data.IsProperty(property))
                        {
                            if (KWLEC200Data.IsWritable(property))
                            {
                                status = _client.WriteProperty(Data, property);

                                if (status.IsGood)
                                {
                                    _logger?.LogDebug($"WriteData(List<property>) to '{property}' OK.");
                                }
                                else
                                {
                                    _logger?.LogDebug($"WriteData(List<property>) to '{property}' not OK.");
                                }
                            }
                            else
                            {
                                _logger?.LogDebug($"WriteData(List<property>) property '{property}' not writeable.");
                                status = BadNotReadable;
                            }
                        }
                        else
                        {
                            _logger?.LogDebug($"WriteData(List<property>) property '{property}' not found.");
                            status = BadNotFound;
                        }
                    }

                    if ((data.IsGood) && (status.IsGood))
                    {
                        _logger?.LogDebug("WriteData(List<property>) OK.");
                    }
                    else
                    {
                        _logger?.LogDebug("WriteData(List<property>) not OK.");
                    }
                }
                else
                {
                    _logger?.LogError("WriteData(List<property>) not connected.");
                    status = BadNotConnected;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"WriteData(List<property>) exception: {ex.Message}.");
                status = BadInternalError;
                status.Explanation = $"Exception: {ex.Message}";
            }
            finally
            {
                _client.Disconnect();
            }

            Data.Status = status;
            return status;
        }

        /// <summary>
        /// Tries to connect to the Modbus TCP/IP slave.
        /// </summary>
        /// <returns>The boolean flag indicating success or failure.</returns>
        public bool Connect()
        {
            try
            {
                if (_client.Connect())
                {
                    _logger?.LogDebug($"ConnectAsync OK.");
                    return true;
                }
                else
                {
                    _logger?.LogDebug($"ConnectAsync not OK.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ConnectAsync exception.");
                Data.Status = DataValue.BadInternalError;
                Data.Status.Explanation = $"Exception: {ex.Message}";
            }

            return false;
        }

        /// <summary>
        /// Method to determine if the property is supported. Switches to the proper section.
        /// Note this routine supports nested properties and simple arrays and generic List (IList).
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <returns>True if the property is found.</returns>
        public static bool IsProperty(string property)
        {
            if (!string.IsNullOrEmpty(property))
            {
                string[] parts = property.Split(new[] { '.' }, 2);

                switch (parts[0])
                {
                    case "KWLEC200":
                        return parts.Length > 1 ? PropertyValue.GetPropertyInfo(typeof(KWLEC200), parts[1]) != null : true;
                    case "Data":
                        return parts.Length > 1 ? PropertyValue.GetPropertyInfo(typeof(KWLEC200Data), parts[1]) != null : true;
                    case "OverviewData":
                        return parts.Length > 1 ? PropertyValue.GetPropertyInfo(typeof(OverviewData), parts[1]) != null : true;
                    default:
                        return PropertyValue.GetPropertyInfo(typeof(KWLEC200Data), property) != null;
                }
            }

            return false;
        }

        /// <summary>
        /// Method to get the property value by name. Switches to the proper section.
        /// Note this routine supports nested properties and simple arrays and generic List (IList).
        /// </summary>
        /// <param name="property">The property name.</param>
        /// <returns>The property value.</returns>
        public object GetPropertyValue(string property)
        {
            if (!string.IsNullOrEmpty(property))
            {
                string[] parts = property.Split(new[] { '.' }, 2);

                switch (parts[0])
                {
                    case "KWLEC200":
                        return parts.Length > 1 ? PropertyValue.GetPropertyValue(this, parts[1]) : this;
                    case "Data":
                        return parts.Length > 1 ? PropertyValue.GetPropertyValue(Data, parts[1]) : Data;
                    case "OverviewData":
                        return parts.Length > 1 ? PropertyValue.GetPropertyValue(OverviewData, parts[1]) : OverviewData;
                    default:
                        return PropertyValue.GetPropertyValue(Data, property);
                }
            }

            return null;
        }

        #endregion Public Methods
    }
}