using System;
using System.Threading.Tasks;

namespace EncompassIntegration
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                string configFilePath = @"Configuration.xml";
                var encompassService = new EncompassService();

                // 1. Config file parse karein
                Console.WriteLine("Loading configuration from XML...");
                AppConfigurationModel config = encompassService.LoadConfiguration(configFilePath);

                // 2. Access Token lein
                Console.WriteLine("Fetching Access Token...");
                string accessToken = await encompassService.GetAccessTokenAsync(config.EncompassInfo);

                Console.WriteLine("\n--- Success ---");
                Console.WriteLine($"Access Token: {accessToken}");
                Console.WriteLine($"Filter JSON Loaded: {config.FieldUpdate.FilterJson}");
                Console.WriteLine($"Field to Update: [{config.FieldUpdate.FieldId}] = {config.FieldUpdate.FieldValue}");



                // 3. Loan Pipeline API se Loan GUID Search Karein
                Console.WriteLine("3. Searching Loan via Pipeline API...");
                string loanGuid = await encompassService.SearchLoanGuidAsync(
                    config.EncompassInfo.ApiServer,
                    accessToken,
                    config.FieldUpdate.FilterJson
                );
                Console.WriteLine($"-> Found Loan GUID: {loanGuid}");

                // 4. Field 4002 Ko Update Karein
                Console.WriteLine($"4. Updating Field [{config.FieldUpdate.FieldId}] to value '{config.FieldUpdate.FieldValue}'...");
                bool isUpdated = await encompassService.UpdateLoanFieldAsync(
                    config.EncompassInfo.ApiServer,
                    accessToken,
                    loanGuid,
                    config.FieldUpdate.FieldId,
                    config.FieldUpdate.FieldValue
                );

                if (isUpdated)
                {
                    Console.WriteLine("\n==========================================");
                    Console.WriteLine(" SUCCESS: Field updated successfully!");
                    Console.WriteLine("==========================================");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR]: {ex.Message}");
            }
        }
    }
}