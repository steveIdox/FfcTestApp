using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using Aspose.Pdf.Operators;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace idox.eim.fusionp8
{
    public class FfcApiOperations
    {
        private static HttpClient _httpClient;
        private const string AccessTokenKey = "access_token";
        private const string BearerScheme = "Bearer";
        private const string FfcJobsEndpoint = "/api/ffc-jobs/";


        /// <summary>
        /// Create an Access Token allowing FFC Api access
        /// Access Tokens allow access to further methods within the FfcApi
        /// Username and password here are the same as those used to access FFC Web Admin Area
        /// </summary>
        /// <param name="webApiUrl">The URL to for the installed Fusion File Convertor</param>
        /// <param name="username">The username allowing acces to FfcApi requests</param>
        /// <param name="password">The password the user</param>
        /// <returns>An Access Token if successful</returns>
        public static string RequestAccessToken(string ffcApiUrl, string username, string password)
        {
            IEnumerable<KeyValuePair<string, string>> tokenRequestObject = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            };

            var tokenRequestContent = new FormUrlEncodedContent(tokenRequestObject);
            Uri tokenUri = new Uri(ffcApiUrl + "/token");
            if (_httpClient == null)
            {
                _httpClient = new HttpClient { BaseAddress = new Uri(ffcApiUrl) };
            }
            HttpResponseMessage httpResponseMessage =
                _httpClient.PostAsync(tokenUri, tokenRequestContent).Result;

            string responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
            string temp = responseContent.Trim('{');

            temp = responseContent.Trim('}');

            List<string> list = new List<string>(temp.Split(','));
            string part = list[0].Split(':')[1];
            string connectionId = part.Trim('\"');

            return connectionId;
        }

        /// <summary>
        /// Submit a job to FFC
        ///A request token is required to perform operations via the FfcApi
        ///</summary>
        /// <param name="ffcApiUrl">The URL to for the installed Fusion File Convertor Api</param>
        /// <param name="token">A Request Token previously obtained from the RequestToken method</param>
        /// <param name="jobTicketPath">Path to job ticket file</param>
        /// <param name="queueName">{Optional) Queue name to post the new job too</param>
        /// <returns>A job id if the submissions is successful</returns>
        public static string SubmitJobTicket(string ffcApiUrl, string token, string jobTicketPath, string queueName)
        {
            if (String.IsNullOrEmpty(token)) return String.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

            string query = $"jobTicketPath={jobTicketPath}";
            if (!String.IsNullOrEmpty(queueName)) query += $"&queueName={queueName}";

            UriBuilder uriBuilder = new UriBuilder(ffcApiUrl + FfcJobsEndpoint)
            {
                Query = query
            };

            var requestData = new StringContent(System.IO.File.ReadAllText(jobTicketPath),
                Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponseMessage = _httpClient.PostAsync(uriBuilder.Uri, requestData).Result;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var fileConverterJob = httpResponseMessage.Content.ReadAsStringAsync().Result;
                return fileConverterJob;
            }
            else
            {
                return httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
        }

        /// <summary>
        /// Get the state of a job previously submitted to FFC.
        /// A request token is required to perform operations via the FfcApi
        /// </summary>
        /// <param name="ffcApiUrl">The URL to for the installed Fusion File Convertor Api</param>
        /// <param name="token">A Request Token previously obtained from the RequestToken method</param>
        /// <param name="jobId">Job Id for a previously submitted job</param>
        /// <returns>Current state of the job</returns>
        public static string GetFileConverterJob(string ffcApiUrl, string token, string jobId)
        {
            if (String.IsNullOrEmpty(token)) return String.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

            UriBuilder uriBuilder = new UriBuilder(ffcApiUrl + FfcJobsEndpoint + jobId);
            HttpResponseMessage httpResponseMessage = _httpClient.GetAsync(uriBuilder.Uri).Result;
            var fileConverterJob = httpResponseMessage.Content.ReadAsStringAsync().Result;
            JObject jsonObject = JObject.Parse(fileConverterJob);
            string status = jsonObject["ConversionStatus"].ToString();
            //string status = idox.eim.fusionp8.JsonHelper.GetJsonProperty(fileConverterJob, "ConversionStatus");
            if (status.ToUpper().Equals("Completed"))
            {
                string output = JsonHelper.GetJsonProperty(fileConverterJob, "OutputFile");
                return output;
            }
            if (status.ToUpper().Equals("Failed"))
            {
                string error = JsonHelper.GetJsonProperty(fileConverterJob, "ErrorDescription");
                return error;
            }

            return String.Empty;
        }

        public static FileConverterJobReport GetFileConverterJobReport(string ffcApiUrl, string token, string jobId)
        {
            if(String.IsNullOrEmpty(token)) return new FileConverterJobReport();
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

            UriBuilder uriBuilder = new UriBuilder(ffcApiUrl + FfcJobsEndpoint + jobId);
            HttpResponseMessage httpResponseMessage = _httpClient.GetAsync(uriBuilder.Uri).Result;

            var json = httpResponseMessage.Content.ReadAsStringAsync().Result;
            var fileConverterJob  = JsonConvert.DeserializeObject<FileConverterJobReport>(json);
            return fileConverterJob;
        }

        public static JobFilesPaths GetFileConverterJobFiles(int iSubmitCount, string ffcApiUrl, string token, string jobId)
        {
            if (String.IsNullOrEmpty(token)) return new JobFilesPaths();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

            var getJobFilesUri = new Uri(ffcApiUrl + FfcJobsEndpoint + jobId + "/filepaths");
            HttpResponseMessage httpResponseMessage = _httpClient.GetAsync(getJobFilesUri).Result;
            var json = httpResponseMessage.Content.ReadAsStringAsync().Result;
            var fileConverterJob = JsonConvert.DeserializeObject<JobFilesPaths>(json);

            return fileConverterJob;
        }


        /// <summary>
        /// Deletes a job previously submitted to FFC.
        /// A request token is required to perform operations via the FfcApi
        /// </summary>
        /// <param name="ffcApiUrl">The URL to for the installed Fusion File Convertor Api</param>
        /// <param name="token">A Request Token previously obtained from the RequestToken method</param>
        /// <param name="jobId">Job Id for a previously submitted job</param>
        /// <param name="deleteInputFiles">Deletes the input files used during the job
        /// <param name="deleteJobTicket">Deletes the job ticket used during the job</param>
        /// <returns>Current state of the job</returns>
        public static void DeleteConverterJob(string ffcApiUrl, string token, string jobId, 
            bool deleteInputFiles, bool deleteJobTicket)
        {
            if (String.IsNullOrEmpty(token)) return;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

            UriBuilder uriBuilder = new UriBuilder(ffcApiUrl + FfcJobsEndpoint + jobId)
            {
                Query = $"deleteInputFiles={deleteInputFiles}&deleteJobTicket={deleteJobTicket}"
            };
            

            HttpResponseMessage httpResponseMessage = _httpClient.GetAsync(uriBuilder.Uri).Result;
            var fileConverterJob = httpResponseMessage.Content.ReadAsStringAsync().Result;

            return;
        }

        public static async Task<string> WaitForConvertorJobAsync(
            int submitCount,
            string ffcApiUrl,
            string token,
            string jobId,
            int timeout,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Token cannot be null");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(BearerScheme, token);

            var uri = new Uri(ffcApiUrl + FfcJobsEndpoint + jobId);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (true)
            {
                //if (sw.ElapsedMilliseconds > timeout)
                //    throw new TimeoutException(
                //        $"Job {jobId} did not complete within {timeout} ms."
                //    );

                HttpResponseMessage httpResponseMessage;
                string content;

                try
                {
                    httpResponseMessage = await _httpClient.GetAsync(uri, ct);
                    content = await httpResponseMessage.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    // transient — retry polling
                    Console.WriteLine($"Transient error for job {jobId}: {ex.Message}");
                    await Task.Delay(500, ct);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine($"Empty response for job {jobId}, retrying...");
                    await Task.Delay(500, ct);
                    continue;
                }

                JObject obj;

                try
                {
                    obj = JObject.Parse(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid JSON for job {jobId}: {ex.Message}");
                    await Task.Delay(500, ct);
                    continue;
                }

                string status = obj["ConversionStatus"]?.ToString()?.ToUpperInvariant();

                if (status == "CONVERTED")
                    return content;                     // JSON of success

                if (status == "FAILED")
                    return content;                     // JSON of failure (but structured)

                await Task.Delay(250, ct);              // Poll interval
            }
        }

    }
}
