using System;
using System.Net;
using Vereesa.Core.Integrations.Interfaces;

namespace Vereesa.Core.Integrations 
{
    public class WebClientWrapper : IWebClientWrapper
    {
        public string DownloadString(string address)
        {
            using (var client = new WebClient()) 
            {
                return client.DownloadString(address);
            }   
        }
    }
}