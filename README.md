# GoDaddy CA Gateway

GoDaddy is a domain registrar, web hosting company, and most relevant here, a public certificate authority.  The GoDaddy AnyGateway is designed to allow Keyfactor Command the ability to - Sync certificates Issued from the CA - Request new certificates from the CA - Revoke certificates directly from Keyfactor Command - Certificate Reissue/Renewal

#### Integration status: Production - Ready for use in production environments.

## About the Keyfactor AnyGateway CA Connector

This repository contains an AnyGateway CA Connector, which is a plugin to the Keyfactor AnyGateway. AnyGateway CA Connectors allow Keyfactor Command to be used for inventory, issuance, and revocation of certificates from a third-party certificate authority.




---








---


*** 

# GoDaddy Supported Certificate Types
GoDaddy supports the following certificate products:
- Domain Validated SSL (DV_SSL)
- Domain Validated Wildcard SSL (DV_WILDCARD_SSL)
- Domain Validated SSL With SANs (UCC_DV_SSL)
- Organization Validated SSL (OV_SSL)
- Organization Validated Wildcard SSL (OV_WILDCARD_SSL)
- Organization Validated SSL With SANs (UCC_OV_SSL)
- Organization Validated Code Signing (OV_CS)  **NOTE: GoDaddy is no longer selling new credits for this product type**
- Organization Validated Driver Signing (OV_DS)  **NOTE: GoDaddy is no longer selling new credits for this product type**
- Extended Validation SSL (EV_SSL)
- Extended Validation SSL With SANs (UCC_EV_SSL)


# GoDaddy Resources

- [GoDaddy Portal](https://ae.godaddy.com/)
- [GoDaddy API Guide](https://developer.godaddy.com/doc)
- [Create GoDaddy SSOKey](https://developer.godaddy.com/keys?hbi_code=1)



# Getting Started
### Prerequsites
To begin, you must have the AnyGateway Service installed and operational before attempting to configure the GoDaddy AnyGateway plugin.

A production GoDaddy account must be set up that will be associated with the gateway and an API Key/Secret created.  For more information on how to create an API Key, follow the instructions [here](https://developer.godaddy.com/keys).

For enrollment, make sure you have pre-purchased enough certificates of the type you will be enrolling before attempting to enroll certificates via this gateway.  The gateway itself does not purchase certificates and requires that the product (certificate) be pre-purchased for the gateway to enroll it successfully.  The certificate may be purchased using any payment method including but not limited to GoDaddy's Good as Gold or in store credits, but just having these funds available is not enough.  The product MUST actually be pre-purchased using an available payment method.


### Installation and Configuration
##### Step 1 - Install the GoDaddy root and intermediate certificates.
There are four CA certificate chains that are supported by GoDaddy that can be used in the GoDaddy AnyGateway.  For each of these CA chains that are to be supported by the local installation of the GoDaddy AnyGateway, the root and intermediate certificates must be installed in the Intermediate Certification Authorities store on the AnyGateway server and the root certificate must be installed in the Trusted Root Certification Authorities store on the AnyGateway server.
- GoDaddy SHA-1 (GODADDY_SHA_1)
  - [Root Certificate](https://certs.godaddy.com/repository/gd-class2-root.crt) 
  - [Intermediate Certificate](https://certs.godaddy.com/repository/gd_intermediate.crt.pem)
- GoDaddy SHA256 (GODADDY_SHA_2)
  - [Root Certificate](https://certs.godaddy.com/repository/gdroot-g2.crt) 
  - [Intermediate Certificate](https://certs.godaddy.com/repository/gdig2.crt.pem)
- Starfield SHA-1 (STARFIELD_SHA_1)
  - [Root Certificate](https://certs.godaddy.com/repository/sf-class2-root.crt) 
  - [Intermediate Certificate](https://certs.godaddy.com/repository/sf_intermediate.crt.pem)
- Starfield SHA256 (STARFIELD_SHA_2)
  - [Root Certificate](https://certs.godaddy.com/repository/sfroot-g2.crt) 
  - [Intermediate Certificate](https://certs.godaddy.com/repository/sfig2.crt.pem)


##### Step 2 - Create Templates in Active Directory
For each GoDaddy product being supported, you must create a Certificate Template on the Keyfactor Command server.  Make note of the template name of each, as it will be referenced in a future step.


##### Step 3 - Stop the Keyfactor AnyGateway service


##### Step 4 - Install the GoDaddy AnyGateway binaries
Once the AnyGateway configuration has been imported, the GoDaddy AnyGateway binaries need to be placed in the Keyfactor AnyGateway Service install directory 
(C:\\Program Files\\Keyfactor\\Keyfactor AnyGateway for default installations). 
The following binaries need to be placed in the install directory:
GoDaddyCAProxy.dll
RestSharp.dll

##### Step 5 - Modify the CAProxyServer.exe.config file
Edit the CAProxyServer.exe.config file in the Keyfactor Gateway installation folder.  Modify the "unity" section "<alias alias=CAConnector" line to read: *\<alias alias="CAConnector" type="**Keyfactor.AnyGateway.GoDaddy.GoDaddyCAProxy, GoDaddyCAProxy**"/\>*

Once this is done, restart the Keyfactor AnyGateway service.


##### Step 6 - Modify the AnyGatewayConfig.json file
After installing the Keyfactor AnyGateway service (see Prerequisites), there should be a AnyGatewayConfig.json file located in your root c:\ folder.  Edit it as follows: 

```json
{
	/*Maps the Active Directory template to the CA certificate type. 
	When enrollment is requested for an Active Directory certificate template, 
	the corresponding CA type will be enrolled.
	Templates are specified by CommonName*/
	"Templates":{
    		"GoDaddyDVSSL": {
     			"ProductID": "DV_SSL"
    		},
    		"GoDaddyDVWildcardSSL": {
      			"ProductID": "DV_WILDCARD_SSL"
		    },
    		"GoDaddyUCCDVSSL": {
      			"ProductID": "UCC_DV_SSL"
    		}	
	},
	/*Grant permissions on the CA to users or groups in the local domain.
	READ: Enumerate and read contents of certificates.
	ENROLL: Request certificates from the CA.
	OFFICER: Perform certificate functions such as issuance and revocation. This is equivalent to "Issue and Manage" permission on the Microsoft CA.
	ADMINISTRATOR: Configure/reconfigure the gateway.
	Valid permission settings are "Allow", "None", and "Deny".*/
	"Security":{
		/* Replace "Keyfactor\\Administrator with the domain\\account that has administrative privileges */
		"Keyfactor\\Administrator":{
			"READ":"Allow",
			"ENROLL":"Allow",
			"OFFICER":"Allow",
			"ADMINISTRATOR":"Allow"
		},
		/* Replace "Keyfactor\\SVC_TimerService with the domain\\account service account that will perform enrollment, sync, and revocation */
		by the Command Service. */
		"Keyfactor\\SVC_TimerService":{
			"READ":"Allow",
			"ENROLL":"Allow",
			"OFFICER":"Allow",
			"ADMINISTRATOR":"None"
		},
		/*Replace Keyfactor\\SVC_AppPool with the Application Pool Account for Keyfactor Command needs read at minimum.  There are some function in Command that are not delegated to the logged in user (may be a result of local lab configuration TBD) so this allows the command portal to enumerate templates available in the AnyGateway CA
		*/
		"Keyfactor\\SVC_AppPool":{
			"READ":"Allow", //List the templates supported by the CA
			"ENROLL":"Allow",
			"OFFICER":"Allow",//Required to allow the portal to revoke a certificate. TODO: Ensure this is the case or if it should be delegated
			"ADMINISTRATOR":"None"
		}		
	},
	/*The Certificate Managers section is optional.
	If configured, all users or groups granted OFFICER permissions under the Security section
	must be configured for at least one Template and one Requester. 
	Uses "<All>" to specify all templates. Uses "Everyone" to specify all requesters.
	Valid permission values are "Allow" and "Deny".*/
	"CertificateManagers":null,
	/*"CertificateManagers":{
		"DOMAIN\\Username":{
			"Templates":{
				"MyTemplateShortName":{
					"Requesters":{
						"Everyone":"Allow",
						"DOMAIN\\Groupname":"Deny"
					}
				},
				"<All>":{
					"Requesters":{
						"Everyone":"Allow"
					}
				}
			}
		}
	},*/
	/*Information necessary to authenticate to the CA.*/
	"CAConnection":{
		// Base URL for GoDaddy API calls.  This value should probably not need to change 
		//  from what is shown below
    		"APIUrl": "https://api.ote-godaddy.com",
		// The ShopperId is the "Customer #" found by selecting the pulldown on the top 
		//   right of the GoDaddy portal home page after signing in using the account 
		//   being used for the Gateway
    		"ShopperId": "9999999999",
		// The APIKey is the GoDaddy API Key and secret mentioned in "Prerequisites"
    		"APIKey": "sso-key {large string value API Key}:{large string value API Secret}",
		// One of four values based on the CA chain enrolled certificates should be 
		//  validated against - GODADDY_SHA_1, GODADDY_SHA_2, STARTFIELD_SHA1, or STARTFIELD_SHA2
    		"RootType": "GODADDY_SHA_2",


		// The SyncPageSize represents the number of certificates that will be returned 
		//  for each GoDaddy "get certificates" API call during a "sync" operation.  
		//  The API call will be repeated in batches of this number until all cerificates 
		//  are retrieved from the GoDady CA.  GoDaddy has no imposed limit on the number 
		//  of certificates that can be returned, but due to the amount of data being returned 
		//  for each call, certificate data may need to be returned in batches.
    		"SyncPageSize": "50", //Default=50, Minimum=10, Maximum=1000


		// The following 7 settings are all optional.  They each have a: 1) default value,  
		//  2) minimum allowed value, and 3) maximum allowed value.  If any value is missing 
		//  or outside the min/max allowed range, the default value will be used


		// EnrollmentRetries is the number of tries an Enroll operation will attempt to successfully 
		//  enroll a certificate (defined as a certificate being ISSUED or PENDING_ISSUANCE) 
		//  against the GoDaddy CA before returning an error.
    		"EnrollmentRetries": "2", //Default=2, Minimum=0, Maximum=5


		// SecondsBetweenEnrollmentRetries is the amount of time an Enroll operation will wait 
		//  between enrollment requests against the GoDaddy CA if the previous attempt did not 
		//  produce a certificate with a status of ISSUED or PENDING_ISSUANCE.
    		"SecondsBetweenEnrollmentRetries": "5", //Default=5, Minimum=2, Maximum=20


		// ApiTimeoutInSeconds is the amount of time in seconds that a GoDaddy API request 
		//  will wait before the call times out, producing a timeout error.
    		"ApiTimeoutInSeconds": "20", //Default=20, Minimum=2, Maximum=100


		// NumberOfCertPageRetrievalRetriesBeforeFailure is the number of times during a sync 
		//  the retrieval of an individual page of certificates will be retried after a 
		//  timeout before the sync will fail
    		"NumberOfCertPageRetrievalRetriesBeforeFailure": "2", //Default=2, Minimum=0, Maximum=10


		// NumberOfCertDownloadRetriesBeforeSkip is the number of times during a sync the retrieval 
		//  of any individual certificate will be retried before that individual certificate is 
		//  skipped if the GoDaddy API download request times out.
    		"NumberOfCertDownloadRetriesBeforeSkip": "2", //Default=2, Minimum=0, Maximum=10


		// NumberOfTimeoutsBeforeSyncFailure is the total number of timeouts all GoDaddy
		//  API requests can return during a sync before the sync will fail with an error
    		"NumberOfTimeoutsBeforeSyncFailure": "2", //Default=100, Minimum=0, Maximum=5000


		// MillisecondsBetweenCertDownloads is the amount of time, in milliseconds a sync 
		//  operation will wait between GoDaddy API download requests.  This is necessary 
		//  because GoDaddy places a 60 API request per minute limit on most accounts.  
		//  After that 60 limit has been reached, GoDaddy will begin returning 
		//  TOO_MANY_REQUEST errors.  A value of "1000" (1 second between requests) is 
		//  recommended, but this is configurable in case an individual account allows a 
		//  higher number of requests.
    		"MillisecondsBetweenCertDownloads": "1000" //Default=1000, Minimum=0, Maximum=1000

	},
	/*Information to register the Gateway for client connections.*/
	"GatewayRegistration":{
		// LogicalName is the Logical Name of the CA set up in Keyfactor - PKI Management => Certificate Authorities (later step)
		"LogicalName": "GoDaddyCA",
		// GatewayCertificate is the location and thumbprint of the GoDaddy intermediate CA certificate installed in a previous step
		"GatewayCertificate": {
      			"StoreName": "CA",
      			"StoreLocation": "LocalMachine",
      			"Thumbprint": "‎27ac9369faf25207bb2627cefaccbe4ef9c319b8"
		}
	},
	/*Settings for the Gateway Service*/
	"ServiceSettings":{
		// ViewIdleMinutes - Number of minutes a Sync operation can take before a timeout is reported
		"ViewIdleMinutes":8,
		// How often, in hours, a full scan will occur
		"FullScanPeriodHours": 1,
		// How often, in minutes, a partial scan will occur.  NOTE: for the GoDady AnyGateway, a partial scan is the same as a full scan
		"PartialScanPeriodMinutes":15
	}
}
```


##### Step 7 - Start the Keyfactor AnyGateway Service


##### Step 8 - Follow the AnyGateway instructions to set up your database and configuration


##### Step 9 - Add the GoDaddy CA to Keyfactor Command


##### Step 10 - Add the GoDaddy Products (Templates) to Keyfactor Command
For each of the three templates (GoDaddyDVSSL, GoDaddyDVWildcardSSL, and GoDaddyUCCDVSSL) configured in Step 4 in the AnyGatewayConfig.json file, create a corresponding template in Keyfactor Command.  **NOTE:** The Template Short Name of each **must** exactly match the corresponding labels set up in the AnyGatewayConfig.json file.


##### Step 11 - Add Custom Enrollment Fields
For each template set up in Step 8, certain custom enrollment fields **must** be added:

**GoDaddyDVSSL and GoDaddyDVWildcardSSL:**
  - CertificatePeriodInYears (required) - Number of years the certificate will be validated
  - LastName (required) - Last name of certificate requestor
  - FirstName (required) - First name of certificate requestor
  - Email (required) - Email address of requestor
  - Phone (required) - Phone number of requestor

**GoDaddyUCCDVSSL:**
  - All enrollment fields for GoDaddyDVSSL **and**
  - SlotSize (optional) - Represents the maximum number of SANs that a certificate may have.  Default is "FIVE" if this is not supplied.  Only valid for GoDaddy UCC* product type certificates.  This should be a multiple choice selection with the following values:
  - FIVE
  - TEN
  - FIFTEEN
  - TWENTY
  - THIRTY
  - FOURTY
  - FIFTY
  - ONE_HUNDRED

  **GoDaddyOVSSL, GoDaddyOVWildcardSSL, GoDaddyOVCS, and GoDaddyOVDS:**
  - All enrollment fields for GoDaddyDVSSL **and**
  - JobTitle (required) - The job title of the certificate requestor
  - Organization Name (required) The name of the organization to be validated against
  - OrganizationAddress (required) - The address of the organization to be validated against
  - OrganizationCity (required) - The city of the organization to be validated against
  - OrganizationState (required) - The full state name (no abbreviations) of the organization to be validated against
  - OrganizationCountry (required) - The 2 character abbreviation of the organization to be validated against
  - OrganizationPhone (required) - The phone number of the organization to be validated against

  **GoDaddyUCCEVSSL:**
  - All enrollment fields for GoDaddyOVSSL **and**
  - SlotSize (optional) - As described under GoDaddyUCCDVSSL 

  **GoDaddyEVSSL:**
  - All enrollment fields for GoDaddyOVSSL **and**
  - JurisdictionState (required) - The full state name (no abbreviations) of where documents were filed to create the organization
  - JurisdictionCountry (required) - The two character country abbreviation of where documents were filed to create the organization
  - RegistrationNumber (required) - The registration number assigned to the organization when its documents were filed for registration

  **GoDaddyUCCEVSSL:**
  - All enrollment fields for GoDaddyEVSSL **and**
  - SlotSize (optional) - As described under GoDaddyUCCDVSSL 


***

### License
[Apache](https://apache.org/licenses/LICENSE-2.0)

