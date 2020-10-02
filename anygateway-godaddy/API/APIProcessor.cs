using Newtonsoft.Json;
using RestSharp;
using GoDaddy.Models;

namespace GoDaddy
{
    class APIProcessor
    {
        private string URL;
        private string AUTH;

        public APIProcessor(string url, string auth)
        {
            URL = url;
            AUTH = auth;
        }

        //private string GetDomainByName(string domainName)
        //{
            //string API = "/v1/domains/available?";

            //HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL + API + domainName);
            //request.Headers.Add("Authorization", AUTH);

            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //StreamReader reader = new StreamReader(response.GetResponseStream());
            //return reader.ReadToEnd();
        //}

        public string EnrollCSR(string csr, string cn)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = "v1/certificates";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.POST);
            request.AddHeader("Authorization", AUTH);

            POSTCertificatesV1DV body = new POSTCertificatesV1DV();

            //body.commonName = cn;
            body.csr = csr;

            body.contact = new ContactInfo();
            body.contact.email = "lee.fine@keyfactor.com";
            body.contact.nameFirst = "bob";
            body.contact.nameLast = "smith";
            body.contact.phone = "555 555 5555";

            body.period = 1;

            request.AddJsonBody(body);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }


        public string ValidateCSR(string csr)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = "v1/certificates/validate";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.POST);
            request.AddHeader("Authorization", AUTH);

            POSTCertificatesV1DV body = new POSTCertificatesV1DV();

            body.csr = csr;

            body.contact = new ContactInfo();
            body.contact.email = "lee.fine@keyfactor.com";
            body.contact.nameFirst = "bob";
            body.contact.nameLast = "smith";
            body.contact.phone = "555 555 5555";

            body.period = 2;

            request.AddJsonBody(body);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }

        public string GetCertificates(string customerId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v2/customers/{customerId}/certificates";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.GET);
            request.AddHeader("Authorization", AUTH);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }


        public string GetCertificate(string certificateId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.GET);
            request.AddHeader("Authorization", AUTH);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }

        public string DownloadCertificate(string certificateId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/download";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.GET);
            request.AddHeader("Authorization", AUTH);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }


        public string RevokeCertificate(string certificateId, POSTCertificateRevoke.REASON reason)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/revoke";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.POST);
            request.AddHeader("Authorization", AUTH);

            POSTCertificateRevoke body = new POSTCertificateRevoke();
            body.reason = reason.ToString();

            request.AddJsonBody(body);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }





















        public string GetCertificateActions(string certificateId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/certificates/{certificateId}/actions";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.GET);
            request.AddHeader("Authorization", AUTH);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }

        public string GetShopper(string shopperId)
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/shoppers/{shopperId}?includes=customerId";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.GET);
            request.AddHeader("Authorization", AUTH);

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {   
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }


        public string GetDomains()
        {
            string rtnMessage = string.Empty;

            string RESOURCE = $"v1/domains";

            RestClient client = new RestClient(URL);
            RestRequest request = new RestRequest(RESOURCE, Method.GET);
            request.AddHeader("Authorization", AUTH);
            request.AddHeader("X-Shopper-Id", "305145420");

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                try
                {
                    APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                    rtnMessage = $"{error.code}: {error.message}";
                }
                catch (JsonReaderException ex)
                {
                    rtnMessage = response.Content;
                }
            }

            return response.Content;
        }
    }
}
