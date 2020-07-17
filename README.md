# Rock mappify.io Location Service

## Intro
This is a location service for [Rock](http://rockrms.com) that verifies, standardises and geocodes Australian (AUS) addresses using the [mappify.io](http://mappify.io) API. The generous 2500 free requests per day on the Pay as you go signup option will meet most churches needs.

This plugin is available on Github to help the Australian churches using Rock RMS.  The repository includes the C# source for use with the [Rockit SDK](http://www.rockrms.com/Rock/Developer). To download the latest release of the plugin in .dll format click [here](https://github.com/hopecentral/mappify.io/releases/latest).

## A Quick Explanation
This location service will pass the values (if any are present) of the address line 1, address line 2, city, state, and postal code fields from Rock to the mappify.io address autocomplete remote procedure call API service. The location service asks for the best match and, if values are present in the response, it verifies the confidence level of the match and will either:
1. confirm verification and replace the address values stored in Rock with the standardised response values, including geocode coordinates, or
2. deny verification, due to low match confidence, and instead provide the best match provided by mappify.io with an associated confidence level as a percentage. If this happens, check the recommended match details provided and if suitable update the address to match it and verify again.

## mappify.io Data
mappify.io uses address data from the PSMA Geocoded National Address File (GNAF). G-NAF is built from addresses supplied by 10 contributors, including the land agencies in each state and territory of Australia. The source data is:
*Independently examined and validated.
*Matched textually and spatially.
*Assigned a geocode to place the address on a map.

## Contribute
If anything looks broken or you think of an improvement please flag up an issue.

## Thanks
Thanks to [Bricks and Mortar Studio](https://bricksandmortarstudio.com/) whose [IdealPostcodes](https://github.com/BricksandMortar/IdealPostcodes) plugin was where initial ideas were drawn from.
Thanks to the [Spark Development Network](https://sparkdevnetwork.org/) for creating [Rock](https://github.com/SparkDevNetwork/Rock) and making it so accessible.

This project is licensed under the [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0.html).
