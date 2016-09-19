using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MobileTopUp
{
    public class VoucherType
    {
        public const int Twenty = 20;
    }
    public enum VoucherStatus
    {
        Available = 0,
        Unpaid = 1,
        Paid = 2
    }
    public class CustomEnum<T>
    {
        #region static
        protected static bool _inited = false;
        protected static readonly Dictionary<string, CustomEnum<T>> _list = new Dictionary<string, CustomEnum<T>>();
        //enum
        protected static readonly CustomEnum<T> Unknown = new CustomEnum<T>("");
        public static bool Contains(string value)
        {
            return _list.ContainsKey(value);
        }
        public static implicit operator string (CustomEnum<T> type)
        {
            return type.Value;
        }
        public static implicit operator CustomEnum<T>(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Unknown;
            }
            else if (Contains(value))
            {
                return _list[value];
            }
            else
            {
                throw new InvalidCastException(string.Format("Type.{0} not found.", value));
            }
        }
        public static bool operator ==(CustomEnum<T> a, CustomEnum<T> b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }
        public static bool operator !=(CustomEnum<T> a, CustomEnum<T> b)
        {
            return !(a == b);
        }

        #endregion
        #region instance
        protected readonly string _value;
        protected bool _isMeta = true;
        protected CustomEnum<T> _instance = null;

        protected CustomEnum()
        {
            _isMeta = false;
            _instance = Unknown;
        }
        protected CustomEnum(string value)
        {
            _value = value;
            _list.Add(value, this);
        }

        public string Value
        {
            get
            {
                if (_isMeta)
                {
                    return _value;
                }
                else
                {
                    return _instance.Value;
                }
            }
            set
            {
                if (!_isMeta)
                {
                    _instance = value;
                }
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            CustomEnum<T> customEnum = obj as CustomEnum<T>;
            if (customEnum == null)
            {
                return false;
            }
            return this.Equals(customEnum);
        }

        public bool Equals(CustomEnum<T> customEnum)
        {
            if (customEnum == null)
            {
                return false;
            }
            return System.Object.ReferenceEquals(
                                this._isMeta ? this : this._instance,
                                customEnum._isMeta ? customEnum : customEnum._instance);
        }

        public override int GetHashCode()
        {
            if (_value == null)
            {
                return "".GetHashCode();
            }
            else
            {
                return _value.GetHashCode();
            }
        }
        #endregion
    }

    //account
    public sealed class AccountType : CustomEnum<AccountType>
    {
        public static readonly AccountType Wechat;
        static AccountType()
        {
            Wechat = new AccountType("WECHAT");
            _inited = true;
        }
        private AccountType(string value) : base(value)
        {
        }
        public AccountType() : base()
        {
        }

        public static implicit operator string (AccountType type)
        {
            return type.Value;
        }
        public static implicit operator AccountType(string value)
        {
            if (!_inited)
            {
                throw new InvalidCastException(string.Format("Account Type has not inited."));
            }
            else if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else if (Contains(value))
            {
                return (AccountType)_list[value];
            }
            else
            {
                throw new InvalidCastException(string.Format("Account Type {0} not found.", value));
            }
        }
    }

    //brand
    public sealed class BrandType : CustomEnum<BrandType>
    {
        public static readonly BrandType Spark;
        public static readonly BrandType Vodafone;
        public static readonly BrandType TwoDegrees;
        public static readonly BrandType Skinny;
        static BrandType()
        {
            Spark = new BrandType("SPARK");
            Vodafone = new BrandType("VODAFONE");
            TwoDegrees = new BrandType("2DEGREES");
            Skinny = new BrandType("SKINNY");
            _inited = true;
        }
        private BrandType(string value) : base(value)
        {

        }
        public BrandType() : base()
        {

        }

        public static implicit operator string (BrandType type)
        {
            return type.Value;
        }
        public static implicit operator BrandType(string value)
        {
            if (!_inited)
            {
                throw new InvalidCastException(string.Format("Brand Type has not inited."));
            }
            else if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else if (Contains(value))
            {
                return (BrandType)_list[value];
            }
            else
            {
                throw new InvalidCastException(string.Format("Brand Type {0} not found.List {1} {2}", value, _list.Count, string.Join(".", _list)));
            }
        }

        public static BrandType VerifiedOrDefault(string brand)
        {
            if (string.IsNullOrEmpty(brand))
            {
                return Spark;
            }
            brand = brand.ToUpper();
            if (!Contains(brand))
            {
                return Spark;
            }

            return (BrandType)brand;
        }
    }

    //currency
    public sealed class CurrencyType : CustomEnum<CurrencyType>
    {
        public static readonly CurrencyType NZD;
        public static readonly CurrencyType CNY;
        static CurrencyType()
        {
            NZD = new CurrencyType("NZD");
            CNY = new CurrencyType("CNY");
            _inited = true;
        }
        private CurrencyType(string value) : base(value)
        {
        }
        public CurrencyType() : base()
        {
        }
        public static implicit operator string (CurrencyType type)
        {
            return type.Value;
        }
        public static implicit operator CurrencyType(string value)
        {
            if (!_inited)
            {
                throw new InvalidCastException(string.Format("Currency Type has not inited."));
            }
            else if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else if (Contains(value))
            {
                return (CurrencyType)_list[value];
            }
            else
            {
                throw new InvalidCastException(string.Format("Currency Type.{0} not found.", value));
            }
        }
    }

    //payment
    public sealed class PaymentType : CustomEnum<PaymentType>
    {
        public static readonly PaymentType Skip;
        public static readonly PaymentType WechatPay;
        public static readonly PaymentType PxPay;

        static PaymentType()
        {
            Skip = new PaymentType("SKIP");
            WechatPay = new PaymentType("WECHATPAY");
            PxPay = new PaymentType("PXPAY");
            _inited = true;

        }
        private PaymentType(string value) : base(value)
        {
        }

        public PaymentType() : base()
        {
        }

        public static implicit operator string (PaymentType type)
        {
            return type.Value;
        }
        public static implicit operator PaymentType(string value)
        {
            if (!_inited)
            {
                throw new InvalidCastException(string.Format("Currency Type has not inited."));
            }
            else if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else if (Contains(value))
            {
                return (PaymentType)_list[value];
            }
            else
            {
                throw new InvalidCastException(string.Format("Payment Type {0} not found.", value));
            }
        }
    }

}