using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public class OrderHeader
    {
        [ValidateNever]
        public int Id { get; set; }


        [ValidateNever]
        public string? ApplicationUserId { get; set; }
        [ForeignKey(nameof(ApplicationUserId))]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }



        [ValidateNever]
        public DateTime OrderDate { get; set; }
        [ValidateNever]
        public DateTime ShippingDate { get; set; }
        [ValidateNever]
        public double OrderTotal { get; set; }
        [ValidateNever]
        public string? OrderStatus { get; set; }
        [ValidateNever]
        public string? PaymentStatus { get; set; }
        [ValidateNever]
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }
        [ValidateNever]
        public DateTime PaymentDate { get; set; }
        [ValidateNever]
        public DateTime PaymentDueDate { get; set; }
        [ValidateNever]
        public string? SessionId { get; set; }
        [ValidateNever]
        public string? PaymentIntentId { get; set; }

        [Required]
        public string? PhoneNumber { get; set; }
        [Required]
        public string? StreetAddress { get; set; }
        [Required]
        public string? City { get; set; }
        [Required]
        public string? State { get; set; }
        [Required]
        public string? PostalCode { get; set; }
        [Required]
        public string? Name { get; set; }
    }
}
