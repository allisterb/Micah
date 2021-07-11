using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Google.Apis.Auth.OAuth2;

namespace Micah.FHIR
{
    public class GoogleHCFHIR3Client : FHIR3Client
    {
        #region Constructors
        static GoogleHCFHIR3Client()
        {
            Info("Google HealthCare API project id is {0}.", Config("GOOGLE_PROJECT_ID"));
            Info("Using service account data from {0}.", Config("GOOGLE_APPLICATION_CREDENTIALS"));
            using (var stream = File.OpenRead(Config("GOOGLE_APPLICATION_CREDENTIALS")))
            {
                Credential = GoogleCredential.FromStream(stream).CreateScoped("https://www.googleapis.com/auth/cloud-platform").UnderlyingCredential;
            }
        }

        public GoogleHCFHIR3Client(): base("https://healthcare.googleapis.com/v1/projects/seismic-bonfire-319022/locations/us-central1/datasets/Micah/fhirStores/1/fhir/")
        {
            if (Client.RequestHeaders.Contains("Authorization"))
            {
                Client.RequestHeaders.Remove("Authorization");
            }
        }
        #endregion

        #region Methods
        protected async override Task PrepClient()
        {
            ThrowIfNotInitialized();
            if (Client.RequestHeaders.Contains("Authorization"))
            {
                Client.RequestHeaders.Remove("Authorization");
            }
            var token = await Credential.GetAccessTokenForRequestAsync();
            Client.RequestHeaders.Add("Authorization", "Bearer " + token);
        }
        #endregion

        #region Fields
        private static ICredential Credential;
        #endregion
    }
}
