using Microsoft.AspNetCore.Mvc;
using System;

namespace SaqayaProject.Dto
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public bool marketingConsent { get; set; }
    }
       
}
