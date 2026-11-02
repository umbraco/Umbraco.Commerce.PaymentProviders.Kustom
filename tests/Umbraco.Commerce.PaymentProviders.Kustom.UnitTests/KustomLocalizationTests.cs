using System.Reflection;
using FluentAssertions;
using Umbraco.Commerce.Core.PaymentProviders;

namespace Umbraco.Commerce.PaymentProviders.Kustom.UnitTests
{
    // The backoffice builds label keys from the provider alias ("kustom-hpp" -> "kustomHpp"), so every
    // setting and transaction meta data entry needs a matching key in wwwroot/lang/en.js, or the raw key shows.
    public class KustomLocalizationTests
    {
        private const string KeyPrefix = "kustomHpp";

        private static readonly string EnJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "lang", "en.js"));

        public static TheoryData<string> SettingKeys()
        {
            TheoryData<string> data = [];
            foreach (PropertyInfo prop in typeof(KustomHppSettings).GetProperties().Where(x => x.GetCustomAttribute<PaymentProviderSettingAttribute>() != null))
            {
                data.Add($"{KeyPrefix}Settings{prop.Name}Label");
                data.Add($"{KeyPrefix}Settings{prop.Name}Description");
            }

            return data;
        }

        public static TheoryData<string> MetaDataKeys()
        {
            TheoryData<string> data = [];
            foreach (TransactionMetaDataDefinition definition in new KustomHppPaymentProvider(null!, null!).TransactionMetaDataDefinitions)
            {
                data.Add($"{KeyPrefix}MetaData{char.ToUpperInvariant(definition.PropertyAlias[0])}{definition.PropertyAlias[1..]}Label");
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(SettingKeys))]
        public void Every_Setting_Has_A_Label_And_Description(string key)
            => EnJs.Should().Contain($"'{key}'");

        [Theory]
        [MemberData(nameof(MetaDataKeys))]
        public void Every_Transaction_MetaData_Entry_Has_A_Label(string key)
            => EnJs.Should().Contain($"'{key}'");
    }
}
