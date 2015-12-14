using System;
using System.Collections;
using System.Text;
using System.Configuration;
using System.Xml;

namespace MobileTopUp.Configuration
{
    public sealed class StoreConfiguration : ConfigurationSection
    {
        private StoreConfiguration()
        {

        }

        private static StoreConfiguration _instance;
        public static StoreConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (StoreConfiguration)System.Configuration.ConfigurationManager.GetSection("topupStore");
                }
                return _instance;
            }
        }

        [ConfigurationProperty("payment")]
        public PaymentElement Payment
        {
            get
            {
                return (PaymentElement)this["payment"];
            }
        }

        [ConfigurationProperty("wechat")]
        public AccessAccountElement Wechat
        {
            get
            {
                return (AccessAccountElement)this["wechat"];
            }
        }
        [ConfigurationProperty("pxpay")]
        public AccessAccountElement PxPay
        {
            get
            {
                return (AccessAccountElement)this["pxpay"];
            }
        }

        [ConfigurationProperty("directories", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(DirectoryElementCollection),
          AddItemName = "add",
          ClearItemsName = "clear",
          RemoveItemName = "remove")]
        public DirectoryElementCollection Directories
        {
            get
            {
                return (DirectoryElementCollection)base["directories"];
            }
        }

        [ConfigurationProperty("administrators", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(AdministratorElementCollection),
          AddItemName = "add",
          ClearItemsName = "clear",
          RemoveItemName = "remove")]
        public AdministratorElementCollection Administrators
        {
            get
            {
                return (AdministratorElementCollection)base["administrators"];
            }
        }

        [ConfigurationProperty("storeSetttings", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(StoreSettingElementCollection),
        AddItemName = "add",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
        public StoreSettingElementCollection StoreSettings
        {
            get
            {
                return (StoreSettingElementCollection)base["storeSetttings"];
            }
        }
        //
        public string TemporaryDirectory
        {
            get
            {
                return Directories["temp"];
            }
        }
        public string TesseractDataDirectory
        {
            get
            {
                return Directories["tesseractData"];
            }
        }
        public string VoucherImageDirectory
        {
            get
            {
                return Directories["voucherImage"];
            }
        }
        public bool FakeLogin
        {
            get
            {
                return bool.Parse(StoreSettings["fakeLogin"]);
            }
        }
        public string RootUrl
        {
            get
            {
                return StoreSettings["RootURL"];
            }
        }

    }

    /// <summary>
    /// payment element
    /// </summary>
    public class PaymentElement : ConfigurationElement
    {
        [ConfigurationProperty("fullCharge", DefaultValue = "false", IsRequired = true)]
        public Boolean IsFullCharge
        {
            get
            {
                return (Boolean)this["fullCharge"];
            }
        }

        [ConfigurationProperty("exchangeRateCNY", IsRequired = true)]
        public decimal ExchangeRateCNY
        {
            get
            {
                return (decimal)this["exchangeRateCNY"];
            }
        }

        public decimal ExchangeRateNZD
        {
            get
            {
                return 1.0M;
            }
        }

        [ConfigurationProperty("discount", IsRequired = true)]
        public decimal Discount
        {
            get
            {
                return (decimal)this["discount"];
            }
        }
    }

    /// <summary>
    /// access account element
    /// </summary>
    public class AccessAccountElement : ConfigurationElement
    {
        [ConfigurationProperty("id", IsRequired = true)]
        public string Id
        {
            get
            {
                return this["id"] as string;
            }
        }
        [ConfigurationProperty("key", IsRequired = true)]
        public string Key
        {
            get
            {
                return this["key"] as string;
            }
        }
    }

    /// <summary>
    /// directory element
    /// </summary>
    public class DirectoryElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true, IsKey = true)]
        public string Type
        {
            get
            {
                return this["type"] as string;
            }
        }
        [ConfigurationProperty("path", IsRequired = true)]
        public string Path
        {
            get
            {
                return this["path"] as string;
            }
        }
    }

    /// <summary>
    /// directory collection
    /// </summary>
    public class DirectoryElementCollection : ConfigurationElementCollection
    {

        new public string this[string directoryType]
        {
            get { return ((DirectoryElement)BaseGet(directoryType)).Path; }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new DirectoryElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DirectoryElement)element).Type;
        }
    }

    /// <summary>
    /// administrator element
    /// </summary>
    public class AdministratorElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }
        [ConfigurationProperty("wechatId", IsRequired = true, IsKey = true)]
        public string WechatId
        {
            get
            {
                return this["wechatId"] as string;
            }
        }
    }

    public class AdministratorElementCollection : ConfigurationElementCollection
    {
        public object[] GetAllKeys()
        {
            return BaseGetAllKeys();
        }
        new public string this[string id]
        {
            get
            {
                return ((AdministratorElement)BaseGet(id)).Name;
            }
        }
        public AdministratorElement this[int i]
        {
            get { return (AdministratorElement)BaseGet(i); }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new AdministratorElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AdministratorElement)element).WechatId;
        }
    }

    public class StoreSettingElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        public string Key
        {
            get
            {
                return this["key"] as string;
            }
        }
        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get
            {
                return this["value"] as string;
            }
        }
    }

    public class StoreSettingElementCollection : ConfigurationElementCollection
    {
        new public string this[string key]
        {
            get { return ((StoreSettingElement)BaseGet(key)).Value; }
        }
        public StoreSettingElement this[int i]
        {
            get { return (StoreSettingElement)BaseGet(i); }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new StoreSettingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((StoreSettingElement)element).Key;
        }
    }
}