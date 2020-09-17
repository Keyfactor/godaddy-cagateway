using CAProxy.AnyGateway.Interfaces;
using CAProxy.AnyGateway.Models;
using CAProxy.Common;
using CSS.Common.Logging;
using CSS.PKI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CAProxy.Generic.GoDaddy
{
    public class GoDaddyCAProxy : LoggingClientBase, ICAConnector
    {
        public void Synchronize(ICertificateDataReader certificateDataReader, BlockingCollection<CertificateRecord> blockingBuffer, CertificateAuthoritySyncInfo certificateAuthoritySyncInfo, CancellationToken cancelToken, string logicalName)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            //TODO: Update Blocking Collection with Go Daddy Certificate Response Model
            BlockingCollection<string> certsFromGoDaddy = new BlockingCollection<string>(100);

            //TODO: Lee create GoDaddy Client to Async populate the blocking collection
            //Task.Run(async() => await MethodToPopulateBc(bc,cancelToken);
            MockUpClient client = new MockUpClient();
            
            Task readCertsFromCA = Task.Run(() => MockUpClient.PopulateCertificateList(certsFromGoDaddy, cancelToken));

            //TODO: Update cert variable with Go Daddy Certificate Response Model
            string cert;
            while (!certsFromGoDaddy.IsCompleted)
            {
                if (readCertsFromCA.IsFaulted)
                {
                    throw readCertsFromCA.Exception.Flatten();
                }

                //Process Certs. Task could be a long running task depending on CA Cert Count. Begin taking certs from queue being populated by task
                //TODO: Ensure out parametere matches Go Daddy Certificate model
                if (certsFromGoDaddy.TryTake(out cert, 10,cancelToken))
                {
                    blockingBuffer.Add(new CertificateRecord(new CAConnectorCertificate { 
                        CARequestID="",
                        Certificate="",
                        CSR="",
                        ResolutionDate=null,
                        RevocationDate=null,
                        RevocationReason=null,
                        Status=20,
                        SubmissionDate=DateTime.Now
                    }));
                }
            }
            blockingBuffer.CompleteAdding();

            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
        }

        public EnrollmentResult Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, PKIConstants.X509.RequestFormat requestFormat, RequestUtilities.EnrollmentType enrollmentType)
        {
            Logger.MethodEntry(ILogExtensions.MethodLogLevel.Debug);
            //productInfo.ProductID; //This is the mapped product ID from the AnyGateway Template Mapping
            //productInfo.ProductParameters;// this is a collection of string key/value pairs from the Template mapping. Can be used to determine required fields?

            //TODO: Create new Enrollment object for Go Daddy based on template configuration. Send request to correct endpoint based on enrollmentType
            
            switch (enrollmentType)
            {
                case RequestUtilities.EnrollmentType.New:
                    break;
                case RequestUtilities.EnrollmentType.Renew:
                    break;
                case RequestUtilities.EnrollmentType.Reissue:
                    break;
            }
            Logger.MethodExit(ILogExtensions.MethodLogLevel.Debug);
            return new EnrollmentResult { 
                CARequestID="",
                Certificate="",
                Status=20,
                StatusMessage=""
            };
            
        }

        public CAConnectorCertificate GetSingleRecord(string caRequestID)
        {
            throw new NotImplementedException();
        }

        public void Initialize(ICAConnectorConfigProvider configProvider)
        {
            throw new NotImplementedException();
        }

        public int Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
        {
            throw new NotImplementedException();
        }

        public void Ping()
        {
            throw new NotImplementedException();
        }

        public void ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
        {
            throw new NotImplementedException();
        }

        public void ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
        {
            throw new NotImplementedException();
        }

        #region Not Implemented

        public EnrollmentResult Approve(string caRequestID)
        {
            throw new NotImplementedException();
        }

        public void Deny(string caRequestID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


    public class MockUpClient
    {
        //Mock up of what it looks like to populate the blocking collection from a task
        public static void PopulateCertificateList(BlockingCollection<string> bc, CancellationToken ct)
        {
            string certModel;
            int itemsProcessed = 0;
            int totalItems = 100; //TODO: Get from Response
            do
            { 
                try
                {
                    certModel = "Parsed from JSON Response";

                    if (bc.TryAdd(certModel, 10, ct))
                    {
                        itemsProcessed++;   
                    }
                    else
                    {
                        //adding to the queue was blocked.  Try again
                    }
                }
                catch (OperationCanceledException)
                {
                    //Operation was canceld
                    bc.CompleteAdding();
                    break;
                }
            } while (itemsProcessed < totalItems);
            bc.CompleteAdding();
        }
    }

}
