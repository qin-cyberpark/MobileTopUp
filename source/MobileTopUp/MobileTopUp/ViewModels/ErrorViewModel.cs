using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MobileTopUp.ViewModels
{
    public class ErrorViewModel
    {
        public enum ErrorType {Customer, General, FileNotFound, UnauthorizedAccess, OutOfStock}
        public ErrorType Type { get; set; }
        public string ImageFile { get; set; }
        public string BackUrl { get; set; }
        public string Message { get; set; }
        public ErrorViewModel()
        {
            Type = ErrorType.General;
        }
        public ErrorViewModel(ErrorType type)
        {
            Type = type;
        }
    }   
}