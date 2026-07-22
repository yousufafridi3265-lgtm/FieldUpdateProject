using System.Collections.Generic;

namespace EncompassIntegration
{
    // XML se EncompassInfo section ka model
    public class EncompassInfoModel
    {
        public string ApiServer { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string InstanceId { get; set; }
        public string GrantType { get; set; }
        public string Scope { get; set; }
    }

    // Field Update configuration ka model
    public class FieldUpdateConfigModel
    {
        public string FilterJson { get; set; }
        public string FieldId { get; set; }
        public string FieldValue { get; set; }
    }

    // Complete Configuration Root Model
    public class AppConfigurationModel
    {
        public EncompassInfoModel EncompassInfo { get; set; }
        public FieldUpdateConfigModel FieldUpdate { get; set; }
    }

    // Token Response Model (JSON response deserialize karne ke liye)
    public class TokenResponseModel
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }
}