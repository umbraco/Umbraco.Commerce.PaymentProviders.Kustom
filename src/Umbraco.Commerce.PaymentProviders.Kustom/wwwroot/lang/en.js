export default {
    ucPaymentProviders: {
        'kustomHppLabel': 'Kustom (HPP)',
        'kustomHppDescription': 'Kustom payment provider using the Kustom Hosted Payment Page (HPP)',
        'kustomHppSettingsContinueUrlLabel': 'Continue URL',
        'kustomHppSettingsContinueUrlDescription': 'The URL to continue to after this provider has done processing. eg: /continue/',
        'kustomHppSettingsCancelUrlLabel': 'Cancel URL',
        'kustomHppSettingsCancelUrlDescription': 'The URL to return to if the payment attempt is canceled. eg: /cancel/',
        'kustomHppSettingsErrorUrlLabel': 'Error URL',
        'kustomHppSettingsErrorUrlDescription': 'The URL to return to if the payment attempt errors. eg: /error/',

        'kustomHppSettingsBillingAddressLine1PropertyAliasLabel': 'Billing Address (Line 1) Property Alias',
        'kustomHppSettingsBillingAddressLine1PropertyAliasDescription': '[Required] The order property alias containing line 1 of the billing address',

        'kustomHppSettingsBillingAddressLine2PropertyAliasLabel': 'Billing Address (Line 2) Property Alias',
        'kustomHppSettingsBillingAddressLine2PropertyAliasDescription': 'The order property alias containing line 2 of the billing address',

        'kustomHppSettingsBillingAddressCityPropertyAliasLabel': 'Billing Address City Property Alias',
        'kustomHppSettingsBillingAddressCityPropertyAliasDescription': '[Required] The order property alias containing the city of the billing address',

        'kustomHppSettingsBillingAddressStatePropertyAliasLabel': 'Billing Address State Property Alias',
        'kustomHppSettingsBillingAddressStatePropertyAliasDescription': 'The order property alias containing the state of the billing address',

        'kustomHppSettingsBillingAddressZipCodePropertyAliasLabel': 'Billing Address ZipCode Property Alias',
        'kustomHppSettingsBillingAddressZipCodePropertyAliasDescription': '[Required] The order property alias containing the zip code of the billing address',

        'kustomHppSettingsApiRegionLabel': 'API Region',
        'kustomHppSettingsApiRegionDescription': 'The Kustom API Region to use',

        'kustomHppSettingsTestApiUsernameLabel': 'Test API Username',
        'kustomHppSettingsTestApiUsernameDescription': 'The Username to use when connecting to the test Kustom API',

        'kustomHppSettingsTestApiPasswordLabel': 'Test API Password',
        'kustomHppSettingsTestApiPasswordDescription': 'The Password to use when connecting to the test Kustom API',

        'kustomHppSettingsLiveApiUsernameLabel': 'Live API Username',
        'kustomHppSettingsLiveApiUsernameDescription': 'The Username to use when connecting to the live Kustom API',

        'kustomHppSettingsLiveApiPasswordLabel': 'Live API Password',
        'kustomHppSettingsLiveApiPasswordDescription': 'The Password to use when connecting to the live Kustom API',

        'kustomHppSettingsCaptureLabel': 'Capture',
        'kustomHppSettingsCaptureDescription': 'Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture',

        'kustomHppSettingsTestModeLabel': 'Test Mode',
        'kustomHppSettingsTestModeDescription': 'Set whether to process payments in test mode',

        // ===================
        // Advanced
        // ===================

        'kustomHppSettingsPaymentPageLogoUrlLabel': 'Payment Page Logo Url',
        'kustomHppSettingsPaymentPageLogoUrlDescription': 'Fully qualified URL of a logo image to display on the payment page',

        'kustomHppSettingsPaymentPagePageTitleLabel': 'Payment Page Page Title',
        'kustomHppSettingsPaymentPagePageTitleDescription': 'A custom title to display on the payment page',

        'kustomHppSettingsProductTypePropertyAliasLabel': 'Product Type Property Alias',
        'kustomHppSettingsProductTypePropertyAliasDescription': 'The order line property alias containing the type of the product. Property value can be one of either \'physical\' or \'digital\'',

        'kustomHppSettingsPaymentMethodCategoriesLabel': 'Payment Method Categories',
        'kustomHppSettingsPaymentMethodCategoriesDescription': 'Comma separated list of payment method categories to show on the payment page. If empty, all allowable options will be presented. Options are DIRECT_DEBIT, DIRECT_BANK_TRANSFER, PAY_NOW, PAY_LATER and PAY_OVER_TIME',

        'kustomHppSettingsPaymentMethodCategoryLabel': 'Payment Method Category',
        'kustomHppSettingsPaymentMethodCategoryDescription': 'The payment method category to show on the payment page. Options are DIRECT_DEBIT, DIRECT_BANK_TRANSFER, PAY_NOW, PAY_LATER and PAY_OVER_TIME',

        'kustomHppSettingsEnableFallbacksLabel': 'Enable Fallbacks',
        'kustomHppSettingsEnableFallbacksDescription': 'Set whether to fallback to other payment options if the initial payment attempt fails before redirecting back to the site',

        'kustomHppMetaDataKustomSessionIdLabel': 'Kustom Session ID',
        'kustomHppMetaDataKustomOrderIdLabel': 'Kustom Order ID',
        'kustomHppMetaDataKustomReferenceLabel': 'Kustom Reference',
    },
};
