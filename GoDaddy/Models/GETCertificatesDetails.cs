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
    public class GETCertificatesDetailsResponse
    {
        public List<CertificateDetails> certificates { get; set; }
        public PageInfo pagination { get; set; }
    }

    public class CertificateDetails
    {
        public string certificateId { get; set; }
        public int period { get; set; }
        public string commonName { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string serialNumber { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? completedAt { get; set; }
        public DateTime? validEndAt { get; set; }
        public DateTime? validStartAt { get; set; }
        public DateTime? revokedAt { get; set; }
    }

    public class PageInfo
    {
        public string first { get; set; }
        public string previous { get; set; }
        public string next { get; set; }
        public string last { get; set; }
    }
}
