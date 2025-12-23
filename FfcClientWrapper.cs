using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ffcextensions
{
    public class FfcClientWrapper
    {
        public string Name { get; set; }              // Friendly name for logging
        public string FfcUri { get; set; }            // Base URI
        public string FfcUsername { get; set; }       // Auth username
        public string FfcPassword { get; set; }       // Auth password
        public HttpClient HttpClient { get; set; }    // HTTP client
        public string AccessToken { get; set; }       // Cached Bearer token
        public DateTime AccessTokenExpires { get; set; }

        public int Weight { get; set; } = 1;          // For weighted balancing
        public bool IsHealthy { get; set; } = true;   // Endpoint health
        public DateTime LastFailure { get; set; }     // When it last failed
    }

}
