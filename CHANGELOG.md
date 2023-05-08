v1.1.0
- Added partial sync functionality
- Added five new optional settings to Gateway config - ApiTimeoutinSeconds, NumberOfCertPageRetrievalRetriesBeforeFailure, NumberOfCertDownloadRetriesBeforeSkip, 
	NumberOfTimeoutsBeforeSyncFailure, MillisecondsBetweenCertDownloads
- Added logic for certificate download retries for timeouts based on new settings above
- Added additional sync statistics logging for each sync showing number of certificate retrievals, downloads, and any API timeout counts

v1.0.8
- Improved logging
- Improved error handling for API timeouts

v1.0.7
- Improved logging

v1.0.6  
- Code cleanup

v1.0.4/1.0.5  
- Update nuget packages

v1.0.3  
- Code cleanup, publish to github. 

v1.0.2  
- Remove PEM header before returning certificates during sync and enrollment  

v1.0.1  
- Added support for 5 OV and 2 EV GoDaddy products  
- Added Renew/Reissue functionality  

v1.0.0:  
- Original Version  


 
  


