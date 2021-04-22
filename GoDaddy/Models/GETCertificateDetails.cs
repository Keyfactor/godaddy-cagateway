// Copyright 2021 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keyfactor.AnyGateway.GoDaddy.Models
{
    public enum CertificateStatusEnum   
    {
        PENDING_ISSUANCE,
        CURRENT,
        ISSUED,
        REVOKED,
        CANCELED,
        DENIED,
        PENDING_REVOCATION,
        PENDING_REKEY,
        UNUSED,
        EXPIRED
    }

    public class GETCertificateDetailsResponse
    {
        public string csr { get; set; }
        public string certificateId { get; set; }
        public string commonName { get; set; }
        public Contact contact { get; set; }
        public DateTime? createdAt { get; set; }
        public string deniedReason { get; set; }
        public Organization organization { get; set; }
        public int? period { get; set; }
        public string productType { get; set; }
        public int? progress { get; set; }
        public DateTime? revokedAt { get; set; }
        public string rootType { get; set; }
        public string serialNumber { get; set; }
        public string serialNumberHex { get; set; }
        public string slotSize { get; set; }
        public string status { get; set; }
        public List<SubjectAlternativeName> subjectAlternativeNames { get; set; }
        public DateTime? validEnd { get; set; }
        public DateTime? validStart { get; set; }
        public DateTime? subscriptionStart { get; set; }
        public DateTime? subscriptionEnd { get; set; }
        public DateTime? nextVettingDate { get; set; }
    }

    public class Contact
    {
        public string email { get; set; }
        public string jobTitle { get; set; }
        public string nameFirst { get; set; }
        public string nameLast { get; set; }
        public string nameMiddle { get; set; }
        public string phone { get; set; }
        public string suffix { get; set; }
    }

    public class Address
    {
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string postalCode { get; set; }
        public string state { get; set; }
    }

    public class JurisdictionOfIncorporation
    {
        public string city { get; set; }
        public string country { get; set; }
        public string county { get; set; }
        public string state { get; set; }
    }

    public class Organization
    {
        public Address address { get; set; }
        public string assumedName { get; set; }
        public JurisdictionOfIncorporation jurisdictionOfIncorporation { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string registrationAgent { get; set; }
        public string registrationNumber { get; set; }
    }

    public class SubjectAlternativeName
    {
        public string status { get; set; }
        public string subjectAlternativeName { get; set; }
    }
}
