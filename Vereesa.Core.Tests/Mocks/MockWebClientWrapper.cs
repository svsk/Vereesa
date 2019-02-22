using System;
using System.Collections.Generic;
using Vereesa.Core.Integrations.Interfaces;

namespace Vereesa.Core.Tests.Mocks
{
    public class MockWebClientWrapper : IWebClientWrapper
    {
        private Random _random;

        private Dictionary<string, string> _urlToContent;

        public MockWebClientWrapper()
        {
            _random = new Random();
            _urlToContent = new Dictionary<string, string>();
        }

        public string DownloadString(string url)
        {
            if (_urlToContent.ContainsKey(url))
            {
                return _urlToContent[url];
            }

            return "<div></div>";
        }

        public void UpsertUrlContent(string url, string contentToReturn)
        {
            if (_urlToContent.ContainsKey(url))
                _urlToContent[url] = contentToReturn;
            else
                _urlToContent.Add(url, contentToReturn);
        }
    }
}