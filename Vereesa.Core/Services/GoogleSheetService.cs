using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Vereesa.Core.Configuration;
using Vereesa.Data.Models.EventHub;

namespace Vereesa.Core.Services
{
    public class GoogleSheetService
    {
        private ILogger<GoogleSheetService> _logger;
        private EventHubService _eventHubService;
        private GoogleSheetSettings _settings;
        private Timer _checkInterval;
        private int _previousRowCount;

        public GoogleSheetService(GoogleSheetSettings settings, EventHubService eventHubService, ILogger<GoogleSheetService> logger)
        {
            _logger = logger;
            _eventHubService = eventHubService;
            _settings = settings;
            _previousRowCount = -1;
            Start();
        }

        private void Start()
        {
            if (_checkInterval != null)
                _checkInterval.Stop();

            _checkInterval = new Timer();
            _checkInterval.Interval = _settings.CheckIntervalSeconds * 1000;
            _checkInterval.AutoReset = true;
            _checkInterval.Elapsed += (object sender, ElapsedEventArgs e) => { ReadSheet(); };
            _checkInterval.Start();
            ReadSheet();
        }

        private async void ReadSheet()
        {
            var rows = new List<string>();
            HttpResponseMessage response = null;

            using (var client = new HttpClient())
            {
                try
                {
                    response = await client.GetAsync(_settings.GoogleSheetCsvUrl);
                }
                catch (TaskCanceledException)
                {
                    //aw well
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message, ex);
                }
            }

            if (response != null && response.StatusCode == HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                rows = responseContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

                //uncomment this to test with latest application made
                //_previousRowCount--;

                if (_previousRowCount != -1 && rows.Count > _previousRowCount)
                {
                    foreach (var row in rows.Skip(_previousRowCount))
                    {
                        _eventHubService.Emit(EventHubEvents.NewCsvRow, row);
                    }
                }

                _previousRowCount = rows.Count;
            }
        }
    }
}