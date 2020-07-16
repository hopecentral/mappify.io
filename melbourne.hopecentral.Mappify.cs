// <copyright>
// Copyright 2020 Hope Central
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
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Net;

using Newtonsoft.Json;
using RestSharp;

using Rock;
using Rock.Address;
using Rock.Attribute;

namespace melbourne.hopecentral.Mappify.Address
{
    /// <summary>
    /// The address lookup and geocoding service from <a href="https://mappify.io/">mappify.io</a>
    /// </summary>
    [Description( "An address verification and geocoding service from mappify.io" )]
    [Export( typeof( VerificationComponent ) )]
    [ExportMetadata( "ComponentName", "Mappify" )]
    [TextField( "API Key", "Your mappify.io API key", true, "", "", 2 )]   
    public class Mappify : VerificationComponent
    {
        /// <summary>
        /// Standardizes and Geocodes an address using the mappify.io service
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="resultMsg">The result</param>
        /// <returns>
        /// True/False value of whether the verification was successful or not
        /// </returns>
        public override VerificationResult Verify( Rock.Model.Location location, out string resultMsg )
        {
            resultMsg = string.Empty;
            VerificationResult result = VerificationResult.None;

            string apiKey = GetAttributeValue( "APIKey" );
           
            //Create address that encodes correctly
            var addressParts = new[] { location.Street1, location.Street2, location.City, location.State, location.PostalCode };
            string streetAddress = string.Join( " ", addressParts.Where( s => !string.IsNullOrEmpty( s ) ) );       

            //restsharp API request
            var client = new RestClient( "https://mappify.io/api/rpc/" );
            var request = BuildRequest( streetAddress, apiKey );
            var response = client.Execute( request );

            if ( response.StatusCode == HttpStatusCode.OK )
            //Deserialize response into object
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                var mappifyResponse = JsonConvert.DeserializeObject<ResponseObject>( response.Content, settings );
                var mappifyAddress = mappifyResponse.result;
                if ( mappifyAddress.Any() )
                {
                    var address = mappifyAddress.FirstOrDefault();
                    int confidencePercentage = (int)(mappifyResponse.confidence * 100);
                    
                    location.StandardizeAttemptedResult = address?.gnafId;

                    if ( mappifyAddress.Count() == 1 || mappifyResponse.confidence >= 0.75 )
                    {
                        bool updateResult = UpdateLocation(location, address);
                        
                        if (updateResult)
                        {
                            result = VerificationResult.Geocoded;
                            resultMsg = string.Format("Verified with mappify.io to match GNAF: {0} with {1}% confidence, address standardised to: {2}. Coordinates updated.", address?.gnafId, confidencePercentage, address?.streetAddress);
                        }
                        else
                        {
                            result = VerificationResult.Standardized;
                            resultMsg = string.Format("Verified with mappify.io to match GNAF: {0} with {1}% confidence, address standardised to: {2}. Coordinates NOT updated.", address?.gnafId, confidencePercentage, address?.streetAddress);
                        }
                    }
                    else
                    {
                         resultMsg = string.Format("Not verified: mappify.io closest matching address: {0} with {1}% confidence", address?.streetAddress, confidencePercentage);
                    }
                }
                else
                {
                    resultMsg = "No match.";
                }
            }
            else
            {
                result = VerificationResult.ConnectionError;
                resultMsg = response.StatusDescription;
            }

            location.StandardizeAttemptedServiceType = "Mappify";
            location.StandardizeAttemptedDateTime = RockDateTime.Now;

            location.GeocodeAttemptedServiceType = "Mappify";
            location.GeocodeAttemptedDateTime = RockDateTime.Now;
            return result;
        }

        /// <summary>
        /// Builds a REST request 
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="inputAddress"></param>
        /// <returns></returns>
        private static IRestRequest BuildRequest( string streetAddress, string apiKey )
        {
            var request = new RestRequest( "address/autocomplete", Method.POST );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Content-Type", "application/json");
            var payload = new ApiPayload
            {
                streetAddress = streetAddress,
                apiKey = apiKey,
                formatCase = true,
                includeInternalIdentifiers = true,
            };
            request.AddBody(payload);

            return request;
        }

        /// <summary>
        /// Updates a Rock location to match a mappify.io StreetAddressRecord
        /// </summary>
        /// <param name="location">The Rock location to be modified</param>
        /// <param name="address">The mappify.io StreetAddressRecord to copy the data from</param>
        /// <returns>Whether the Location was succesfully geocoded</returns>
        public bool UpdateLocation( Rock.Model.Location location, StreetAddressRecord address )
        {
            string addressStreet = address.streetAddress;
            string[] separatingStrings = { ", " };
            string[] addressComponents = addressStreet.Split(separatingStrings, StringSplitOptions.RemoveEmptyEntries);
            
            if ( address.primary )
            {
                location.Street1 = addressComponents[0];
                location.Street2 = addressComponents[1];
            }
            else
            {
                location.Street1 = addressComponents[0];
                location.Street2 = "";
            }
            
            string city = address.suburb;
            city = CultureInfo.CurrentCulture.TextInfo.ToTitleCase( city.ToLower() );
            location.City = city;
            location.State = address.state;
            location.PostalCode = address.postCode;
            location.StandardizedDateTime = RockDateTime.Now;

            // If StreetAddressRecord has geocoding data set it on Location
            if (address.location.lat.HasValue && address.location.lon.HasValue)
            {
                bool setLocationResult = location.SetLocationPointFromLatLong(address.location.lat.Value, address.location.lon.Value);
                if ( setLocationResult )
                {
                    location.GeocodedDateTime = RockDateTime.Now;
                }
                return setLocationResult;
            }
            
            return false;
        }

#pragma warning disable

        public class ApiPayload
        {
            public string streetAddress { get; set; }
            public string apiKey { get; set; }
            public bool formatCase { get; set; }
            public bool includeInternalIdentifiers { get; set; }
        }

        public class Location
        {
            public double? lat { get; set; }
            public double? lon { get; set; }
        }

        public class StreetAddressRecord
        {
            public string buildingName { get; set; }
            public string flatNumberPrefix { get; set; }
            public int? flatNumber { get; set; }
            public string flatNumberSuffix { get; set; }
            public int? levelNumber { get; set; }
            public int? numberFirst { get; set; }
            public int? numberLast { get; set; }
            public string streetName { get; set; }
            public string streetType { get; set; }
            public string streetSuffixCode { get; set; }
            public string suburb { get; set; }
            public string state { get; set; }
            public string postCode { get; set; }
            public Location location { get; set; }
            public bool primary { get; set; }
            public string streetAddress { get; set; }
            public string jurisdictionId { get; set; }
            public string gnafId { get; set; }
        }

        public class ResponseObject
        {
            public string type { get; set; }
            public List<StreetAddressRecord> result { get; set; }
            public double? confidence { get; set; }
        }
#pragma warning restore

    }
}
